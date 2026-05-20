$ErrorActionPreference = "Stop"

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Split-Path -Parent $scriptRoot
$backendRoot = Join-Path $repoRoot "backend"
$apiProject = Join-Path $backendRoot "src\Supermarket.Api\Supermarket.Api.csproj"
$outputPath = Join-Path $repoRoot "artifacts\backend"

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Write-Error ".NET SDK was not found. Install the required .NET SDK before building the backend."
}

if (-not (Test-Path $apiProject)) {
    Write-Error "API project was not found at: $apiProject"
}

Push-Location $backendRoot
try {
    Write-Host "Restoring backend packages..."
    dotnet restore

    Write-Host "Publishing backend Release build..."
    dotnet publish "src\Supermarket.Api\Supermarket.Api.csproj" -c Release -o "..\artifacts\backend"

    Write-Host "Backend artifacts written to:"
    Write-Host $outputPath
}
finally {
    Pop-Location
}
