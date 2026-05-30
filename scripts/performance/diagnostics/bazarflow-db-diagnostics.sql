/*
    BazarFlow Performance Database Diagnostics Collector
    Scope: read-only SQL Server diagnostics for performance/test databases.

    Safety:
    - No INSERT, UPDATE, DELETE, MERGE.
    - No CREATE, ALTER, DROP.
    - No destructive DBCC.
    - No database setting changes.
*/

SET NOCOUNT ON;

DECLARE @database_id int = DB_ID();
DECLARE @database_name sysname = DB_NAME();

PRINT '============================================================';
PRINT ' BazarFlow Performance Database Diagnostics Collector';
PRINT ' Read-only snapshot. No writes or setting changes are made.';
PRINT ' Database: ' + COALESCE(@database_name, N'<unknown>');
PRINT ' Captured UTC: ' + CONVERT(varchar(33), SYSUTCDATETIME(), 126);
PRINT '============================================================';

PRINT '';
PRINT 'A) Database overview';
PRINT '------------------------------------------------------------';

SELECT
    d.name AS database_name,
    @@VERSION AS sql_server_version,
    d.compatibility_level,
    d.recovery_model_desc,
    CAST(SUM(mf.size) * 8.0 / 1024.0 AS decimal(18, 2)) AS database_size_mb,
    CAST(SUM(CASE WHEN mf.type_desc = 'ROWS' THEN mf.size ELSE 0 END) * 8.0 / 1024.0 AS decimal(18, 2)) AS data_files_size_mb,
    CAST(SUM(CASE WHEN mf.type_desc = 'LOG' THEN mf.size ELSE 0 END) * 8.0 / 1024.0 AS decimal(18, 2)) AS log_file_size_mb
FROM sys.databases AS d
JOIN sys.master_files AS mf ON mf.database_id = d.database_id
WHERE d.database_id = @database_id
GROUP BY d.name, d.compatibility_level, d.recovery_model_desc;

SELECT
    DB_NAME() AS database_name,
    CAST(SUM(FILEPROPERTY(name, 'SpaceUsed')) * 8.0 / 1024.0 AS decimal(18, 2)) AS used_space_mb,
    CAST(SUM(size - FILEPROPERTY(name, 'SpaceUsed')) * 8.0 / 1024.0 AS decimal(18, 2)) AS free_unallocated_space_mb
FROM sys.database_files
WHERE type_desc = 'ROWS';

PRINT '';
PRINT 'B) Table sizes';
PRINT '------------------------------------------------------------';

;WITH TableSize AS
(
    SELECT
        s.name AS schema_name,
        t.name AS table_name,
        SUM(CASE WHEN ps.index_id IN (0, 1) THEN ps.row_count ELSE 0 END) AS row_count,
        SUM(ps.reserved_page_count) AS total_pages,
        SUM(ps.used_page_count) AS used_pages,
        SUM(CASE WHEN ps.index_id IN (0, 1)
            THEN ps.in_row_data_page_count + ps.lob_used_page_count + ps.row_overflow_used_page_count
            ELSE 0
        END) AS data_pages
    FROM sys.tables AS t
    JOIN sys.schemas AS s ON s.schema_id = t.schema_id
    JOIN sys.dm_db_partition_stats AS ps ON ps.object_id = t.object_id
    WHERE t.is_ms_shipped = 0
    GROUP BY s.name, t.name
)
SELECT
    schema_name,
    table_name,
    row_count,
    CAST(total_pages * 8.0 / 1024.0 AS decimal(18, 2)) AS reserved_mb,
    CAST(data_pages * 8.0 / 1024.0 AS decimal(18, 2)) AS data_mb,
    CAST((used_pages - data_pages) * 8.0 / 1024.0 AS decimal(18, 2)) AS index_mb,
    CAST((total_pages - used_pages) * 8.0 / 1024.0 AS decimal(18, 2)) AS unused_mb
