# Performance Scripts

This directory is reserved for future BazarFlow performance work.

Planned future contents:

- NBomber performance test project.
- Synthetic dataset seed tooling.
- SQL Server DMV capture scripts.
- PowerShell orchestration scripts.
- Local result collection helpers.

Current status:

- V2-06A creates documentation artifacts only.
- No load test implementation exists here yet.
- V2-06B-1 seed skeleton exists at `backend/tools/BazarFlow.PerformanceSeeder`; `scripts/performance/seed` documents how to run it.
- V2-06C diagnostics collector exists at `scripts/performance/diagnostics/bazarflow-db-diagnostics.sql`.
- V2-06D NBomber smoke tests exist at `scripts/performance/BazarFlow.PerformanceTests`.
- No orchestration scripts exist here yet.

Safety rules:

- Do not run performance tests against production.
- Do not use real customer data.
- Do not run destructive database reset commands without explicit confirmation.
- Do not run stress tests on a production machine.
- Treat diagnostics output as operational data. Do not publish it outside the team without review.
- Do not include these assets in the installer unless explicitly approved later.

Load-test execution assets remain deferred to later phases.
