param(
    [string]$Url = "http://localhost:5070",
    [string]$ShortcutName = "BazarFlow",
    [string]$IconPath = "packaging/assets/bazarflow-icon.ico"
)

$ErrorActionPreference = "Stop"

# Get Repo Root
$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Split-Path -Parent $scriptRoot

# If IconPath is relative, make it absolute relative to repo root
$absoluteIconPath = $IconPath
if (-not [System.IO.Path]::IsPathRooted($absoluteIconPath)) {
    $absoluteIconPath = [System.IO.Path]::GetFullPath((Join-Path $repoRoot $IconPath))
}

# 1. Check if Icon Exists
$hasIcon = $true
if (-not (Test-Path $absoluteIconPath)) {
    Write-Warning "Icon file not found at: $absoluteIconPath"
    $hasIcon = $false
} else {
    Write-Host "Using icon file at: $absoluteIconPath"
}

# 2. Determine Desktop Path
$desktopPath = [System.Environment]::GetFolderPath("Desktop")
$shortcutFile = Join-Path $desktopPath "$ShortcutName.lnk"

Write-Host "Creating desktop shortcut at: $shortcutFile"

# 3. Create Shortcut using COM
try {
    $wshShell = New-Object -ComObject WScript.Shell
    $shortcut = $wshShell.CreateShortcut($shortcutFile)
    
    # We use explorer.exe to launch the URL so we can assign a custom icon to the .lnk file
    $shortcut.TargetPath = "explorer.exe"
    $shortcut.Arguments = "`"$Url`""
    $shortcut.Description = "Launch BazarFlow Application"
    
    if ($hasIcon) {
        $shortcut.IconLocation = "$absoluteIconPath,0"
    }
    
    $shortcut.Save()
    Write-Host "Shortcut created successfully!"
}
catch {
    Write-Error "Failed to create desktop shortcut: $_"
    exit 1
}