FROM TableSize
ORDER BY reserved_mb DESC, row_count DESC;

PRINT '';
PRINT 'C1) Largest tables - Top 20 by MB';
PRINT '------------------------------------------------------------';

;WITH TableSize AS
(
    SELECT
        s.name AS schema_name,
        t.name AS table_name,
        SUM(CASE WHEN ps.index_id IN (0, 1) THEN ps.row_count ELSE 0 END) AS row_count,
        SUM(ps.reserved_page_count) AS total_pages
    FROM sys.tables AS t
    JOIN sys.schemas AS s ON s.schema_id = t.schema_id
    JOIN sys.dm_db_partition_stats AS ps ON ps.object_id = t.object_id
    WHERE t.is_ms_shipped = 0
    GROUP BY s.name, t.name
)
SELECT TOP (20)
    schema_name,
    table_name,
    row_count,
    CAST(total_pages * 8.0 / 1024.0 AS decimal(18, 2)) AS reserved_mb
FROM TableSize
ORDER BY reserved_mb DESC, row_count DESC;

PRINT '';
PRINT 'C2) Largest tables - Top 20 by row count';
PRINT '------------------------------------------------------------';

;WITH TableSize AS
(
    SELECT
        s.name AS schema_name,
        t.name AS table_name,
        SUM(CASE WHEN ps.index_id IN (0, 1) THEN ps.row_count ELSE 0 END) AS row_count,
        SUM(ps.reserved_page_count) AS total_pages
    FROM sys.tables AS t
    JOIN sys.schemas AS s ON s.schema_id = t.schema_id
    JOIN sys.dm_db_partition_stats AS ps ON ps.object_id = t.object_id
    WHERE t.is_ms_shipped = 0
    GROUP BY s.name, t.name
)
SELECT TOP (20)
    schema_name,
    table_name,
    row_count,
    CAST(total_pages * 8.0 / 1024.0 AS decimal(18, 2)) AS reserved_mb
FROM TableSize
ORDER BY row_count DESC, reserved_mb DESC;

PRINT '';
PRINT 'C3) Important table spotlight';
PRINT '------------------------------------------------------------';

;WITH ImportantNames AS
(
    SELECT UPPER(name) AS table_name
    FROM (VALUES
        ('BLACK_BOX_EVENTS'),
        ('INVOICES'),
        ('INVOICE_LINES'),
        ('PURCHASE_INVOICES'),
        ('PURCHASE_INVOICE_LINES'),
        ('PRODUCT_BATCHES'),
        ('PRODUCTS')
    ) AS v(name)
),
TableSize AS
(
    SELECT
        s.name AS schema_name,
        t.name AS table_name,
        UPPER(t.name) AS normalized_table_name,
        SUM(CASE WHEN ps.index_id IN (0, 1) THEN ps.row_count ELSE 0 END) AS row_count,
        SUM(ps.reserved_page_count) AS total_pages
    FROM sys.tables AS t
    JOIN sys.schemas AS s ON s.schema_id = t.schema_id
    JOIN sys.dm_db_partition_stats AS ps ON ps.object_id = t.object_id
    WHERE t.is_ms_shipped = 0
    GROUP BY s.name, t.name
)
SELECT
    i.table_name AS requested_table,
    ts.schema_name,
    ts.table_name AS actual_table_name,
    COALESCE(ts.row_count, 0) AS row_count,
    CAST(COALESCE(ts.total_pages, 0) * 8.0 / 1024.0 AS decimal(18, 2)) AS reserved_mb,
    CASE WHEN ts.table_name IS NULL THEN 'missing' ELSE 'present' END AS status
FROM ImportantNames AS i
LEFT JOIN TableSize AS ts ON ts.normalized_table_name = i.table_name
ORDER BY i.table_name;

PRINT '';
PRINT 'D) Index usage';
PRINT '------------------------------------------------------------';

