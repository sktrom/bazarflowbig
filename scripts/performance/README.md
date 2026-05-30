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
- No SQL DMV scripts exist here yet.
- No orchestration scripts exist here yet.

Safety rules:

- Do not run performance tests against production.
- Do not use real customer data.
- Do not run destructive database reset commands without explicit confirmation.
- Do not run stress tests on a production machine.
- Do not include these assets in the installer unless explicitly approved later.

Implementation of executable performance assets is deferred to V2-06B/C and later phases.
