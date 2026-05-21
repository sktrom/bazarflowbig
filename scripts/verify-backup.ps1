param(
    [Parameter(Mandatory = $true)]
    [string]$BackupFile,

    [string]$ConnectionString
)

$ErrorActionPreference = "Stop"

if (-not $ConnectionString) {
    $ConnectionString = [Environment]::GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
}

if ([string]::IsNullOrWhiteSpace($ConnectionString)) {
    Write-Error "Connection string is required. Set ConnectionStrings__DefaultConnection or pass -ConnectionString."
    exit 1
}

if (-not (Test-Path -LiteralPath $BackupFile -PathType Leaf)) {
    Write-Error "Backup file not found: $BackupFile"
    exit 1
}

$fullBackupPath = [System.IO.Path]::GetFullPath($BackupFile)
$escapedBackupPath = $fullBackupPath.Replace("'", "''")

Write-Host "Verifying backup file:"
Write-Host $fullBackupPath
Write-Host "No restore or database write will be performed."

$connection = [System.Data.SqlClient.SqlConnection]::new($ConnectionString)

try {
    $connection.Open()
    $command = $connection.CreateCommand()
    $command.CommandTimeout = 300
    $command.CommandText = "RESTORE VERIFYONLY FROM DISK = N'$escapedBackupPath' WITH CHECKSUM;"
    $command.ExecuteNonQuery() | Out-Null

    Write-Host "Backup verification succeeded."
}
catch {
    Write-Error "Backup verification failed. $($_.Exception.Message)"
    exit 1
}
finally {
    if ($command) {
        $command.Dispose()
    }

    if ($connection.State -ne [System.Data.ConnectionState]::Closed) {
        $connection.Close()
    }

    $connection.Dispose()
}
