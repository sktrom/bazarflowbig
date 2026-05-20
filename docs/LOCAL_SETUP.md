# Local Setup Guide

## Requirements
* Appropriate .NET SDK (.NET 9)
* Node.js
* Angular CLI (if required globally)
* SQL Server

## Local Development vs Deployment

Local development uses source projects directly:

* `dotnet run` starts the backend from source for development only.
* `ng serve` starts the Angular dev server for development only.
* Development CORS allows the Angular dev server origins.

Deployment uses published artifacts:

* backend is published with `scripts\build-backend.ps1` into `artifacts\backend`.
* frontend is built with `scripts\build-frontend.ps1` into `artifacts\frontend`.
* backend is started from `artifacts\backend\Supermarket.Api.exe` through `scripts\run-backend.ps1`.
* frontend files are served by IIS or another static file server.
* production settings must be provided through environment variables, not user-secrets or tracked files.

## Backend Commands
`appsettings.json` does not store the database password. Configure the local
connection string with user-secrets before running migrations or the API:

```bash
cd backend
dotnet user-secrets init --project src\Supermarket.Api
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost\SQLEXPRESS;Database=SupermarketDb;User Id=bazarflow_app;Password=<local-password>;TrustServerCertificate=True;Encrypt=False;" --project src\Supermarket.Api
```

Alternative for a terminal-only session:

```bash
set ConnectionStrings__DefaultConnection=Server=localhost\SQLEXPRESS;Database=SupermarketDb;User Id=bazarflow_app;Password=<local-password>;TrustServerCertificate=True;Encrypt=False;
```

Run the following commands in the terminal:
```bash
cd backend
dotnet restore
dotnet build
dotnet test tests\Supermarket.UnitTests
dotnet test tests\Supermarket.IntegrationTests
dotnet ef database update --project src/Supermarket.Infrastructure --startup-project src/Supermarket.Api
dotnet run --project src\Supermarket.Api
```

`dotnet run` is for local development only. Use the published executable in
`artifacts\backend` for deployment.

## Frontend Commands
Open a new terminal and run:
```bash
cd frontend
npm install
npx ng build --configuration development
npx ng test
npx ng serve
```

`ng serve` is for local development only. Use `npx ng build --configuration production`
or `scripts\build-frontend.ps1` for deployment, then serve the generated static files.

## System URLs
* **Frontend UI:** http://localhost:4200
* **Backend API:** http://localhost:5070

## Local CORS / Hosts
Local development allows the Angular dev server origins:

* `http://localhost:4200`
* `https://localhost:4200`

Development configuration may use `AllowedHosts: "*"`. Production must set
explicit hosts and CORS origins through configuration or environment variables.

## Test Credentials
Use the first-run setup wizard to create the first administrator account and POS device.

> **Note:** `Encrypt=False` is acceptable only for local SQL Server Express development.
> For production or paid beta deployments, configure the connection string through
> environment variables, use a dedicated SQL user, and review SQL encryption settings.
