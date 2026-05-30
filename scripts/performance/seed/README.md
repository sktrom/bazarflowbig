# Performance Seed Tooling

Synthetic performance dataset seeding lives in:

```text
backend/tools/BazarFlow.PerformanceSeeder
```

`V2-06B-3A` supports safety validation, DB-free dry-run previews, insert-only core reference data writes, and opt-in purchase/product batch seeding with `--include-transactions`. It does not reset data.

Example:

```powershell
dotnet run --project backend/tools/BazarFlow.PerformanceSeeder -- --profile small --connection "Server=.;Database=BazarFlowPerformance;Trusted_Connection=True;TrustServerCertificate=True" --seed 12345 --confirm --dry-run
```

Write mode:

```powershell
dotnet run --project backend/tools/BazarFlow.PerformanceSeeder -- --profile small --connection "Server=.;Database=BazarFlowPerformance;Trusted_Connection=True;TrustServerCertificate=True" --seed 12345 --confirm
```

Purchase/product batch write mode:

```powershell
dotnet run --project backend/tools/BazarFlow.PerformanceSeeder -- --profile small --connection "Server=.;Database=BazarFlowPerformance;Trusted_Connection=True;TrustServerCertificate=True" --seed 12345 --confirm --include-transactions
```

Safety rules:

- Use synthetic data only.
- Do not target production databases.
- Use database names containing `Performance`, `Perf`, `Test`, or `Load`.
- Do not use `--reset`; reset remains deferred.
- Do not include this tooling in the production installer.
