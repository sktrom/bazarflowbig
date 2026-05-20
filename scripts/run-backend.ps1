$ErrorActionPreference = "Stop"

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Split-Path -Parent $scriptRoot
$artifactPath = Join-Path $repoRoot "artifacts\backend"
$apiExecutable = Join-Path $artifactPath "Supermarket.Api.exe"

$requiredEnvironmentVariables = @(
    "ASPNETCORE_ENVIRONMENT",
    "ConnectionStrings__DefaultConnection",
    "Cors__AllowedOrigins__0",
    "AllowedHosts"
)

if (-not (Test-Path $apiExecutable)) {
    Write-Error "Published backend executable was not found at: $apiExecutable. Run scripts\build-backend.ps1 first."
}

foreach ($name in $requiredEnvironmentVariables) {
    $value = [Environment]::GetEnvironmentVariable($name)
    if ([string]::IsNullOrWhiteSpace($value)) {
        Write-Error "$name is not set. Configure required production environment variables before running the backend."
    }
}

Write-Host "Starting BazarFlow backend from:"
Write-Host $apiExecutable

Push-Location $artifactPath
try {
    & $apiExecutable
}
finally {
    Pop-Location
}
