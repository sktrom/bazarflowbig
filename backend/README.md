# Supermarket Backend Foundation

This contains the backend solution for the Supermarket project built on ASP.NET Core Web API following a Clean Architecture pattern.

## Bootstrap Instructions
Run the bootstrap script from the `scripts` folder to automatically scaffold the entire robust solution, set up clean architecture references, aggressively clean up default Web API templates (like WeatherForecast), and lay down the exact foundational placeholders (minimal Program.cs, DBContext Skeleton).

```powershell
cd scripts
.\bootstrap-backend-foundation.ps1
```

## Standard Commands

**1. Restore dependencies:**
```powershell
dotnet restore
```

**2. Build the solution:**
```powershell
dotnet build
```

**3. Run unit, integration, and API tests:**
```powershell
dotnet test
```

**4. Run API locally:**
```powershell
cd src/Supermarket.Api
dotnet run
```
