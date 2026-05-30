# BazarFlow Performance Test Plan

## Executive Verdict

V2-06 establishes a local-first performance and load testing discipline for BazarFlow before commercial release. The objective is to measure real limits and identify bottlenecks in the ASP.NET Core Web API, Angular frontend, SQL Server database, Windows Service hosting model, LAN deployment mode, BlackBox Activity Recorder, and backup/export paths.

This phase must not change business logic, runtime behavior, installer packaging, or production data. Performance testing starts with simple local measurements, then expands to controlled LAN and higher-load runs only after the baseline is repeatable.

## Performance Goals

- Validate that the system is stable for expected small-shop and medium-supermarket usage.
- Discover practical limits for larger local-market workloads without guessing.
- Measure critical API response times at p50, p95, and p99.
- Measure invoice throughput and product search capacity.
- Detect slow SQL queries, missing indexes, blocking, and database growth risks.
- Verify that BlackBox event recording does not materially slow critical POS flows.
- Verify that backup/export does not create unacceptable blocking during normal use.
- Produce repeatable results that can be compared across release candidates.

## Critical Scenarios

### Authentication

- Successful login.
- Failed login.
- Login rate limiting behavior.
- Session validation and authenticated user lookup, where applicable.

### Products

- Load product list.
- Search products by name.
- Search products by barcode.
- Create product.
- Update product.
- Product lookup during invoice entry.

### Sales And Invoicing

- Create invoice.
- Add invoice items.
- Complete invoice.
- Persist invoice items.
- Update inventory after sale.
- Generate receipt or receipt payload for printing.

### Purchases

- Create purchase.
- Add purchase items.
- Complete purchase.
- Update inventory after purchase.

### Inventory

- Create inventory adjustment.
- List inventory movements.
- Query low-stock or stock status views, where available.

### Reports

- Daily sales report.
- Date-range sales report.
- Product movement report.
- Inventory report.
- Employee or activity report, where available.

### BlackBox Activity Recorder

- Write BlackBox events during normal workflows.
- Read recent BlackBox events.
- Filter by date range.
- Filter by action type.
- Filter by employee/user.

### Backup And Export

- Trigger backup/export in a controlled non-production environment.
- Measure backup/export duration.
- Measure impact on concurrent login, search, and invoice completion.

## Load Profiles

### Small Shop

Dataset:

- 1-3 concurrent users.
- 500 products.
- 10 categories.
- 5 suppliers.
- 50 customers.
- 100 invoices/day.

Purpose:

- Baseline commercial readiness.
- Validate that common workflows are comfortably below acceptance thresholds.

### Medium Supermarket

Dataset:

- 5-10 concurrent users.
- 5,000 products.
- 50 categories.
- 30 suppliers.
- 500 customers.
- 1,000 invoices/day.

Purpose:

- Primary acceptance profile for the first commercial candidate.
- Validate sustained product search, barcode lookup, invoice completion, purchases, inventory changes, reports, and BlackBox event writes.

### Large Local Market

Dataset:

- 20-30 concurrent users.
- 20,000 products.
- 150 categories.
- 100 suppliers.
- 2,000 customers.
- 5,000 invoices/day.

Purpose:

- Exploratory stress and bottleneck discovery.
- Not automatically a first-release guarantee unless explicitly promoted to a release requirement.

## Dataset Strategy

All performance datasets must be synthetic.

Generated data should include:

- Products.
- Categories.
- Suppliers.
- Customers.
- Employees/test users.
- Historical invoices.
- Invoice items.
- Purchases.
- Purchase items.
- Inventory movements.
- BlackBox events.
- Audit logs, if separate from BlackBox.
- Sessions or authentication-related records needed by the app.

Dataset rules:

- Do not use real customer data.
- Do not test against a production database.
- Use deterministic generation where practical so results are comparable.
- Support separate `small`, `medium`, and `large` dataset profiles in later implementation phases.
- Generate realistic invoice item counts:
  - small: 1-10 items/invoice.
  - medium: 3-40 items/invoice.
  - large: 5-80 items/invoice.
- Generate history across 30-180 days for report testing.
- Generate BlackBox volume proportional to business activity.

## Recommended Tools

### NBomber

NBomber is the recommended primary load testing tool because it fits the .NET ecosystem and supports realistic scenario-based API testing.

Use NBomber later for:

- Smoke performance tests.
- Load tests.
- Stress tests.
- Soak tests.
- Invoice throughput tests.
- BlackBox write-heavy tests.

