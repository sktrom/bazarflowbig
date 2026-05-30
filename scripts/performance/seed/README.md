# Performance Seed Tooling

Synthetic performance dataset seeding lives in:

```text
backend/tools/BazarFlow.PerformanceSeeder
```

`V2-06B-1` supports safety validation and dry-run previews only. It does not write data and does not reset data.

Example:

```powershell
dotnet run --project backend/tools/BazarFlow.PerformanceSeeder -- --profile small --connection "Server=.;Database=BazarFlowPerformance;Trusted_Connection=True;TrustServerCertificate=True" --seed 12345 --confirm --dry-run
```

Safety rules:

- Use synthetic data only.
- Do not target production databases.
- Use database names containing `Performance`, `Perf`, `Test`, or `Load`.
- Do not include this tooling in the production installer.
