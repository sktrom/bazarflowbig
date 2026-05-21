$ErrorActionPreference = "Stop"

# 1. Check Administrator Privileges
$currentUser = [Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()
if (-not $currentUser.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    Write-Error "This script must be run as Administrator (Elevated PowerShell session)."
    exit 1
}

$serviceName = "BazarFlow.Api"

# 2. Check if Service Exists
$service = Get-Service -Name $serviceName -ErrorAction SilentlyContinue
if (-not $service) {
    Write-Host "Service $serviceName is not installed."
    exit 0
}

# 3. Stop Service if Running
if ($service.Status -eq "Running") {
    Write-Host "Stopping service $serviceName..."
    Stop-Service -Name $serviceName -Force
    Start-Sleep -Seconds 2
}

# 4. Delete the Service
Write-Host "Deleting service $serviceName..."
sc.exe delete $serviceName | Out-Null
Start-Sleep -Seconds 1

# 5. Verify Removal
if (-not (Get-Service -Name $serviceName -ErrorAction SilentlyContinue)) {
    Write-Host "Service $serviceName has been successfully uninstalled."
} else {
    Write-Warning "Service $serviceName was marked for deletion but is still present. A system reboot may be required to clear it."
}