### SQL Server DMVs

SQL Server Dynamic Management Views should be used to inspect database behavior before and after test runs.

Use DMVs later for:

- Top expensive queries.
- Long-running queries.
- High logical reads.
- Blocking.
- Wait stats.
- Missing index signals.
- Index usage.
- Table and database size growth.

### PowerShell Orchestration

PowerShell should be used as orchestration glue, not as the main load generator.

Use PowerShell later for:

- Starting test runs.
- Capturing environment details.
- Collecting logs and result files.
- Measuring process CPU/RAM.
- Running approved SQL DMV snapshots.
- Invoking backup/export impact tests in controlled environments.

## Test Designs

### Smoke Performance Test

Purpose:

- Confirm scripts, authentication, seeded data, and critical endpoints are working before longer tests.

Profile:

- Small dataset.
- 1 concurrent user.
- 2-5 minutes.

Scenarios:

- Login.
- Product search.
- Complete invoice.
- Write BlackBox event.
- Run a simple report.

Pass criteria:

- No test setup failures.
- No authentication/session failures unrelated to the scenario.
- Error rate below 1%.

### Load Test

Purpose:

- Validate expected commercial usage.

Profiles:

- Small: 1-3 concurrent users.
- Medium: 5-10 concurrent users.
- Optional large discovery run: 20 concurrent users.

Duration:

- 10-20 minutes per profile.

Suggested workload mix:

- 35% product/barcode search.
- 25% complete invoice.
- 10% load products.
- 10% BlackBox read/write.
- 5% create/update product.
- 5% purchases.
- 5% inventory adjustment.
- 5% reports.

### Stress Test

Purpose:

- Find the practical breaking point and first major bottleneck.

Profile:

- Gradual ramp: 1, 3, 5, 10, 15, 20, 30 concurrent users.
- 3-5 minutes per step.

Stop conditions:

- Error rate above 5%.
- Invoice completion p95 above 3 seconds.
- Product search p95 above 1 second.
- Sustained CPU saturation.
- Severe SQL blocking.
- Abnormal memory growth.

### Soak Test

Purpose:

- Detect obvious resource leaks or degradation over time.

Profile:

- Medium dataset.
- 5-10 concurrent users.
- 30-60 minutes.

Scenarios:

- Product search.
- Invoice completion.
- BlackBox writes.
- Periodic reports.

Pass criteria:

- No obvious memory leak.
- No increasing error trend.
- No severe response-time degradation over the run.
- Windows Service remains responsive.

### DB Growth Test

Purpose:

- Estimate growth from invoices, invoice items, inventory movements, audit data, and BlackBox events.

Suggested simulated periods:

- 1 day.
- 7 days.
- 30 days.
- Optional 180 days.

Measurements:

- Database size.
- Largest tables.
- Index size.
- Invoice row count.
- Invoice item row count.
- BlackBox row count.
- Report duration as history grows.

### BlackBox Write-Heavy Test

Purpose:

- Validate the Activity Recorder under elevated event volume.

Scenarios:

- Login events.
- Invoice events.
- Product create/update events.
- Inventory adjustment events.
- Backup/export events.
- Report access events.

Measurements:

- BlackBox event write latency.
- BlackBox event read/filter latency.
- Table growth.
- Index effectiveness.
- Impact on invoice completion.

## Metrics

### API Metrics

- Response time p50.
- Response time p95.
- Response time p99.
- Requests/sec.
- Concurrent users.
- Error rate.
- Timeout count.

### Machine Metrics

- ASP.NET Core / Windows Service CPU.
- ASP.NET Core / Windows Service RAM.
- SQL Server CPU.
- SQL Server RAM.
- Disk activity during backup/export.
- LAN latency when running non-local tests.

### Database Metrics

- Query duration.
- Logical reads.
- CPU time per expensive query.
- Blocking.
- Deadlocks.
- Missing index signals.
- Table size growth.
- Index size growth.
- Total DB size growth.

### Business Metrics

- Invoices/minute.
- Invoice items/minute.
- Product searches/minute.
- BlackBox events/minute.
- Backup/export duration.
- Report duration by date range.

## Acceptance Criteria

Initial realistic targets:

