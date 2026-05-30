# Performance Test Results Template

## Test Summary

| Field | Value |
| --- | --- |
| Test name |  |
| Date/time |  |
| App version/tag |  |
| Tester |  |
| Environment | Local / LAN / Other |
| Test type | Smoke / Load / Stress / Soak / DB growth / BlackBox write-heavy |
| Pass/fail decision | Pass / Fail / Inconclusive |

## Machine Specs

| Field | Value |
| --- | --- |
| Machine name |  |
| CPU |  |
| RAM |  |
| Disk type/free space |  |
| OS version |  |
| Network mode | Localhost / LAN |
| Windows Service mode | Yes / No |

## SQL Server

| Field | Value |
| --- | --- |
| SQL Server edition |  |
| SQL Server version |  |
| Database name |  |
| Initial database size |  |
| Final database size |  |
| Recovery model |  |

## Dataset Profile

| Field | Value |
| --- | --- |
| Profile | Small / Medium / Large / Custom |
| Products |  |
| Categories |  |
| Suppliers |  |
| Customers |  |
| Employees/test users |  |
| Historical invoices |  |
| Invoice items |  |
| Purchases |  |
| Inventory movements |  |
| BlackBox events |  |
| History range |  |

## Test Configuration

| Field | Value |
| --- | --- |
| Concurrent users |  |
| Duration |  |
| Ramp-up |  |
| Tool |  |
| Test command |  |
| Target API URL |  |
| Frontend involved | Yes / No |
| Backup/export involved | Yes / No |

## Endpoint Metrics

| Scenario | Endpoint/API | Requests | Req/sec | p50 | p95 | p99 | Min | Max | Error rate | Notes |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | --- |
| Login |  |  |  |  |  |  |  |  |  |  |
| Load products |  |  |  |  |  |  |  |  |  |  |
| Product search/barcode |  |  |  |  |  |  |  |  |  |  |
| Complete invoice |  |  |  |  |  |  |  |  |  |  |
| Print receipt |  |  |  |  |  |  |  |  |  |  |
| Create/update product |  |  |  |  |  |  |  |  |  |  |
| Complete purchase |  |  |  |  |  |  |  |  |  |  |
| Inventory adjustment |  |  |  |  |  |  |  |  |  |  |
| Reports |  |  |  |  |  |  |  |  |  |  |
| BlackBox write |  |  |  |  |  |  |  |  |  |  |
| BlackBox read/filter |  |  |  |  |  |  |  |  |  |  |
| Backup/export |  |  |  |  |  |  |  |  |  |  |

## Business Metrics

| Metric | Value | Notes |
| --- | ---: | --- |
| Invoices/minute |  |  |
| Invoice items/minute |  |  |
| Product searches/minute |  |  |
| BlackBox events/minute |  |  |
| Purchases/minute |  |  |
| Inventory adjustments/minute |  |  |
| Report requests/minute |  |  |
| Backup/export duration |  |  |

## SQL Metrics

| Metric | Value | Notes |
| --- | ---: | --- |
| Slowest query duration |  |  |
| Highest logical reads |  |  |
| Highest CPU query |  |  |
| Blocking observed |  | Yes / No |
| Deadlocks observed |  | Yes / No |
| Missing index signals |  |  |
| Largest table |  |  |
| Largest index |  |  |
| DB growth during test |  |  |

## Machine Metrics

| Metric | Minimum | Average | Maximum | Notes |
| --- | ---: | ---: | ---: | --- |
| API/Service CPU |  |  |  |  |
| API/Service RAM |  |  |  |  |
| SQL Server CPU |  |  |  |  |
| SQL Server RAM |  |  |  |  |
| Disk activity |  |  |  |  |
| Network latency |  |  |  |  |

## Errors

| Time | Scenario | Error | Count | Notes |
| --- | --- | --- | ---: | --- |
|  |  |  |  |  |

## Observations

- 

## Pass/Fail Decision

Decision:

- Pass / Fail / Inconclusive

Reason:

- 

Follow-up bottleneck reports:

- 
