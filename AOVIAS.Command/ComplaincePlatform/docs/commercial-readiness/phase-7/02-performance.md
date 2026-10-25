# 02 — Performance

**Phase:** 7.2  
**Sprint:** 2  
**Date:** 2026-07-26

## Delivered

| Capability | Implementation |
|------------|----------------|
| Redis distributed cache | `ConnectionStrings:Redis` or `Redis:Configuration` → `AddStackExchangeRedisCache` on Gateway + ComplianceService |
| Idempotency cache | `IdempotencyMiddleware` uses `IDistributedCache` when Redis set; `IMemoryCache` fallback |
| Dashboard / framework caches | `DashboardSummaryCache`, `FrameworkCatalogMemoryCache` hybrid distributed + memory |
| Hot-path query bound | Control Center tasks: `AsNoTracking` + `OrderByDescending(CreatedAt).Take(2000)` |
| Evidence binder UI | CDK virtual scroll on objective list when count > 40 |
| Hangfire dashboard | Non-Development requires `HangfireAuthorizationFilter` (ComplianceScope); Development uses `LocalRequestsOnlyAuthorizationFilter` |

## Redis

```bash
# Optional profile in docker-compose.dev.yml
docker compose -f docker-compose.dev.yml --profile redis up -d redis-dev
```

Set on Gateway / ComplianceService:

```json
{
  "ConnectionStrings": {
    "Redis": "localhost:6379"
  }
}
```

Or `Redis:Configuration` (same connection string format StackExchange.Redis accepts).

## Hangfire (`/hangfire` on ComplianceService)

| Environment | Authorization |
|-------------|---------------|
| Development | Local requests only |
| Staging / Production | Authenticated user + `ComplianceScope` policy |

Never deploy with an empty `DashboardOptions.Authorization` list — Hangfire treats that as allow-all.

## Exit

Caches degrade gracefully without Redis; Control Center task load is bounded; Hangfire is auth-gated outside Development.
