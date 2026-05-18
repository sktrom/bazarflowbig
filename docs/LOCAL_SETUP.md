# Local Setup Guide

## Requirements
* Appropriate .NET SDK (.NET 9)
* Node.js
* Angular CLI (if required globally)
* SQL Server

## Backend Commands
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

## Test Credentials
* **Username:** `admin`
* **Password:** `admin123`
* **Device Code:** `DEFAULT_DEVICE`

> **Note:** The `deviceCode` is fixed to `DEFAULT_DEVICE` inside the UI for V1.
