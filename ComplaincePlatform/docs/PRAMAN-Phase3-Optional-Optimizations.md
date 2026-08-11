# PRAMAN Phase 3 Optional Optimizations

This document captures optional post-validation enhancements to be executed after
Phase 1 and Phase 2 metrics show stable gains.

## Preconditions

- First-load, navigation, and API latency metrics from Phase 1/2 are stable.
- No regression in Journey / Journey Execution behavior.
- Request-cost telemetry is enabled in at least staging.

## 1) Gateway aggregation cache (optional)

### Goal

Reduce repeated network hops from WebApp -> Gateway -> ComplianceService for
split journey reads in burst navigation scenarios.

### Candidate scope

- Short-TTL cache in Gateway handlers for:
  - `/journeys/{id}/session`
  - `/journeys/{id}/dashboard`
  - `/journeys/{id}/chapters`
- Key dimensions:
  - journeyId
  - operationalMode (if provided)
  - auditLifecycleMode (if provided)
  - org/tenant context

### Constraints

- Keep all existing route contracts unchanged.
- TTL must be short (for example 15-30 seconds) to avoid stale read surprises.
- Disable by configuration in production until validated.

## 2) Journey prefetch strategy (optional)

### Goal

Improve perceived responsiveness by prefetching likely next data in idle time.

### Candidate client strategy

- After chapter standards load succeeds:
  - prefetch standards for adjacent chapter
- After selecting standard:
  - prefetch OE detail for top-ranked likely next OE

### Constraints

- Respect existing in-memory cache keys.
- Never prefetch if network is offline / request load is high.
- Keep behind feature flags.

## 3) Suggested feature flags

- `journeyGatewayReadCacheEnabled`
- `journeyGatewayReadCacheTtlSeconds`
- `journeyPrefetchEnabled`
- `journeyPrefetchMaxConcurrent`

## 4) Validation checklist

- Compare P50/P95 for:
  - first journey load
  - chapter switch
  - OE context open
- Track API hit rate and cache hit/miss ratio.
- Ensure no stale-data support tickets increase.
