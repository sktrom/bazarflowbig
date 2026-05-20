param(
    [string]$SqlServiceAccount,
    [string]$BackupDirectory = "C:\BazarFlowBackups"
)

$ErrorActionPreference = "Stop"

Write-Host "Preparing BazarFlow backup directory:"
Write-Host $BackupDirectory

if (-not (Test-Path $BackupDirectory)) {
    New-Item -ItemType Directory -Path $BackupDirectory -Force | Out-Null
    Write-Host "Created backup directory."
}
else {
    Write-Host "Backup directory already exists."
}

Write-Host "SQL Server writes .bak files using the SQL Server service account, not only the ASP.NET Core process account."

if ([string]::IsNullOrWhiteSpace($SqlServiceAccount)) {
    Write-Host "No SQL Server service account was provided."
    Write-Host "Find the account in SQL Server Configuration Manager or Windows Services, then grant it write permission."
    Write-Host "Example:"
    Write-Host ".\scripts\prepare-backup-folder.ps1 -SqlServiceAccount 'NT SERVICE\MSSQL`$SQLEXPRESS'"
    return
}

Write-Host "Granting modify permission to SQL Server service account:"
Write-Host $SqlServiceAccount

icacls $BackupDirectory /grant "$SqlServiceAccount`:(OI)(CI)M"

Write-Host "Backup folder permissions updated."