SELECT
    SCHEMA_NAME(t.schema_id) AS schema_name,
    t.name AS table_name,
    i.name AS index_name,
    i.type_desc AS index_type,
    COALESCE(us.user_seeks, 0) AS user_seeks,
    COALESCE(us.user_scans, 0) AS user_scans,
    COALESCE(us.user_lookups, 0) AS user_lookups,
    COALESCE(us.user_updates, 0) AS user_updates,
    CASE
        WHEN COALESCE(us.user_seeks, 0) + COALESCE(us.user_scans, 0) + COALESCE(us.user_lookups, 0) = 0
             AND COALESCE(us.user_updates, 0) > 0 THEN 'unused_with_writes'
        WHEN COALESCE(us.user_updates, 0) > 1000
             AND COALESCE(us.user_seeks, 0) + COALESCE(us.user_scans, 0) + COALESCE(us.user_lookups, 0) < 10 THEN 'high_write_low_read'
        WHEN COALESCE(us.user_seeks, 0) + COALESCE(us.user_scans, 0) + COALESCE(us.user_lookups, 0) = 0 THEN 'unused_or_not_seen_since_restart'
        ELSE 'used'
    END AS usage_indication
FROM sys.tables AS t
JOIN sys.indexes AS i ON i.object_id = t.object_id
LEFT JOIN sys.dm_db_index_usage_stats AS us
    ON us.database_id = @database_id
    AND us.object_id = i.object_id
    AND us.index_id = i.index_id
WHERE t.is_ms_shipped = 0
  AND i.index_id > 0
ORDER BY
    CASE
        WHEN COALESCE(us.user_seeks, 0) + COALESCE(us.user_scans, 0) + COALESCE(us.user_lookups, 0) = 0 THEN 0
        ELSE 1
    END,
    COALESCE(us.user_updates, 0) DESC,
    t.name,
    i.name;

PRINT '';
PRINT 'E) Missing indexes';
PRINT '------------------------------------------------------------';
PRINT 'These are recommendations only. This script does not create or apply indexes.';

SELECT TOP (50)
    SCHEMA_NAME(o.schema_id) AS schema_name,
    o.name AS table_name,
    mid.equality_columns,
    mid.inequality_columns,
    mid.included_columns,
    migs.user_seeks,
    migs.user_scans,
    CAST(migs.avg_total_user_cost * migs.avg_user_impact * (migs.user_seeks + migs.user_scans) AS decimal(18, 2)) AS estimated_improvement,
    migs.avg_total_user_cost,
    migs.avg_user_impact
FROM sys.dm_db_missing_index_group_stats AS migs
JOIN sys.dm_db_missing_index_groups AS mig ON mig.index_group_handle = migs.group_handle
JOIN sys.dm_db_missing_index_details AS mid ON mid.index_handle = mig.index_handle
JOIN sys.objects AS o ON o.object_id = mid.object_id
WHERE mid.database_id = @database_id
ORDER BY estimated_improvement DESC;

PRINT '';
PRINT 'F) Slow / expensive queries';
PRINT '------------------------------------------------------------';

IF EXISTS (SELECT 1 FROM sys.database_query_store_options WHERE actual_state_desc = 'READ_WRITE')
BEGIN
    PRINT 'F1) Query Store top queries';

    SELECT TOP (25)
        CAST(SUM(rs.count_executions * rs.avg_cpu_time) AS decimal(18, 2)) AS total_worker_time_ms,
        CAST(SUM(rs.count_executions * rs.avg_duration) AS decimal(18, 2)) AS total_elapsed_time_ms,
        CAST(SUM(rs.count_executions * rs.avg_logical_io_reads) AS decimal(18, 2)) AS total_logical_reads,
        SUM(rs.count_executions) AS execution_count,
        CAST(SUM(rs.count_executions * rs.avg_duration) / NULLIF(SUM(rs.count_executions), 0) AS decimal(18, 2)) AS avg_duration_ms,
        LEFT(REPLACE(REPLACE(qt.query_sql_text, CHAR(13), ' '), CHAR(10), ' '), 1000) AS query_text
    FROM sys.query_store_query_text AS qt
    JOIN sys.query_store_query AS q ON q.query_text_id = qt.query_text_id
    JOIN sys.query_store_plan AS p ON p.query_id = q.query_id
    JOIN sys.query_store_runtime_stats AS rs ON rs.plan_id = p.plan_id
    GROUP BY qt.query_sql_text
    ORDER BY total_elapsed_time_ms DESC;
