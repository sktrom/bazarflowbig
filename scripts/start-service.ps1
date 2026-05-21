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
    Write-Error "Service $serviceName is not installed. Please run scripts\install-service.ps1 first."
    exit 1
}

# 3. Start Service if Stopped
if ($service.Status -ne "Running") {
    Write-Host "Starting service $serviceName..."
    Start-Service -Name $serviceName
    # Wait for the status to update
    $service.Refresh()
    $timeout = 10
    $elapsed = 0
    while ($service.Status -ne "Running" -and $elapsed -lt $timeout) {
        Start-Sleep -Seconds 1
        $service.Refresh()
        $elapsed++
    }
}

# 4. Display Final Status
if ($service.Status -eq "Running") {
    Write-Host "Service $serviceName is running successfully."
} else {
    Write-Error "Failed to start service $serviceName. Current status: $($service.Status). Check Windows Event Viewer for details."
    exit 1
}
