# BazarFlow Performance Seeder

`BazarFlow.PerformanceSeeder` is a standalone console tool for preparing synthetic performance datasets.

Current status: `V2-06B-3A`. It validates CLI options, validates database safety rules, prints deterministic dry-run previews, and can insert core synthetic reference data:

- Categories
- Suppliers
- Products
- Employees test users
- POS devices

When `--include-transactions` is passed, it also creates synthetic purchases, purchase lines, and product batches. It does not create sales invoices, invoice lines, inventory allocations, blackbox events, audit logs, or reset data.

## Safety Warnings

- Production databases are forbidden.
- Real customer data must never be used.
- The full connection string is never printed.
- `--confirm` is required for every run.
- A database name must contain one of: `Performance`, `Perf`, `Test`, `Load`.
- Production-like database names are rejected, including `BazarFlow`, `BazarFlowProd`, `Production`, and `Prod`.
- Reset is not implemented in `V2-06B-3A`. If `--reset` is passed in non-dry-run mode, the tool refuses to run writes and asks you to remove `--reset`.
- The tool is not included in the production installer.
- Employees use synthetic `example.test` usernames. If login is needed for performance environments, the test-only password is `PerformanceTest!123`; only its hash is stored and the raw password is not printed at runtime.

## Supported Profiles

| Profile | Categories | Suppliers | Products | Employees | Devices |
| --- | ---: | ---: | ---: | ---: | ---: |
| small | 10 | 5 | 500 | 3 | 3 |
| medium | 50 | 30 | 5,000 | 10 | 10 |
| large | 150 | 100 | 20,000 | 30 | 30 |

## CLI Options

```text
--profile small|medium|large
--connection "<connection-string>"
--seed 12345
--confirm
--reset
--confirm-reset
--dry-run
--include-transactions
```

The connection string can also come from:

```text
BAZARFLOW_PERF_SEED_CONNECTION
```

## Dry-Run Examples

```powershell
dotnet run --project backend/tools/BazarFlow.PerformanceSeeder -- --profile small --connection "Server=.;Database=BazarFlowPerformance;Trusted_Connection=True;TrustServerCertificate=True" --seed 12345 --confirm --dry-run
```

```powershell
$env:BAZARFLOW_PERF_SEED_CONNECTION="Server=.;Database=BazarFlowLoadTest;Trusted_Connection=True;TrustServerCertificate=True"
dotnet run --project backend/tools/BazarFlow.PerformanceSeeder -- --profile medium --seed 12345 --confirm --dry-run
```

Reset validation preview:

```powershell
dotnet run --project backend/tools/BazarFlow.PerformanceSeeder -- --profile small --connection "Server=.;Database=BazarFlowPerformance;Trusted_Connection=True;TrustServerCertificate=True" --reset --confirm-reset --confirm --dry-run
```

Transactional dry-run:

```powershell
dotnet run --project backend/tools/BazarFlow.PerformanceSeeder -- --profile small --connection "Server=.;Database=BazarFlowPerformance;Trusted_Connection=True;TrustServerCertificate=True" --seed 12345 --confirm --dry-run --include-transactions
```

## Write Mode Examples

Write mode is insert-only and idempotent. It inserts missing synthetic rows with `BF-PERF` identifiers and skips existing rows for the same seed/profile.

```powershell
dotnet run --project backend/tools/BazarFlow.PerformanceSeeder -- --profile small --connection "Server=.;Database=BazarFlowPerformance;Trusted_Connection=True;TrustServerCertificate=True" --seed 12345 --confirm
```

Using the environment variable:

```powershell
$env:BAZARFLOW_PERF_SEED_CONNECTION="Server=.;Database=BazarFlowPerf;Trusted_Connection=True;TrustServerCertificate=True"
dotnet run --project backend/tools/BazarFlow.PerformanceSeeder -- --profile medium --seed 12345 --confirm
```

Do not use `--reset` in write mode. Reset is deferred to a future phase.

## Transactional Purchases

`--include-transactions` adds purchases and product batches after the core reference data path:

```powershell
dotnet run --project backend/tools/BazarFlow.PerformanceSeeder -- --profile small --connection "Server=.;Database=BazarFlowPerformance;Trusted_Connection=True;TrustServerCertificate=True" --seed 12345 --confirm --include-transactions
```

V2-06B-3A transactional scope:

- `PurchaseInvoice`
- `PurchaseInvoiceLine`
- `ProductBatch`

Deferred:

- sales invoices
- invoice lines
- invoice line batch allocations
- blackbox events
- audit logs

Approximate transactional volumes:

| Profile | Purchases | Purchase Lines | Product Batches |
| --- | ---: | ---: | ---: |
| small | 100 | 500-2,000 | 500-2,000 |
| medium | 1,000 | 5,000-20,000 | 5,000-20,000 |
| large | 5,000 | 25,000-100,000 | 25,000-100,000 |

Large profile can take time and should only be run against a dedicated performance database.

## Future Phases

- Add synthetic-only reset support with double confirmation.
- Add sales invoices, invoice allocations, blackbox event, and audit log generation where schema constraints allow.
- Keep this tool out of production installers unless explicitly approved later.
