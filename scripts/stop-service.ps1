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
    Write-Warning "Service $serviceName is not installed."
    exit 0
}

# 3. Stop Service if Running
if ($service.Status -ne "Stopped") {
    Write-Host "Stopping service $serviceName..."
    Stop-Service -Name $serviceName -Force
    # Wait for the status to update
    $service.Refresh()
    $timeout = 10
    $elapsed = 0
    while ($service.Status -ne "Stopped" -and $elapsed -lt $timeout) {
        Start-Sleep -Seconds 1
        $service.Refresh()
        $elapsed++
    }
}

# 4. Display Final Status
if ($service.Status -eq "Stopped") {
    Write-Host "Service $serviceName has been stopped successfully."
} else {
    Write-Warning "Service $serviceName current status is: $($service.Status)."
}
