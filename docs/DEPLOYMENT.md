# BazarFlow Deployment Guide

This guide covers the v0.4 script-based deployment flow. It does not install a Windows Service, configure IIS automatically, install certificates, or install SQL Server.

## Prerequisites

- .NET 9 SDK for building and Entity Framework migrations.
- .NET 9 ASP.NET Core Runtime on machines that only run the published backend.
- Node.js and npm for building the Angular frontend.
- SQL Server or SQL Server Express.
- PowerShell.
- Windows permissions to:
  - run PowerShell scripts,
  - create deployment folders,
  - configure environment variables,
  - create or update the SQL Server database,
  - create `C:\BazarFlowBackups`,
  - grant backup folder access to the SQL Server service account.

## Build Steps

Run backend publish:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\build-backend.ps1
```

The backend is published to:

```text
artifacts\backend
```

Run frontend production build:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\build-frontend.ps1
```

The frontend static files are copied to:

```text
artifacts\frontend
```

The frontend build script fails if `frontend\src\environments\environment.prod.ts` still contains `YOUR_API_HOST`.

## Environment Variables

Configure production values outside source control. Do not store secrets in scripts or tracked configuration files.

Required backend variables:

```powershell
$env:ASPNETCORE_ENVIRONMENT = "Production"
$env:ASPNETCORE_URLS = "https://localhost:7251;http://localhost:5070"
$env:ConnectionStrings__DefaultConnection = "<set outside source control>"
$env:Cors__AllowedOrigins__0 = "https://localhost"
$env:AllowedHosts = "localhost"
```

Use real host names and origins for the deployed machine. Do not use wildcard CORS origins in production. Do not use `AllowedHosts=*` for customer deployments.

## SQL Server Setup

- Do not use the `sa` account.
- Create a dedicated SQL login or Windows account for BazarFlow, for example `bazarflow_app`.
- Grant only the permissions needed for the application and deployment.
- Apply migrations after configuring `ConnectionStrings__DefaultConnection`. For source-based deployments, use:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\update-database.ps1
```

For packaged deployments without the project source or .NET SDK, use the Database Migrator described below.

## Database Migrator

`BazarFlow.DbMigrator` is a small console tool that prepares or updates the SQL Server database by running the same EF Core migrations used by the backend. It is intended for client machines where the source code and `dotnet ef` are not available.

When `scripts\build-single-host.ps1` is used, the tool is published to:

```text
artifacts\single-host\tools\BazarFlow.DbMigrator.exe
```

Run it with a connection string:

```powershell
.\tools\BazarFlow.DbMigrator.exe --connection "<connection-string>"
```

The migrator can also read the connection string from environment variables, in this order:

```text
ConnectionStrings__DefaultConnection
BAZARFLOW_CONNECTION_STRING
```

The tool does not print the full connection string or password. It sanitizes password and user id fields in console output. Do not paste real production secrets into shared logs or screenshots.

The migrator performs these checks:

1. Verifies that a connection string is provided.
2. Opens a SQL Server connection to verify the server is reachable.
3. If the target database already exists and `PRODUCTS` exists, checks for duplicate product barcodes before migrations.
4. Runs `context.Database.Migrate()`.
5. Verifies core tables exist after migration: `EMPLOYEES`, `APP_SCREENS`, `PRODUCTS`, `CASH_SESSIONS`, `SUPPLIERS`, `PURCHASE_INVOICES`.

Exit codes:

| Code | Meaning |
|---:|---|
| 0 | Success. Database is ready. |
| 1 | Missing connection string. |
| 2 | SQL Server connection failed. |
| 3 | Migration failed or readiness checks failed. |
| 4 | Duplicate product barcodes block migration, including the unique barcode index migration. |
| 5 | Unexpected error. |

If exit code `4` is returned, the migrator prints up to the first 20 duplicate barcodes and counts. It does not delete, merge, or modify product data. Resolve the duplicates manually before rerunning migrations.

The migrator does not reset, drop, or seed the database. Installer automation and SQL Server installation are separate future steps. A self-contained publish can be added later if the target machine should not need a .NET runtime; it still does not require the .NET SDK.

Backups are written through SQL Server native backup. The SQL Server service account needs write access to the backup folder.

Default backup folder:

```text
C:\BazarFlowBackups
```

Prepare the folder:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\prepare-backup-folder.ps1
```

