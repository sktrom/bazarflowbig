# bootstrap-backend-foundation.ps1
# Script to scaffold the Supermarket .NET Backend Foundation

$ErrorActionPreference = "Stop"

$BackendDir = Resolve-Path -Path "..\backend" -ErrorAction SilentlyContinue
if (!$BackendDir) {
    New-Item -ItemType Directory -Force -Path "..\backend" | Out-Null
    $BackendDir = Resolve-Path -Path "..\backend"
}
Set-Location -Path $BackendDir.Path

Write-Host "1. Creating Solution and Projects..."
dotnet new sln -n Supermarket --force

dotnet new webapi -n Supermarket.Api -o src/Supermarket.Api --use-controllers --force
dotnet new classlib -n Supermarket.Application -o src/Supermarket.Application --force
dotnet new classlib -n Supermarket.Domain -o src/Supermarket.Domain --force
dotnet new classlib -n Supermarket.Infrastructure -o src/Supermarket.Infrastructure --force
dotnet new classlib -n Supermarket.Contracts -o src/Supermarket.Contracts --force

dotnet new xunit -n Supermarket.UnitTests -o tests/Supermarket.UnitTests --force
dotnet new xunit -n Supermarket.IntegrationTests -o tests/Supermarket.IntegrationTests --force
dotnet new xunit -n Supermarket.ApiTests -o tests/Supermarket.ApiTests --force

Write-Host "2. Adding projects to solution..."
dotnet sln add src/Supermarket.Api
dotnet sln add src/Supermarket.Application
dotnet sln add src/Supermarket.Domain
dotnet sln add src/Supermarket.Infrastructure
dotnet sln add src/Supermarket.Contracts
dotnet sln add tests/Supermarket.UnitTests
dotnet sln add tests/Supermarket.IntegrationTests
dotnet sln add tests/Supermarket.ApiTests

Write-Host "3. Configuring Project References..."
dotnet add src/Supermarket.Application reference src/Supermarket.Domain src/Supermarket.Contracts
dotnet add src/Supermarket.Infrastructure reference src/Supermarket.Application src/Supermarket.Domain
dotnet add src/Supermarket.Api reference src/Supermarket.Application src/Supermarket.Infrastructure src/Supermarket.Contracts
dotnet add tests/Supermarket.UnitTests reference src/Supermarket.Application src/Supermarket.Domain
dotnet add tests/Supermarket.IntegrationTests reference src/Supermarket.Infrastructure
dotnet add tests/Supermarket.ApiTests reference src/Supermarket.Api

Write-Host "3.5 Adding required Nuget packages for foundational placeholders..."
dotnet add src/Supermarket.Application package Microsoft.Extensions.DependencyInjection.Abstractions
dotnet add src/Supermarket.Infrastructure package Microsoft.Extensions.DependencyInjection.Abstractions
dotnet add src/Supermarket.Infrastructure package Microsoft.Extensions.Configuration.Abstractions
dotnet add src/Supermarket.Infrastructure package Microsoft.EntityFrameworkCore
dotnet add src/Supermarket.Infrastructure package Microsoft.EntityFrameworkCore.SqlServer
dotnet add src/Supermarket.Api package Swashbuckle.AspNetCore
Write-Host "4. Cleaning up default template files..."
$ApiDir = "src/Supermarket.Api"
if (Test-Path "$ApiDir/WeatherForecast.cs") { Remove-Item "$ApiDir/WeatherForecast.cs" -Force }
if (Test-Path "$ApiDir/Controllers") { Remove-Item "$ApiDir/Controllers" -Recurse -Force }
if (Test-Path "$ApiDir/Supermarket.Api.http") { Remove-Item "$ApiDir/Supermarket.Api.http" -Force }
if (Test-Path "src/Supermarket.Application/Class1.cs") { Remove-Item "src/Supermarket.Application/Class1.cs" -Force }
if (Test-Path "src/Supermarket.Domain/Class1.cs") { Remove-Item "src/Supermarket.Domain/Class1.cs" -Force }
if (Test-Path "src/Supermarket.Infrastructure/Class1.cs") { Remove-Item "src/Supermarket.Infrastructure/Class1.cs" -Force }
if (Test-Path "src/Supermarket.Contracts/Class1.cs") { Remove-Item "src/Supermarket.Contracts/Class1.cs" -Force }

Write-Host "5. Writing foundational structure files..."

$ProgramContent = @"
using Supermarket.Application;
using Supermarket.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Register layer DI extensions
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();
"@
Set-Content -Path "$ApiDir/Program.cs" -Value $ProgramContent -Encoding UTF8

$AppConfigContent = @"
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=SupermarketDb;Trusted_Connection=True;MultipleActiveResultSets=true;Encrypt=Optional"
  }
}
"@
Set-Content -Path "$ApiDir/appsettings.json" -Value $AppConfigContent -Encoding UTF8

$AppConfigDevContent = @"
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
"@
Set-Content -Path "$ApiDir/appsettings.Development.json" -Value $AppConfigDevContent -Encoding UTF8

$AppDiContent = @"
using Microsoft.Extensions.DependencyInjection;

namespace Supermarket.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            // Placeholder for Application services
            return services;
        }
    }
}
"@
Set-Content -Path "src/Supermarket.Application/DependencyInjection.cs" -Value $AppDiContent -Encoding UTF8

$InfraDiContent = @"
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Supermarket.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
        {
            // Placeholder for Infrastructure services
            // services.AddDbContext<SupermarketDbContext>(options =>
            //     options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));
            
            return services;
        }
    }
}
"@
Set-Content -Path "src/Supermarket.Infrastructure/DependencyInjection.cs" -Value $InfraDiContent -Encoding UTF8

$DbContextDir = "src/Supermarket.Infrastructure/Persistence"
if (!(Test-Path $DbContextDir)) { New-Item -ItemType Directory -Force -Path $DbContextDir | Out-Null }
$DbContextContent = @"
using Microsoft.EntityFrameworkCore;

namespace Supermarket.Infrastructure.Persistence
{
    public class SupermarketDbContext : DbContext
    {
        public SupermarketDbContext(DbContextOptions<SupermarketDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }
    }
}
"@
Set-Content -Path "$DbContextDir/SupermarketDbContext.cs" -Value $DbContextContent -Encoding UTF8

Write-Host "Backend foundation scaffolded and cleaned successfully!"