| Scenario | Target |
| --- | ---: |
| Login | < 500ms p95 |
| Load products | < 800ms p95 |
| Product search/barcode | < 300ms p95 |
| Complete invoice | < 1s p95 |
| Print receipt payload | < 500ms p95 |
| Create/update product | < 700ms p95 |
| Complete purchase | < 1.5s p95 |
| Inventory adjustment | < 700ms p95 |
| Common reports | < 3s p95 |
| BlackBox event write | < 150ms p95 |
| BlackBox event read/filter | < 1s p95 |
| Error rate | < 1% |
| Medium profile stability | 10 concurrent users stable |
| Soak test | No obvious memory leak in 30-60 minutes |

Backup/export is measured first. A hard acceptance target should be set after the first controlled baseline because duration depends heavily on database size and disk speed.

## DB / Index Review Plan

Index changes should be evidence-driven. Do not add indexes based only on speculation.

### Products

Review:

- Barcode exact search.
- Name search.
- Category filters.
- Supplier filters.
- Active/inactive filters.

Likely candidates after evidence:

- Barcode index.
- Name/search index or normalized search support if already present.
- Category/status composite index if heavily used.

### Invoices And Invoice Items

Review:

- Invoice completion insert cost.
- Invoice detail loading.
- Reports by date.
- Reports by product.
- Reports by employee/cashier.

Likely candidates after evidence:

- Invoice date index.
- Invoice status/date index.
- Invoice item invoice id index.
- Invoice item product/date index.

### BlackBox Events

Review:

- CreatedAtUtc filtering.
- Action type filtering.
- Employee/user filtering.
- Recent event listing.

Likely candidates after evidence:

- CreatedAtUtc index.
- ActionType + CreatedAtUtc index.
- EmployeeId/UserId + CreatedAtUtc index.

### Audit Logs

Review if audit logs are separate from BlackBox:

- Date filtering.
- Actor filtering.
- Entity filtering.

### Sessions

Review:

- Active session lookup.
- Expiry cleanup.
- Revoked/active filtering.

Likely candidates after evidence:

- Session token/hash index.
- User id index.
- ExpiresAtUtc index.
- Active/revoked + expiry composite index.

### Inventory Movements

Review:

- Product movement history.
- Date range queries.
- Movement type filtering.

Likely candidates after evidence:

- ProductId + CreatedAtUtc index.
- MovementType + CreatedAtUtc index.

### Reports

Review:

- Actual generated SQL.
- Logical reads.
- Execution duration.
- Impact of date range size.
- Impact of historical data growth.

## Risks

### Windows Service Resource Limits

The Windows Service may show CPU, memory, thread, or handle growth under sustained load. The soak test should track process behavior over 30-60 minutes.

### SQL Express Limitations

SQL Express limits CPU, memory, and database size. Results must clearly state the SQL Server edition used.

### LAN Latency

Localhost results may not represent real cashier machines on LAN. LAN testing should be added only after local baselines are stable.

### BlackBox Table Growth

BlackBox events can grow quickly and affect reads or writes if not indexed correctly. V2-06 should measure growth and query cost before proposing retention or archive behavior.

### Heavy Reports

Reports can become expensive as invoice and inventory history grows. Report queries must be reviewed with medium and large datasets.

### Backup Blocking Performance

Backup/export may create disk or SQL contention. Impact should be measured in controlled non-production runs only.

### Installer Package Size

Performance assets should not be included in the commercial installer unless explicitly approved later.

### Rate Limiting Interference

Login tests may trigger intended rate limiting. Normal login performance and rate-limit behavior should be separate scenarios.

## Implementation Roadmap

### V2-06A - Documentation Artifacts

- Create performance test plan.
- Create result template.
- Create bottleneck report template.
- Create placeholder README under `scripts/performance`.

### V2-06B - Dataset Strategy And Seed Design

- Design synthetic dataset generator.
- Define small, medium, and large profiles.
- Add explicit safety checks to prevent production database usage.

### V2-06C - Performance Test Harness

- Add NBomber project.
- Add smoke and load scenarios.
- Add local execution instructions.

### V2-06D - SQL And Machine Metric Collection

- Add SQL DMV scripts.
- Add PowerShell orchestration.
- Add CPU/RAM collection.

### V2-06E - Baseline Runs

- Run small smoke.
- Run small load.
- Run medium load.
- Run medium soak.
- Capture first bottleneck report.

### V2-06F - Controlled Stress And Optimization Planning

- Run controlled stress outside production.
- Identify launch blockers.
- Propose performance fixes separately from the test plan.
