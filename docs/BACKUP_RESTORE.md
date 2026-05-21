# BazarFlow Backup / Restore

## Overview

BazarFlow v0.4 uses SQL Server native backup for local database backups. The application creates a `.bak` file by running `BACKUP DATABASE` against the configured SQL Server database.

This version does not include a Restore UI, Restore API endpoint, scheduled backups, cloud backup, backup encryption, or direct download of backup files.

## Create Backup From Settings

1. Sign in with a user that has access to Settings.
2. Open Settings.
3. Open the Backup tab.
4. Click the create backup button.
5. Confirm that the UI shows the generated file name, creation time, and file size.
6. Confirm that the Audit Logs screen contains a `CREATE_BACKUP` entry.

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

Do not place backup files inside the frontend static hosting folder, IIS web root, or any public folder.

## Sensitive Data Warning

`.bak` files contain sensitive business and system data, including employees, invoices, products, inventory, purchases, permissions, and application settings.

Store backup files in a protected folder. Do not share them with unauthorized users, upload them to public storage, or expose them through static hosting.

## SQL Server Service Account Permissions

The SQL Server service account writes the `.bak` file, not only the ASP.NET Core process account.

If backup fails with a path or access error, make sure the Windows account running SQL Server has write permission on `C:\BazarFlowBackups`.

For SQL Server Express, the service account is often similar to:

```text
NT SERVICE\MSSQL$SQLEXPRESS
```

Confirm the exact account from SQL Server Configuration Manager or Windows Services. Do not assume all machines use `SQLEXPRESS`.

## Create the Backup Directory

Run PowerShell as Administrator:

```powershell
New-Item -ItemType Directory -Path "C:\BazarFlowBackups" -Force
```

Grant write permission to the SQL Server service account. Replace the account name if your SQL Server service uses a different account:

```powershell
icacls "C:\BazarFlowBackups" /grant "NT SERVICE\MSSQL$SQLEXPRESS:(OI)(CI)M"
```

The repository also includes:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\prepare-backup-folder.ps1 -SqlServiceAccount "NT SERVICE\MSSQL`$SQLEXPRESS"
```

## List Backup Files

Use the helper script to list backup files without deleting or restoring anything:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\list-backups.ps1
```

For a custom directory:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\list-backups.ps1 -BackupDirectory "D:\BazarFlowBackups"
```

## Manual Backup Command

You can create a backup manually in SQL Server Management Studio or `sqlcmd`:

```sql
BACKUP DATABASE [SupermarketDb]
TO DISK = N'C:\BazarFlowBackups\BazarFlow_Backup_yyyyMMdd_HHmmss.bak'
WITH INIT, CHECKSUM;
```

Replace the database name and timestamp before running the command.

## Validate Backup With RESTORE VERIFYONLY

`WITH CHECKSUM` during backup is useful, but it is not enough by itself. Validate important backup files with `RESTORE VERIFYONLY`.

Using the helper script:

```powershell
$env:ConnectionStrings__DefaultConnection = "Server=...;Database=...;User Id=...;Password=...;TrustServerCertificate=True"
powershell -ExecutionPolicy Bypass -File scripts\verify-backup.ps1 -BackupFile "C:\BazarFlowBackups\BazarFlow_Backup_20260520_143012.bak"
```

Or pass the connection string explicitly without storing it in the script:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\verify-backup.ps1 -BackupFile "C:\BazarFlowBackups\BazarFlow_Backup_20260520_143012.bak" -ConnectionString "<connection string>"
```

Equivalent SQL:

```sql
RESTORE VERIFYONLY
FROM DISK = N'C:\BazarFlowBackups\BazarFlow_Backup_20260520_143012.bak'
WITH CHECKSUM;
```

`RESTORE VERIFYONLY` checks that SQL Server can read the backup and validate checksum metadata. It does not replace a full restore test.

## Restore To A Test Database First

Restore is manual in this version. There is no Restore UI and no Restore API endpoint.

Before any restore:

1. Stop the BazarFlow API before a real restore operation.
2. Take a fresh backup of the current database before restoring any older file.
3. Run `RESTORE VERIFYONLY` on the selected `.bak` file.
4. Restore into a new test database first, not over the production or working database.
5. Confirm the restored database contains the expected data.
6. Only consider restoring over the working database after a verified backup and an explicit data-loss decision.

Use this template as a starting point:

```text
scripts\restore-database-manual-template.sql
```

Review logical file names with `RESTORE FILELISTONLY` before writing the final `WITH MOVE` paths.

## Post-Restore Table Checks

After restoring to a test database, check that these core tables exist and contain expected records:

```sql
SELECT COUNT(*) AS EmployeesCount FROM [__TARGET_DATABASE__].dbo.EMPLOYEES;
SELECT COUNT(*) AS ProductsCount FROM [__TARGET_DATABASE__].dbo.PRODUCTS;
SELECT COUNT(*) AS ProductBatchesCount FROM [__TARGET_DATABASE__].dbo.PRODUCT_BATCHES;
SELECT COUNT(*) AS InvoicesCount FROM [__TARGET_DATABASE__].dbo.INVOICES;
SELECT COUNT(*) AS PurchaseInvoicesCount FROM [__TARGET_DATABASE__].dbo.PURCHASE_INVOICES;
SELECT COUNT(*) AS AppSettingsCount FROM [__TARGET_DATABASE__].dbo.APP_SETTINGS;
```

If the actual database uses different table casing or schema names, adjust the queries to match the SQL Server metadata.

## Critical Restore Warning

Do not restore over a live or actively used database unless all of the following are true:

- The API is stopped.
- A fresh backup of the current database has been created.
- The selected backup passed `RESTORE VERIFYONLY`.
- The selected backup was restored successfully to a test database first.
- The operator accepts that newer data can be lost.

Restoring an older backup over the working database can permanently remove sales, purchases, inventory changes, settings, and audit logs created after the backup time.

## Troubleshooting

### Access Denied

- Confirm the backup directory exists.
- Confirm the SQL Server service account has write permission.
- Confirm the backup file is not blocked by antivirus or file permissions.
- Remember that SQL Server writes the file during backup and reads the file during verify/restore.

### Disk Full

- Check free space on the backup drive.
- Move old backups to secure offline storage if needed.
- Do not delete backups until the customer confirms the retention requirement.

### SQL Service Account

- Confirm the exact service account from SQL Server Configuration Manager or Windows Services.
- Grant access to that account, not only to the Windows user running the API.

### Wrong Instance

- Confirm the connection string points to the intended SQL Server instance.
- Confirm the backup file belongs to the expected environment.
- Avoid verifying or restoring a production backup against the wrong local instance.

### Corrupted Backup

- Run `RESTORE VERIFYONLY ... WITH CHECKSUM`.
- If verify fails, do not use the file for restore.
- Create a fresh backup if the source database is still available.
- If multiple backups exist, verify an older known-good backup and restore it to a test database first.
