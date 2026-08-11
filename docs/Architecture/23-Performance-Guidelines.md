# Performance Guidelines

[← README](./README.md)

---

## Caching

| Layer | Approach |
|-------|----------|
| Frontend lookups | `CommonLookupService` caches |
| Backend | No global Redis; snapshot tables for heavy reads |
| HTTP | Standard cache headers **not** widely used on APIs |

---

## EF Core

- Use `AsNoTracking()` on read-only queries where applied.
- Avoid N+1: **known issue** Copilot persona metrics (TD-H01).
- Explicit precision on decimals where configured (Copilot confidence).

---

## Indexes

- Org/hospital indexes on operational tables.
- Copilot: persona+status composite on recommendations.
- Review migration files for new indexes when adding filters.

---

## Snapshots

Prefer snapshot reads for dashboard/analytics over live aggregation of full history.

---

## Background Jobs (Hangfire)

| Job | Schedule |
|-----|----------|
| Nightly compliance snapshot | 2 AM UTC |
| Compliance expiry | 1 AM UTC |
| Escalation | 3 AM UTC |
| Analytics daily snapshot | 4 AM UTC |

Long-running work off request path.

---

## Frontend

- Lazy-loaded feature routes.
- OnPush + signals reduce re-renders.
- `journeyLongReadInterceptor` for long-running journey API tolerance.

---

## Scalability Targets (Engineering goals — not SLAs)

| Metric | Target direction |
|--------|------------------|
| API P95 | < 500ms for reads (excluding refresh/rule engine) |
| Copilot refresh | May take seconds — show loading state |
| Concurrent hospitals | Multi-tenant org isolation at DB query level |

---

## Known Performance Debt

See `ComplaincePlatform/docs/PRAMAN-Performance-Audit-Optimization-Report.md` and TD-H01, TD-M03 in technical debt register.