Grant access to the actual SQL Server service account:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\prepare-backup-folder.ps1 -SqlServiceAccount "NT SERVICE\MSSQL`$SQLEXPRESS"
```

Replace the account with the exact account from SQL Server Configuration Manager or Windows Services.

## Run Backend

Build the backend first, configure the required environment variables, then run:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\run-backend.ps1
```

The script runs the published executable from:

```text
artifacts\backend\Supermarket.Api.exe
```

Do not use `dotnet run` for deployment.

## Single-Host Mode (Recommended)

In Single-Host mode, the ASP.NET Core backend hosts both the web API and the Angular frontend static files from a single process on the same port. This eliminates the need for a separate web server (like IIS) and resolves all CORS configuration issues.

### Build and Package Single-Host

To build and package the application in Single-Host mode, run:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\build-single-host.ps1
```

This script:
1. Builds the Angular frontend with the production configuration.
2. Publishes the backend Release build to `artifacts\single-host`.
3. Publishes `BazarFlow.DbMigrator` to `artifacts\single-host\tools`.
4. Copies the built frontend assets into the `wwwroot` folder inside the published backend output (`artifacts\single-host\wwwroot`).

The completed package is located at:

```text
artifacts\single-host
```

### Environment Configuration in Single-Host Mode

In Single-Host mode, the frontend requests API endpoints from the same origin (protocol, host, and port) where it was loaded.
* **Frontend `environment.prod.ts`**: The `apiUrl` is set to `''` (an empty string). All requests are sent as relative paths (e.g., `/api/auth/login`).
* **CORS Settings**: Since requests are same-origin, CORS headers are not strictly required for local frontend access, but the backend maintains its safety policy.
* **Allowed Hosts**: Set `AllowedHosts = localhost` (or the specific local network hostname).

### Running in Single-Host Mode

To run the packaged application:
1. Set the required backend environment variables.
2. Run the executable directly from the output folder:

```powershell
Push-Location artifacts\single-host
$env:ASPNETCORE_ENVIRONMENT = "Production"
$env:ASPNETCORE_URLS = "http://localhost:5070"
$env:ConnectionStrings__DefaultConnection = "<your connection string>"
.\Supermarket.Api.exe
Pop-Location
```

3. Open a browser and navigate to `http://localhost:5070`. The backend will serve the frontend UI and route all SPA paths correctly.

## Windows Service Setup

To host BazarFlow Backend (API and Frontend static assets in Single-Host mode) as a Windows Service, follow these steps. This setup allows the application to start automatically with Windows and run in the background without needing a user to stay logged in or keep a console window open.

### 1. Build Single-Host Package First
The Windows Service requires the Single-Host executable. Build it first using the single-host build script:
```powershell
powershell -ExecutionPolicy Bypass -File scripts\build-single-host.ps1
```
This builds and packages both the frontend and backend to `artifacts\single-host`.

### 2. Install the Service
The installation script creates the service and configures its startup environment. Run an elevated (Administrator) PowerShell session and execute:
```powershell
# Set variables or let them fallback to environment defaults
powershell -ExecutionPolicy Bypass -File scripts\install-service.ps1 -ConnectionString "Server=localhost\SQLEXPRESS;Database=SupermarketDb;Trusted_Connection=True;TrustServerCertificate=True" -Urls "http://localhost:5070" -AllowedOrigins "http://localhost:5070" -AllowedHosts "localhost"
```

