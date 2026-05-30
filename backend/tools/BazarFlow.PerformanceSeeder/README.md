# BazarFlow Performance Seeder

`BazarFlow.PerformanceSeeder` is a standalone console tool for preparing synthetic performance datasets.

Current status: `V2-06B-1` skeleton only. It validates CLI options, validates database safety rules, and prints deterministic dry-run previews. It does not write to the database and does not reset data.

## Safety Warnings

- Production databases are forbidden.
- Real customer data must never be used.
- The full connection string is never printed.
- `--confirm` is required for every run.
- A database name must contain one of: `Performance`, `Perf`, `Test`, `Load`.
- Production-like database names are rejected, including `BazarFlow`, `BazarFlowProd`, `Production`, and `Prod`.
- Reset is not implemented in `V2-06B-1`. If `--reset` is passed, `--confirm-reset` is still required and dry-run output only says reset would run in a future phase.

## Supported Profiles

| Profile | Categories | Suppliers | Products | Employees | Devices | Invoices |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| small | 10 | 5 | 500 | 3 | 3 | 1,000 |
| medium | 50 | 30 | 5,000 | 10 | 10 | 10,000 |
| large | 150 | 100 | 20,000 | 30 | 30 | 50,000 |

## CLI Options

```text
--profile small|medium|large
--connection "<connection-string>"
--seed 12345
--confirm
--reset
--confirm-reset
--dry-run
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

## Future Phases

- Add schema-aware synthetic data generation.
- Add batched database writes for safe test/performance databases only.
- Add synthetic-only reset support with double confirmation.
- Add inventory movement, blackbox event, and audit log generation where schema constraints allow.
- Keep this tool out of production installers unless explicitly approved later.
