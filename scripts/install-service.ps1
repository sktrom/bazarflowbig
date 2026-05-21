param(
    [string]$ConnectionString = $env:ConnectionStrings__DefaultConnection,
    [string]$Urls = "http://localhost:5070",
    [string]$AllowedOrigins = "http://localhost:5070",
    [string]$AllowedHosts = "localhost"
)

$ErrorActionPreference = "Stop"

# 1. Check Administrator Privileges
$currentUser = [Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()
if (-not $currentUser.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    Write-Error "This script must be run as Administrator (Elevated PowerShell session)."
    exit 1
}

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Split-Path -Parent $scriptRoot
$binPath = Join-Path $repoRoot "artifacts\single-host\Supermarket.Api.exe"

# 2. Check if Executable Exists
if (-not (Test-Path $binPath)) {
    Write-Error "BazarFlow single-host executable was not found at: $binPath. Please run scripts\build-single-host.ps1 first."
    exit 1
}

# 3. Check for Connection String
if ([string]::IsNullOrWhiteSpace($ConnectionString)) {
    Write-Error "ConnectionString is required. Pass it via -ConnectionString or set the ConnectionStrings__DefaultConnection environment variable."
    exit 1
}

$serviceName = "BazarFlow.Api"
$displayName = "BazarFlow Backend Service"
$description = "Hosts the BazarFlow API and static frontend UI files in Single-Host Mode."

# 4. Remove existing service if it exists to allow clean re-installation
if (Get-Service -Name $serviceName -ErrorAction SilentlyContinue) {
    Write-Host "Service $serviceName already exists. Stopping and removing it first..."
    Stop-Service -Name $serviceName -Force -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 2
    # Use sc.exe delete to remove the service definition
    sc.exe delete $serviceName | Out-Null
    Start-Sleep -Seconds 1
}

# 5. Create the Windows Service
Write-Host "Creating Windows Service: $displayName ($serviceName)..."
$service = New-Service -Name $serviceName -BinaryPathName "`"$binPath`"" -DisplayName $displayName -Description $description -StartupType Automatic

# 6. Configure Delayed Auto-Start
Write-Host "Configuring delayed auto-start..."
try {
    Set-Service -Name $serviceName -StartupType AutomaticDelayed
}
catch {
    sc.exe config $serviceName start= delayed-auto | Out-Null
}

# 7. Configure Environment Variables in Service Registry Key
Write-Host "Writing environment configurations to Windows Registry..."
$regPath = "HKLM:\SYSTEM\CurrentControlSet\Services\$serviceName"
$envVars = @(
    "ASPNETCORE_ENVIRONMENT=Production",
    "ASPNETCORE_URLS=$Urls",
    "ConnectionStrings__DefaultConnection=$ConnectionString",
    "Cors__AllowedOrigins__0=$AllowedOrigins",
    "AllowedHosts=$AllowedHosts"
)
Set-ItemProperty -Path $regPath -Name "Environment" -Value $envVars -PropertyType MultiString

# 8. Set folder permissions for the LocalService account (least privilege service account)
Write-Host "Configuring NTFS folder permissions..."
$backupDir = "C:\BazarFlowBackups"
if (-not (Test-Path $backupDir)) {
    New-Item -ItemType Directory -Path $backupDir -Force | Out-Null
}

# Grant NT AUTHORITY\LocalService modify access to the backup folder
icacls.exe $backupDir /grant "NT AUTHORITY\LocalService:(OI)(CI)M" /T /C | Out-Null

Write-Host "Service installed successfully!"
Write-Host "Use scripts\start-service.ps1 to start the service, or check services.msc."