**Parameters supported by `install-service.ps1`**:
* `-ConnectionString`: The connection string for SQL Server. If not specified, it falls back to the current shell environment variable `ConnectionStrings__DefaultConnection`.
* `-Urls`: The hosting URLs for the service (default is `http://localhost:5070`).
* `-AllowedOrigins`: CORS allowed origins (default is `http://localhost:5070`).
* `-AllowedHosts`: Allowed hosts header verification (default is `localhost`).

This script registers the Windows Service named **`BazarFlow.Api`** (with Display Name: **`BazarFlow Backend Service`**) configured with **Automatic (Delayed Start)**. It also writes the environment configurations to the service's registry key and automatically grants folder write permission for backups (`C:\BazarFlowBackups`) to the service's security identifier (`NT AUTHORITY\LocalService`).

### 3. Service Management Scripts
Use the following administrative scripts (running in Administrator mode) to manage the service:

* **Start Service**:
  ```powershell
  powershell -ExecutionPolicy Bypass -File scripts\start-service.ps1
  ```
* **Stop Service**:
  ```powershell
  powershell -ExecutionPolicy Bypass -File scripts\stop-service.ps1
  ```
* **Restart Service**:
  ```powershell
  powershell -ExecutionPolicy Bypass -File scripts\restart-service.ps1
  ```
* **Uninstall Service**:
  ```powershell
  powershell -ExecutionPolicy Bypass -File scripts\uninstall-service.ps1
  ```

### 4. Create Desktop Shortcut

You can create a desktop shortcut named **BazarFlow** that opens the application URL in the default web browser with the custom brand icon.

To create the shortcut:
1. Ensure the Windows Service is running (run `scripts\start-service.ps1` first).
2. Run the shortcut creation script (does **not** require Administrator privileges):
   ```powershell
   powershell -ExecutionPolicy Bypass -File scripts\create-desktop-shortcut.ps1
   ```
3. Open BazarFlow at any time by double-clicking the new **BazarFlow** icon on your Desktop.

**Parameters supported by `create-desktop-shortcut.ps1`**:
* `-Url`: The application hosting URL (default is `http://localhost:5070`).
* `-ShortcutName`: The name of the shortcut file (default is `BazarFlow`).
* `-IconPath`: Path to the brand `.ico` file (default is `packaging/assets/bazarflow-icon.ico`).

## Installer Package (Inno Setup - Skeleton)

A basic installer configuration file is available at `packaging/inno/bazarflow-setup.iss`. This Inno Setup script compiles into a standalone `BazarFlowSetup.exe` installer for client deployments.

### What the Installer Does
1. **Requires Admin Rights**: Forces execution with Administrator privileges (`PrivilegesRequired=admin`) to allow proper folder creation and future service setup.
2. **Copies Files**:
   * Extracts single-host publish artifacts to `C:\Program Files\BazarFlow` (default).
   * Copies administration scripts (`install-service.ps1`, `uninstall-service.ps1`, etc.) to `C:\Program Files\BazarFlow\scripts`.
   * Copies the branding icon to `C:\Program Files\BazarFlow\packaging\assets`.
3. **Collects Database Settings**:
   * SQL Server name, database name, authentication mode, optional SQL username/password, and application port.
4. **Runs Database Migrator**:
   * Runs `BazarFlow.DbMigrator.exe` from the installed `tools` folder before completing installation.
   * Passes the generated connection string through a temporary process environment variable.
   * Does not pass the connection string on the command line and does not write it to a permanent installer file.
5. **Creates Shortcuts**:
   * Adds a Start Menu shortcut under **BazarFlow** pointing to `http://localhost:{selected-port}`.
   * Optionally adds a Desktop shortcut pointing to `http://localhost:{selected-port}` using the custom logo.

### V2-02 Installer SQL Wizard + DbMigrator

The installer now prepares or updates the database through the Database Migrator. SQL Server is still a prerequisite and is not installed automatically.

Wizard fields:

- SQL Server, default `localhost\SQLEXPRESS`.
- Database name, default `BazarFlow`.
- Authentication mode: Windows Authentication or SQL Authentication.
- SQL username and masked password when SQL Authentication is selected.
- Application port, default `5070`.

