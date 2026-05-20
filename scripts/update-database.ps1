$ErrorActionPreference = "Stop"

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Split-Path -Parent $scriptRoot
$backendRoot = Join-Path $repoRoot "backend"

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Write-Error ".NET SDK was not found. Install the required .NET SDK before updating the database."
}

if ([string]::IsNullOrWhiteSpace($env:ConnectionStrings__DefaultConnection)) {
    Write-Error "ConnectionStrings__DefaultConnection is not set. Set it in the current environment before running database migrations."
}

Push-Location $backendRoot
try {
    Write-Host "Applying Entity Framework Core migrations..."
    dotnet ef database update --project "src\Supermarket.Infrastructure" --startup-project "src\Supermarket.Api"
}
finally {
    Pop-Location
}
