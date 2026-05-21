/*
    BazarFlow manual restore template.

    WARNING:
    - Do not run this against a production database directly.
    - Restore to a new test database first.
    - Stop the BazarFlow API before any real restore over a working database.
    - Take a fresh backup of the current database before restoring an older backup.
    - Review RESTORE FILELISTONLY output and replace all placeholders before running.

    Placeholders:
    __BACKUP_FILE__      Full path to the .bak file.
    __TARGET_DATABASE__  New database name to restore into first.
    __DATA_FILE_PATH__   Target .mdf path for the restored data file.
    __LOG_FILE_PATH__    Target .ldf path for the restored log file.
*/

RESTORE FILELISTONLY
FROM DISK = N'__BACKUP_FILE__';

RESTORE VERIFYONLY
FROM DISK = N'__BACKUP_FILE__'
WITH CHECKSUM;

/*
    Replace LogicalDataFileName and LogicalLogFileName below with the logical
    names returned by RESTORE FILELISTONLY.
*/
RESTORE DATABASE [__TARGET_DATABASE__]
FROM DISK = N'__BACKUP_FILE__'
WITH
    MOVE N'LogicalDataFileName' TO N'__DATA_FILE_PATH__',
    MOVE N'LogicalLogFileName' TO N'__LOG_FILE_PATH__',
    CHECKSUM,
    RECOVERY,
    STATS = 5;

/*
    Basic post-restore checks. Adjust schema/table names if your SQL Server
    metadata differs.
*/
SELECT COUNT(*) AS EmployeesCount FROM [__TARGET_DATABASE__].dbo.EMPLOYEES;
SELECT COUNT(*) AS ProductsCount FROM [__TARGET_DATABASE__].dbo.PRODUCTS;
SELECT COUNT(*) AS ProductBatchesCount FROM [__TARGET_DATABASE__].dbo.PRODUCT_BATCHES;
SELECT COUNT(*) AS InvoicesCount FROM [__TARGET_DATABASE__].dbo.INVOICES;
SELECT COUNT(*) AS PurchaseInvoicesCount FROM [__TARGET_DATABASE__].dbo.PURCHASE_INVOICES;
SELECT COUNT(*) AS AppSettingsCount FROM [__TARGET_DATABASE__].dbo.APP_SETTINGS;