The installer validates required fields before database initialization. Port values must be numeric and between `1` and `65535`.

Connection string formats generated at install time:

```text
Server=<server>;Database=<db>;Trusted_Connection=True;TrustServerCertificate=True;Encrypt=False;
Server=<server>;Database=<db>;User Id=<username>;Password=<password>;TrustServerCertificate=True;Encrypt=False;
```

The generated connection string exists only in installer runtime memory and as a temporary process environment variable inherited by `BazarFlow.DbMigrator.exe`. It is not stored in the repository, not written to `.iss`, not written to a permanent config file by this installer phase, and not passed through command-line arguments.

Migrator exit codes shown by the installer:

| Code | Installer behavior |
|---:|---|
| 0 | Database is ready; installation continues. |
| 1 | Missing migrator connection string; installation stops. |
| 2 | SQL Server connection failed; check server and credentials. |
| 3 | Database initialization or migration failed. |
| 4 | Duplicate product barcodes block migration; no data was deleted. |
| 5 | Unexpected database preparation error. |

If the migrator fails, installation stops. The installer does not delete data or reset the database.

### V2-02D Automated Windows Service Install + Start

After the SQL wizard and successful database migration, the installer now performs the local service setup automatically:

```text
SQL Wizard -> DbMigrator -> Install Service -> Start Service -> Health Check
```

The installer runs:

```text
{app}\scripts\install-service.ps1
{app}\scripts\start-service.ps1
```

The connection string is not passed on the PowerShell command line. The installer sets `ConnectionStrings__DefaultConnection` only as a temporary process environment variable before invoking `install-service.ps1`. The service installation script then writes the service runtime environment to the Windows Service registry key so the service can start after reboot.

The installer also sets these values for the service:

```text
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://localhost:{selected-port}
Cors__AllowedOrigins__0=http://localhost:{selected-port}
AllowedHosts=localhost
```

Before starting the service, the installer checks whether the selected local port is available. If the port is already in use, installation stops with a clear message and the database is not deleted or reset.

After starting `BazarFlow.Api`, the installer verifies that the service is `Running`, then checks:

```text
http://localhost:{selected-port}/api/setup/status
```

The health check retries for a short period. If the service is running but the health check does not respond, the installer shows a warning. No database or backup files are deleted.

### Post-Installation Service Registration
At this stage (V2-02D), the installer installs and starts the Windows Service automatically. Manual service commands are only needed for troubleshooting or advanced maintenance.

To reinstall the service manually from the installation directory:

   ```powershell
   cd "C:\Program Files\BazarFlow"
   powershell -ExecutionPolicy Bypass -File scripts\install-service.ps1 -ConnectionString "Your_SQL_Connection_String"
   ```

To start the service manually:

   ```powershell
   powershell -ExecutionPolicy Bypass -File scripts\start-service.ps1
   ```

### Uninstall Behavior

During uninstall, the installer runs:

```text
{app}\scripts\uninstall-service.ps1
```

Uninstall removes the Windows Service and application files. It does not delete:

- the SQL Server database,
- backups,
- `C:\BazarFlowBackups`.

If automatic service removal fails, the installer shows a warning. Database and backup files are still preserved.

## Serve Frontend (Alternative via IIS)

Serve the files in:

```text
artifacts\frontend
```

Use IIS or another static file server. If IIS is used with Angular routes, configure fallback routing to `index.html`.

Do not use `ng serve` for production or customer deployments.

## First-Run Setup

After the backend and frontend are running:

1. Open the frontend `/setup` route.
2. Create the first administrator account.
3. Create the first POS device.
4. Go to the login page.
5. Sign in with the administrator account.

## Smoke Tests

Verify these flows after deployment:

- Login and logout.
- Cashier sale.
- Products.
- Inventory.
- Purchases.
- Reports and profit views.
- Backup creation.
- Audit logs.
- Device management.

## Troubleshooting

