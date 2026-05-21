param(
    [string]$BackupDirectory = "C:\BazarFlowBackups"
)

$ErrorActionPreference = "Stop"

Write-Host "BazarFlow backup files"
Write-Host "Directory: $BackupDirectory"

if (-not (Test-Path -LiteralPath $BackupDirectory -PathType Container)) {
    Write-Host "Backup directory does not exist."
    exit 0
}

$files = Get-ChildItem -LiteralPath $BackupDirectory -Filter "*.bak" -File |
    Sort-Object LastWriteTime -Descending

if (-not $files) {
    Write-Host "No .bak files found."
    exit 0
}

$files | Select-Object `
    @{Name = "FileName"; Expression = { $_.Name } },
    @{Name = "SizeMB"; Expression = { [Math]::Round($_.Length / 1MB, 2) } },
    @{Name = "LastWriteTime"; Expression = { $_.LastWriteTime } } |
    Format-Table -AutoSize
