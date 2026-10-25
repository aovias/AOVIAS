# 10 — Health & Monitoring

**Date:** 26 July 2026

---

## Health Endpoints (HTTP Services)

Via `Shared.Logging` → `MapPramanAoviasPlatform()`:

| Route | Role |
|-------|------|
| `/health` | Liveness-style |
| `/health/ready` | Readiness |
| `/liveness` | Alias |
| `/readiness` | Alias |
| `/platform/registration` | Platform registration metadata |

Most domain services register `AddHealthChecks` + `AddDbContextCheck`.

**Gateway:** health mapped; no DB check (correct — Gateway is DB-less).

**OutboxRelayWorker:** no HTTP health — **gap**. Recommend process-level probe or minimal Kestrel health later.

---

## Logging

| Aspect | Status |
|--------|--------|
| Serilog bootstrap | All HTTP services + OutboxRelayWorker |
| Correlation IDs | Platform middleware |
| Structured logging | Serilog |
| OpenTelemetry | **Absent** (CRP / CR-T4) |
| FE error tracking | TODO Sentry (WebApp) |

---

## Background Jobs Observability

| Job | Visibility |
|-----|------------|
| Hangfire (Compliance) | `/hangfire` — **must authenticate in Production** |
| Outbox processors | Logs only |
| Communication outbound | Logs; console email |
| Analytics snapshots | Log errors when unauthenticated (known Hangfire context issue) |

---

## Phase 0 Status

Health endpoints **operational** on running services (verified HTTP 200 on core stack). Monitoring is **not** production-grade without OTel + secured Hangfire + worker probes.

---

## Next

1. Auth-protect Hangfire  
2. Add OutboxRelayWorker health/heartbeat  
3. Introduce OpenTelemetry exporters  
4. Fix Hangfire analytics snapshot to use system scope (stop UserContext auth failures)