### Installer

- **SQL connection failed**: Confirm SQL Server is installed and running, the instance name is correct, and the selected authentication mode is valid.
- **Port used**: Choose another port in the installer. Another process is already listening on the selected local port.
- **Service install failed**: Run the installer as Administrator and check that `Supermarket.Api.exe` and the `scripts` folder exist under the installation directory.
- **Service start failed**: Open Windows Event Viewer -> Windows Logs -> Application and inspect errors from `BazarFlow.Api`.
- **Health check failed**: The service may still be starting. Try opening `http://localhost:{selected-port}` after a few moments, then check Event Viewer if it still does not load.
- **Duplicate product barcode**: Resolve duplicate rows in `PRODUCTS` before running the installer again. The installer does not delete or merge data.

### SQL Connection

- Confirm SQL Server is running.
- Confirm the instance name is correct.
- Confirm `ConnectionStrings__DefaultConnection` is set in the same environment used to run the backend.
- Confirm the application SQL user is not `sa`.
- Review SQL encryption and certificate settings for the target machine.

### CORS Error

- Confirm `Cors__AllowedOrigins__0` exactly matches the frontend origin.
- Include scheme and port, for example `https://localhost` or `https://pos.example.local:443`.
- Do not use wildcard origins in production.
- Restart the backend after changing environment variables.

### Backup Permission

- Confirm `C:\BazarFlowBackups` exists.
- Confirm the SQL Server service account has write or modify access.
- Remember that SQL Server writes the `.bak` file, not only the ASP.NET Core process.

### HTTPS / Certificate

- Confirm the backend URL in `ASPNETCORE_URLS` matches the certificate configuration.
- For local or beta deployments, document whether `TrustServerCertificate=True` is acceptable for SQL Server.
- Production HTTPS certificates must be configured outside these scripts.

### Firewall

- Confirm Windows Firewall allows the backend port.
- Confirm frontend users can reach the static hosting port.
- Confirm SQL Server remote access only when required and properly restricted.

### Windows Service Failures

#### Service does not start or crashes immediately
- Open **Windows Event Viewer** -> **Windows Logs** -> **Application** to view the runtime exceptions.
- Check if `artifacts\single-host\Supermarket.Api.exe` exists. If not, run `build-single-host.ps1`.
- Verify the Content Root directory path. The API uses `AppContext.BaseDirectory` so configuration files (`appsettings.json`, etc.) and `wwwroot` are loaded from the service installation folder.

#### Missing environment variables
- Environment variables for the Windows Service are stored in the registry path: `HKLM:\SYSTEM\CurrentControlSet\Services\BazarFlow.Api` under the multi-string property `Environment`.
- To inspect or fix them, run `regedit.exe`, navigate to the path, check the `Environment` value, and restart the service.

#### SQL Server connection failure on startup
- If SQL Server is installed on the same machine, the BazarFlow service might start before SQL Server is fully initialized during Windows startup.
- The installation script configures the service with **Automatic (Delayed Start)**, which delays startup by about 2 minutes after boot to allow SQL Server to start.
- Ensure the connection string includes `TrustServerCertificate=True` if using SSL/TLS, and that the database server allows local logins for the service account if needed.

#### Port already in use
- If you get an exception that a port is already in use, check if another process (e.g. IIS, another instance of the backend, or another service) is running on the port configured in `ASPNETCORE_URLS`.
- Run `netstat -ano | findstr <port>` to identify the process using that port.

#### Backup folder permissions
- The Windows Service runs under the low-privilege `NT AUTHORITY\LocalService` account.
- The `install-service.ps1` script automatically grants `LocalService` modify permission on `C:\BazarFlowBackups`. If you change the backup directory location in configuration, ensure you manually grant NTFS modify access to `NT AUTHORITY\LocalService` on the new path using:
  ```powershell
  icacls "C:\YourNewBackupPath" /grant "NT AUTHORITY\LocalService:(OI)(CI)M" /T /C
  ```
