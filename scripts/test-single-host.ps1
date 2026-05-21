$ErrorActionPreference = "Stop"

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Split-Path -Parent $scriptRoot
$singleHostDir = Join-Path $repoRoot "artifacts\single-host"
$apiExe = Join-Path $singleHostDir "Supermarket.Api.exe"

if ([string]::IsNullOrWhiteSpace($env:ConnectionStrings__DefaultConnection)) {
    $env:ConnectionStrings__DefaultConnection = "Server=(localdb)\mssqllocaldb;Database=SupermarketDb;Trusted_Connection=True;MultipleActiveResultSets=true;Encrypt=False;TrustServerCertificate=True;"
}

$env:ASPNETCORE_ENVIRONMENT = "Production"
$env:ASPNETCORE_URLS = "http://localhost:5070"
$env:AllowedHosts = "localhost"
$env:Cors__AllowedOrigins__0 = "http://localhost:5070"

Write-Host "Starting BazarFlow in Single-Host mode..."
Write-Host "URL: http://localhost:5070"
Write-Host "Connection: $env:ConnectionStrings__DefaultConnection"

Push-Location $singleHostDir
try {
    & $apiExe
}
finally {
    Pop-Location
}
