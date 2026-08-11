# 06 — Enterprise Operations

**Phase:** 7.6  
**Sprint:** 5  
**Date:** 2026-07-26

## Delivered

| Capability | Implementation |
|------------|----------------|
| Maintenance mode | `Gateway/Middleware/MaintenanceModeMiddleware.cs` — `MAINTENANCE_MODE=true` or `App_Data/maintenance.lock` → HTTP 503 + `Retry-After: 60` (exempts health, `/platform`, setup, `/api/v1/ops/*`) |
| Helm sample | `charts/aovias/` — Gateway + ComplianceService Deployments/Services/Ingress/ConfigMap; rolling update `maxUnavailable: 0`, `maxSurge: 1` |
| Web UI maintenance | WebApp route `/maintenance` (reuses `MaintenanceComponent`) |
| Capacity worksheet | Below — 100-hospital sizing starting point |

## Maintenance activation

```bash
# Env (compose / k8s)
MAINTENANCE_MODE=true

# Or file flag on Gateway content root
# App_Data/maintenance.lock
```

UI users can be directed to `/maintenance` on the Angular host while APIs return 503 JSON.

## Capacity planning worksheet (100 hospitals)

Assumptions: multi-tenant shared plane, ~50 concurrent users per hospital at peak (burst), average evidence upload 5 MB, nightly backup RPO ≤ 24h.

| Resource | Starter size (100 hospitals) | Notes / scale trigger |
|----------|------------------------------|------------------------|
| **Gateway replicas** | 2–3 | CPU &gt; 70% sustained or p95 proxy latency &gt; 1s |
| **ComplianceService replicas** | 2–3 | Hangfire + projection backlog; outbox poison growth |
| **Identity / Organization** | 2 each | Auth spikes at shift change |
| **SQL (shared or elastic pool)** | 4–8 vCore, 256–512 GB | DTU/vCore &gt; 80%; separate Compliance DB if I/O bound |
| **Redis / cache (if used)** | 1 GB | Dashboard summary cache miss storms |
| **Object storage (evidence)** | ~5–10 TB year-1 | 100 hospitals × evidence growth; lifecycle policies |
| **Backup storage** | 2× DB size + 14-day retention | Align with Phase 6 `backup-databases.ps1` |
| **Observability retention** | 14–30 days logs | AOVIAS observability ingest volume |

### Concurrent load sketch

| Metric | Estimate |
|--------|----------|
| Peak concurrent users | ~5,000 (100 × 50) |
| Peak RPS through Gateway (interactive) | 200–500 |
| Evidence uploads / hour (peak) | 100–300 |
| Outbox messages / hour | depends on journey transitions; alert if poison &gt; 50 |

### Worksheet (fill per site)

| Item | Value | Owner |
|------|-------|-------|
| Hospital count | 100 | |
| Peak concurrent users | | |
| Gateway CPU request/limit | 250m / 1 | |
| Compliance memory limit | 1536Mi | |
| SQL SKU | | |
| Evidence store SKU / region | | |
| RPO achieved (last DR drill) | | |
| RTO achieved (last DR drill) | | |

## Helm rolling updates

See `charts/aovias/README.md`. Prefer image-tag bumps + `helm upgrade` with readiness probes; use maintenance mode for breaking schema cutovers.

## Exit

Maintenance gate live; Helm sample for Gateway + Compliance; WebApp `/maintenance`; capacity worksheet for 100-hospital pilots.
