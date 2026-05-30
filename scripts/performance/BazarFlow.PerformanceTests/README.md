# BazarFlow Performance Smoke Tests

NBomber smoke tests for a local BazarFlow API connected to a seeded performance database.

## Purpose

This project verifies that the API can serve basic read-only workflows against synthetic performance data and captures first baseline metrics such as p50, p95, p99, request count, failed count, and status code distribution.

This is not a load test or stress test.

## Prerequisites

- BazarFlow API is running.
- API is connected to `BazarFlowPerformance` or another approved performance/test database.
- The database has been migrated and seeded with synthetic data.
- A test user is available.
- The test user can log in from the provided device code.
- The test user has at least `Products` permission.
- `Invoices` and `BlackBox` permissions are needed for the optional invoice and BlackBox scenarios.

## Safety

- Default base URL is `http://localhost:5070`.
- Non-localhost targets are refused unless `--allow-non-localhost` is passed.
- Production-like URLs containing `prod` or `production` are refused.
- Default concurrency is 1 user.
- Default duration is 60 seconds.
- The test does not call create, update, delete, complete invoice, purchase, backup, export, or reset endpoints.
- Passwords and session tokens are never printed.

## CLI

```text
--baseUrl http://localhost:5070
--username admin
--password 123456
--deviceCode POS-01
--duration 60
--users 1
--output scripts/performance/results
--allow-non-localhost
```

Environment fallback:

```text
BASE_URL
USERNAME
PASSWORD
DEVICE_CODE
DURATION_SECONDS
CONCURRENT_USERS
PERF_RESULTS_DIR
```

CLI values override environment variables.

## Run

```powershell
dotnet run --project scripts/performance/BazarFlow.PerformanceTests -- --baseUrl http://localhost:5070 --username admin --password 123456 --deviceCode POS-01 --duration 60 --users 1
```

Environment variable example:

```powershell
$env:BASE_URL="http://localhost:5070"
$env:USERNAME="admin"
$env:PASSWORD="123456"
$env:DEVICE_CODE="POS-01"
dotnet run --project scripts/performance/BazarFlow.PerformanceTests
```

## Scenarios

- `health_or_setup_status`: `GET /api/setup/status`
- `product_list`: `GET /api/products`
- `invoice_list`: `GET /api/invoices?page=1&pageSize=20`
- `invoice_read`: `GET /api/invoices/{id}/details` using an invoice id discovered from invoice list
- `blackbox_list`: `GET /api/black-box/events?page=1&pageSize=20`

Login is performed once before NBomber starts:

- `POST /api/auth/login`
- session token is kept in memory
- authenticated requests send `X-Session-Token`

`product_list` is required. Invoice and BlackBox scenarios are optional because permissions may not be granted to every test user.

## Results

Reports are written to:

```text
scripts/performance/results
```

The results folder is ignored by git. Save important snapshots with descriptive names when comparing before/after runs.

## Build

```powershell
dotnet build scripts/performance/BazarFlow.PerformanceTests/BazarFlow.PerformanceTests.csproj
```