END
ELSE
BEGIN
    PRINT 'F1) Query Store is not READ_WRITE for this database. Showing plan cache fallback.';
END;

PRINT 'F2) Plan cache top queries';

SELECT TOP (25)
    qs.total_worker_time AS total_worker_time,
    qs.total_elapsed_time AS total_elapsed_time,
    qs.total_logical_reads,
    qs.execution_count,
    qs.total_elapsed_time / NULLIF(qs.execution_count, 0) AS average_duration,
    LEFT(
        REPLACE(REPLACE(
            SUBSTRING(
                st.text,
                (qs.statement_start_offset / 2) + 1,
                CASE
                    WHEN qs.statement_end_offset = -1 THEN LEN(CONVERT(nvarchar(max), st.text))
                    ELSE (qs.statement_end_offset - qs.statement_start_offset) / 2 + 1
                END),
            CHAR(13), ' '), CHAR(10), ' '),
        1000) AS query_text
FROM sys.dm_exec_query_stats AS qs
CROSS APPLY sys.dm_exec_sql_text(qs.sql_handle) AS st
WHERE st.dbid = @database_id OR st.dbid IS NULL
ORDER BY qs.total_elapsed_time DESC;

PRINT '';
PRINT 'G) Blocking / waits snapshot';
PRINT '------------------------------------------------------------';

SELECT
    r.session_id,
    DB_NAME(r.database_id) AS database_name,
    r.blocking_session_id,
    r.wait_type,
    r.wait_time,
    r.status,
    r.command,
    r.cpu_time,
    r.logical_reads,
    r.reads,
    r.writes,
    LEFT(REPLACE(REPLACE(st.text, CHAR(13), ' '), CHAR(10), ' '), 1000) AS sql_text
FROM sys.dm_exec_requests AS r
OUTER APPLY sys.dm_exec_sql_text(r.sql_handle) AS st
WHERE r.database_id = @database_id
ORDER BY
    CASE WHEN r.blocking_session_id <> 0 THEN 0 ELSE 1 END,
    r.wait_time DESC;

PRINT '';
PRINT 'H) BlackBox volume';
PRINT '------------------------------------------------------------';

IF OBJECT_ID(N'dbo.BLACK_BOX_EVENTS', N'U') IS NOT NULL
BEGIN
    SELECT
        COUNT_BIG(*) AS total_events,
        MIN(CreatedAtUtc) AS oldest_created_at_utc,
        MAX(CreatedAtUtc) AS latest_created_at_utc,
        SUM(CASE WHEN Message LIKE '%BF-PERF-BBX%' OR MetadataJson LIKE '%BF-PERF-BBX%' THEN 1 ELSE 0 END) AS synthetic_marker_count
    FROM dbo.BLACK_BOX_EVENTS;

    SELECT ActionType, COUNT_BIG(*) AS event_count
    FROM dbo.BLACK_BOX_EVENTS
    GROUP BY ActionType
    ORDER BY event_count DESC;

    SELECT Result, COUNT_BIG(*) AS event_count
    FROM dbo.BLACK_BOX_EVENTS
    GROUP BY Result
    ORDER BY event_count DESC;

    SELECT CONVERT(date, CreatedAtUtc) AS event_date, COUNT_BIG(*) AS event_count
    FROM dbo.BLACK_BOX_EVENTS
    GROUP BY CONVERT(date, CreatedAtUtc)
    ORDER BY event_date DESC;

    SELECT TOP (20) DeviceCode, COUNT_BIG(*) AS event_count
    FROM dbo.BLACK_BOX_EVENTS
    GROUP BY DeviceCode
    ORDER BY event_count DESC;

    SELECT TOP (20) EmployeeId, COUNT_BIG(*) AS event_count
    FROM dbo.BLACK_BOX_EVENTS
    GROUP BY EmployeeId
    ORDER BY event_count DESC;
