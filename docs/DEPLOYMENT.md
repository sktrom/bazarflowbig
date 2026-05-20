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

## Serve Frontend

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
