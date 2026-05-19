# BazarFlow Backup / Restore

## Overview

BazarFlow v0.4 uses SQL Server native backup for local database backups. The application creates a `.bak` file by running `BACKUP DATABASE` against the configured SQL Server database.

This version does not include a Restore UI, scheduled backups, cloud backup, backup encryption, or direct download of backup files.

## Default Backup Location

Default directory:

```text
C:\BazarFlowBackups
```

Default filename format:

```text
BazarFlow_Backup_yyyyMMdd_HHmmss.bak
```

Example:

```text
BazarFlow_Backup_20260520_143012.bak
```

## Important Permission Note

The SQL Server service account writes the `.bak` file, not only the ASP.NET Core process account.

If backup fails with a path or access error, make sure the Windows account running SQL Server has write permission on `C:\BazarFlowBackups`.

For SQL Server Express, the service account is often similar to:

```text
NT SERVICE\MSSQL$SQLEXPRESS
```

Confirm the exact account from SQL Server Configuration Manager or Windows Services.

## Create the Backup Directory

Run PowerShell as Administrator:

```powershell
New-Item -ItemType Directory -Path "C:\BazarFlowBackups" -Force
```

Grant write permission to the SQL Server service account. Replace the account name if your SQL Server service uses a different account:

```powershell
icacls "C:\BazarFlowBackups" /grant "NT SERVICE\MSSQL$SQLEXPRESS:(OI)(CI)M"
```

## Manual Backup Command

You can create a backup manually in SQL Server Management Studio or `sqlcmd`:

```sql
BACKUP DATABASE [SupermarketDb]
TO DISK = N'C:\BazarFlowBackups\BazarFlow_Backup_yyyyMMdd_HHmmss.bak'
WITH INIT, CHECKSUM;
```

Replace the timestamp in the filename before running the command.

## Restore Manual Guide

Restore is manual in this version. There is no Restore UI.

Before restore:

1. Stop the BazarFlow API and frontend.
2. Take a fresh backup of the current database before restoring any older file.
3. Test restore into a new database first, not over the production/local working database.
4. Do not restore during normal daily work.
5. Confirm the `.bak` file came from a trusted source and matches the intended environment.

Example restore to a separate test database should be performed through SQL Server Management Studio, where logical file names and target paths can be reviewed before execution.

Do not restore over `SupermarketDb` until you have verified the backup and accepted the data-loss risk.