END
ELSE
BEGIN
    PRINT 'BLACK_BOX_EVENTS table not found. Skipping BlackBox volume section.';
END;

PRINT '';
PRINT 'I) Invoice / purchase throughput';
PRINT '------------------------------------------------------------';

IF OBJECT_ID(N'dbo.INVOICES', N'U') IS NOT NULL
BEGIN
    SELECT
        COUNT_BIG(*) AS total_invoices,
        MIN(CreatedAt) AS oldest_invoice_created_at,
        MAX(CreatedAt) AS latest_invoice_created_at,
        SUM(CASE WHEN InvoiceNumber LIKE 'BF-PERF-INV%' THEN 1 ELSE 0 END) AS synthetic_invoice_count
    FROM dbo.INVOICES;

    SELECT CONVERT(date, CreatedAt) AS invoice_date, COUNT_BIG(*) AS invoice_count
    FROM dbo.INVOICES
    GROUP BY CONVERT(date, CreatedAt)
    ORDER BY invoice_date DESC;
END
ELSE
BEGIN
    PRINT 'INVOICES table not found. Skipping invoice count and invoice per day.';
END;

IF OBJECT_ID(N'dbo.INVOICE_LINES', N'U') IS NOT NULL
BEGIN
    SELECT COUNT_BIG(*) AS invoice_lines_count
    FROM dbo.INVOICE_LINES;
END
ELSE
BEGIN
    PRINT 'INVOICE_LINES table not found. Skipping invoice line count.';
END;

IF OBJECT_ID(N'dbo.INVOICES', N'U') IS NOT NULL AND OBJECT_ID(N'dbo.INVOICE_LINES', N'U') IS NOT NULL
BEGIN
    SELECT
        CAST(COUNT_BIG(il.Id) * 1.0 / NULLIF(COUNT_BIG(DISTINCT i.Id), 0) AS decimal(18, 2)) AS avg_lines_per_invoice
    FROM dbo.INVOICES AS i
    LEFT JOIN dbo.INVOICE_LINES AS il ON il.InvoiceId = i.Id;
END;

IF OBJECT_ID(N'dbo.PURCHASE_INVOICES', N'U') IS NOT NULL
BEGIN
    SELECT
        COUNT_BIG(*) AS purchase_invoices_total,
        MIN(CreatedAt) AS oldest_purchase_created_at,
        MAX(CreatedAt) AS latest_purchase_created_at,
        SUM(CASE WHEN InvoiceNumber LIKE 'BF-PERF-PUR%' THEN 1 ELSE 0 END) AS synthetic_purchase_count
    FROM dbo.PURCHASE_INVOICES;

    SELECT CONVERT(date, CreatedAt) AS purchase_date, COUNT_BIG(*) AS purchase_count
    FROM dbo.PURCHASE_INVOICES
    GROUP BY CONVERT(date, CreatedAt)
    ORDER BY purchase_date DESC;
END
ELSE
BEGIN
    PRINT 'PURCHASE_INVOICES table not found. Skipping purchase invoice count and purchase per day.';
END;

IF OBJECT_ID(N'dbo.PURCHASE_INVOICE_LINES', N'U') IS NOT NULL
BEGIN
    SELECT COUNT_BIG(*) AS purchase_lines_count
    FROM dbo.PURCHASE_INVOICE_LINES;
