$ErrorActionPreference = "Stop"

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Split-Path -Parent $scriptRoot
$frontendRoot = Join-Path $repoRoot "frontend"
$backendRoot = Join-Path $repoRoot "backend"
$apiProject = Join-Path $backendRoot "src\Supermarket.Api\Supermarket.Api.csproj"
$outputPath = Join-Path $repoRoot "artifacts\single-host"
$frontendDist = Join-Path $repoRoot "artifacts\frontend"

Write-Host "Checking prerequisites..."
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Write-Error ".NET SDK was not found. Install the required .NET SDK."
}
if (-not (Get-Command node -ErrorAction SilentlyContinue)) {
    Write-Error "Node.js was not found. Install Node.js."
}
if (-not (Get-Command npm -ErrorAction SilentlyContinue)) {
    Write-Error "npm was not found. Install npm."
}

# Clean output directories
Write-Host "Cleaning old artifacts..."
if (Test-Path $outputPath) {
    Remove-Item -Path $outputPath -Recurse -Force
}
New-Item -ItemType Directory -Path $outputPath -Force | Out-Null

# Build Frontend
Write-Host "Building Angular frontend..."
$buildFrontendScript = Join-Path $scriptRoot "build-frontend.ps1"
if (Test-Path $buildFrontendScript) {
    & $buildFrontendScript
} else {
    Write-Error "build-frontend.ps1 not found at $buildFrontendScript"
}

if (-not (Test-Path $frontendDist)) {
    Write-Error "Frontend build did not produce artifacts at $frontendDist"
}

# Publish Backend
Write-Host "Publishing ASP.NET Core backend..."
Push-Location $backendRoot
try {
    dotnet restore
    dotnet publish "src\Supermarket.Api\Supermarket.Api.csproj" -c Release -o $outputPath
}
finally {
    Pop-Location
}

# Copy Frontend assets to backend wwwroot
Write-Host "Packaging frontend assets into backend wwwroot..."
$wwwrootPath = Join-Path $outputPath "wwwroot"
if (Test-Path $wwwrootPath) {
    Remove-Item -Path $wwwrootPath -Recurse -Force
}
New-Item -ItemType Directory -Path $wwwrootPath -Force | Out-Null

Copy-Item -Path (Join-Path $frontendDist "*") -Destination $wwwrootPath -Recurse -Force

Write-Host "Single-host build completed successfully!"
Write-Host "Artifacts location: $outputPath"
