# Local Setup Guide

## Requirements
* Appropriate .NET SDK (.NET 9)
* Node.js
* Angular CLI (if required globally)
* SQL Server

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

## Frontend Commands
Open a new terminal and run:
```bash
cd frontend
npm install
npx ng build --configuration development
npx ng test
npx ng serve
```

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
