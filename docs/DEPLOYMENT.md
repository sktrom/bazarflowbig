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
- Apply migrations after configuring `ConnectionStrings__DefaultConnection`:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\update-database.ps1
```

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
3. Copies the built frontend assets into the `wwwroot` folder inside the published backend output (`artifacts\single-host\wwwroot`).

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
