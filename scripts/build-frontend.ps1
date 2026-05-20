$ErrorActionPreference = "Stop"

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Split-Path -Parent $scriptRoot
$frontendRoot = Join-Path $repoRoot "frontend"
$environmentProd = Join-Path $frontendRoot "src\environments\environment.prod.ts"
$distPath = Join-Path $frontendRoot "dist\supermarket-frontend"
$artifactPath = Join-Path $repoRoot "artifacts\frontend"

if (-not (Get-Command node -ErrorAction SilentlyContinue)) {
    Write-Error "Node.js was not found. Install Node.js before building the frontend."
}

if (-not (Get-Command npm -ErrorAction SilentlyContinue)) {
    Write-Error "npm was not found. Install npm before building the frontend."
}

if (-not (Test-Path $environmentProd)) {
    Write-Error "Production environment file was not found at: $environmentProd"
}

$environmentContent = Get-Content -Path $environmentProd -Raw
if ($environmentContent -match "YOUR_API_HOST") {
    Write-Error "environment.prod.ts still contains YOUR_API_HOST. Set the production API URL before building."
}

Push-Location $frontendRoot
try {
    if (Test-Path "package-lock.json") {
        Write-Host "Installing frontend packages with npm ci..."
        npm ci
    }
    else {
        Write-Host "Installing frontend packages with npm install..."
        npm install
    }

    Write-Host "Building Angular production bundle..."
    npx ng build --configuration production

    if (-not (Test-Path $distPath)) {
        Write-Error "Angular dist output was not found at: $distPath"
    }

    if (Test-Path $artifactPath) {
        Remove-Item -Path $artifactPath -Recurse -Force
    }

    New-Item -ItemType Directory -Path $artifactPath -Force | Out-Null
    Copy-Item -Path (Join-Path $distPath "*") -Destination $artifactPath -Recurse -Force

    Write-Host "Frontend artifacts written to:"
    Write-Host $artifactPath
}
finally {
    Pop-Location
}
