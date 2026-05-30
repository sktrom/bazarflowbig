# Performance Seed Tooling

Synthetic performance dataset seeding lives in:

```text
backend/tools/BazarFlow.PerformanceSeeder
```

`V2-06B-3C` supports safety validation, DB-free dry-run previews, insert-only core reference data writes, and opt-in purchase/product batch, sales invoice, and BlackBox event seeding with `--include-transactions`. It does not reset data.

Example:

```powershell
dotnet run --project backend/tools/BazarFlow.PerformanceSeeder -- --profile small --connection "Server=.;Database=BazarFlowPerformance;Trusted_Connection=True;TrustServerCertificate=True" --seed 12345 --confirm --dry-run
```

Write mode:

```powershell
dotnet run --project backend/tools/BazarFlow.PerformanceSeeder -- --profile small --connection "Server=.;Database=BazarFlowPerformance;Trusted_Connection=True;TrustServerCertificate=True" --seed 12345 --confirm
```

Transactional write mode:

```powershell
dotnet run --project backend/tools/BazarFlow.PerformanceSeeder -- --profile small --connection "Server=.;Database=BazarFlowPerformance;Trusted_Connection=True;TrustServerCertificate=True" --seed 12345 --confirm --include-transactions
```

V2-06B-3C creates synthetic BlackBox events only when `--include-transactions` is passed. It does not create cash sessions, audit logs, invoice line batch allocations, or stock consumption.

Safety rules:

- Use synthetic data only.
- Do not target production databases.
- Use database names containing `Performance`, `Perf`, `Test`, or `Load`.
- Do not use `--reset`; reset remains deferred.
- Do not include this tooling in the production installer.
