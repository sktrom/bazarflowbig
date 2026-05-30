# BazarFlow Database Diagnostics Collector

## Purpose

`bazarflow-db-diagnostics.sql` captures a read-only SQL Server snapshot for BazarFlow performance databases. Use it to understand database growth, heavy tables, index usage, missing index suggestions, expensive queries, blocking/waits, BlackBox volume, and invoice/purchase throughput.

## Safety

- The script is read-only.
- It does not run `INSERT`, `UPDATE`, `DELETE`, `MERGE`, `CREATE`, `ALTER`, `DROP`, or destructive `DBCC`.
- It does not create indexes or change database settings.
- Missing index output is advisory only. Do not apply suggestions without review.
- Do not run against production unless a DBA explicitly approves the diagnostic capture.

## When To Run

- After synthetic seeding.
- Before a load test to capture a baseline.
- After a load test to compare growth, waits, index usage, and query costs.
- After schema or indexing changes to compare before/after snapshots.

## Run From SSMS

1. Open `scripts/performance/diagnostics/bazarflow-db-diagnostics.sql`.
2. Connect to the target performance database.
3. Confirm the database selector points to the intended database, for example `BazarFlowPerformance`.
4. Execute the script.
5. Save the Results and Messages output with a timestamp.

## Run With sqlcmd

SQL authentication:

```powershell
sqlcmd -S .\SQLEXPRESS -d BazarFlowPerformance -U sa -P 123456 -i scripts/performance/diagnostics/bazarflow-db-diagnostics.sql -o diagnostics-output.txt
```

Windows authentication:

```powershell
sqlcmd -S .\SQLEXPRESS -d BazarFlowPerformance -E -i scripts/performance/diagnostics/bazarflow-db-diagnostics.sql -o diagnostics-output.txt
```

## Saving Results

Use timestamped filenames so snapshots can be compared later:

```text
diagnostics-before-seed-2026-05-30.txt
diagnostics-after-seed-2026-05-30.txt
diagnostics-before-load-2026-05-30.txt
diagnostics-after-load-2026-05-30.txt
```

For comparisons, inspect:

- database size and free space deltas
- largest table growth
- BlackBox event growth
- invoice and purchase throughput
- expensive query changes
- new missing index recommendations
- blocking/wait changes

## Notes

Query Store output appears only when Query Store is enabled and writable for the database. If Query Store is unavailable, the script still returns plan cache top queries.
