# Performance Bottleneck Report Template

## Summary

Brief description of the bottleneck:

- 

## Severity

Severity:

- Launch blocker / High / Medium / Low / Observation

Reason:

- 

## Affected Scenario

| Field | Value |
| --- | --- |
| Test name |  |
| Load profile | Small / Medium / Large / Custom |
| Scenario |  |
| Concurrent users |  |
| Duration |  |
| Dataset profile |  |

## Endpoint / API

| Field | Value |
| --- | --- |
| Endpoint/API |  |
| HTTP method |  |
| Status codes observed |  |
| Error rate |  |

## SQL Query / Table / Index

Fill this section only when database evidence is available.

| Field | Value |
| --- | --- |
| Query or query pattern |  |
| Table(s) |  |
| Existing index involved |  |
| Missing index signal |  |
| Blocking observed | Yes / No |
| Deadlock observed | Yes / No |

## Evidence

### Response-Time Evidence

| Metric | Value |
| --- | ---: |
| p50 |  |
| p95 |  |
| p99 |  |
| Max |  |
| Requests/sec |  |
| Error rate |  |

### Logs

Relevant log references or excerpts:

- 

### SQL DMV Evidence

Relevant DMV output or saved result reference:

- 

### CPU / RAM Evidence

| Metric | Average | Maximum | Notes |
| --- | ---: | ---: | --- |
| API/Service CPU |  |  |  |
| API/Service RAM |  |  |  |
| SQL Server CPU |  |  |  |
| SQL Server RAM |  |  |  |
| Disk activity |  |  |  |

## Root Cause Hypothesis

Current hypothesis:

- 

Confidence:

- High / Medium / Low

Supporting evidence:

- 

Contradicting or missing evidence:

- 

## Proposed Fix

Proposed change:

- 

Expected impact:

- 

Scope:

- Backend / Frontend / Database / Infrastructure / Installer / Documentation

## Risk Of Fix

Risk level:

- High / Medium / Low

Risks:

- 

Rollback plan:

- 

## Verification Plan

After applying the proposed fix:

- Re-run test:
- Dataset profile:
- Concurrent users:
- Duration:
- Expected result:
- Metrics to compare:

Pass condition:

- 