END
ELSE
BEGIN
    PRINT 'PURCHASE_INVOICE_LINES table not found. Skipping purchase line count.';
END;

IF OBJECT_ID(N'dbo.PRODUCT_BATCHES', N'U') IS NOT NULL
BEGIN
    SELECT COUNT_BIG(*) AS product_batches_count
    FROM dbo.PRODUCT_BATCHES;
END
ELSE
BEGIN
    PRINT 'PRODUCT_BATCHES table not found. Skipping product batch count.';
END;

PRINT '';
PRINT 'J) Report readiness';
PRINT '------------------------------------------------------------';

;WITH HeavyTables AS
(
    SELECT UPPER(name) AS table_name
    FROM (VALUES
        ('BLACK_BOX_EVENTS'),
        ('INVOICES'),
        ('INVOICE_LINES'),
        ('PURCHASE_INVOICES'),
        ('PURCHASE_INVOICE_LINES'),
        ('PRODUCT_BATCHES'),
        ('PRODUCTS')
    ) AS v(name)
),
TableRows AS
(
    SELECT
        UPPER(t.name) AS table_name,
        SUM(ps.row_count) AS row_count
    FROM sys.tables AS t
    JOIN sys.dm_db_partition_stats AS ps ON ps.object_id = t.object_id
    WHERE ps.index_id IN (0, 1)
    GROUP BY t.name
)
SELECT
    h.table_name,
    COALESCE(tr.row_count, 0) AS row_count,
    CASE WHEN tr.table_name IS NULL THEN 'missing' ELSE 'present' END AS status
FROM HeavyTables AS h
LEFT JOIN TableRows AS tr ON tr.table_name = h.table_name
ORDER BY h.table_name;

IF OBJECT_ID(N'dbo.INVOICES', N'U') IS NOT NULL
BEGIN
    SELECT
        MIN(CreatedAt) AS invoice_min_created_at,
        MAX(CreatedAt) AS invoice_max_created_at,
        DATEDIFF(day, MIN(CreatedAt), MAX(CreatedAt)) AS invoice_date_span_days
    FROM dbo.INVOICES;
END;

IF OBJECT_ID(N'dbo.INVOICE_LINES', N'U') IS NOT NULL AND OBJECT_ID(N'dbo.PRODUCTS', N'U') IS NOT NULL
BEGIN
    SELECT TOP (20)
        p.Id AS product_id,
        p.Barcode,
        p.Name AS product_name,
        COUNT_BIG(*) AS invoice_line_count,
        SUM(il.Quantity) AS total_quantity
    FROM dbo.INVOICE_LINES AS il
    JOIN dbo.PRODUCTS AS p ON p.Id = il.ProductId
    GROUP BY p.Id, p.Barcode, p.Name
    ORDER BY invoice_line_count DESC;
END
ELSE
BEGIN
    PRINT 'INVOICE_LINES or PRODUCTS table not found. Skipping top products by invoice lines.';
END;

IF OBJECT_ID(N'dbo.BLACK_BOX_EVENTS', N'U') IS NOT NULL
BEGIN
    SELECT COUNT_BIG(*) AS blackbox_recent_24h_count
    FROM dbo.BLACK_BOX_EVENTS
    WHERE CreatedAtUtc >= DATEADD(day, -1, SYSUTCDATETIME());
END;

IF OBJECT_ID(N'dbo.PRODUCT_BATCHES', N'U') IS NOT NULL
BEGIN
    SELECT
        COUNT_BIG(*) AS product_batch_count,
        SUM(QuantityReceived) AS total_quantity_received,
        SUM(QuantityAvailable) AS total_quantity_available,
        SUM(CASE WHEN QuantityAvailable <= 0 THEN 1 ELSE 0 END) AS depleted_batch_count
    FROM dbo.PRODUCT_BATCHES;
END;

PRINT '';
PRINT 'Diagnostics snapshot complete.';
