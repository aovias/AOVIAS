# 05 — Reliability

**Phase:** 7.5  
**Sprint:** 4  
**Date:** 2026-07-26

## Delivered

| Capability | Implementation |
|------------|----------------|
| Gateway HTTP resilience | Polly timeout (30s/attempt) + circuit breaker (5 failures / 30s break) + existing retries on Compliance, IdentityClient, OrganizationClient; Identity public client keeps connection-only retries |
| SQL transient retry | `EnableRetryOnFailure(5, 30s)` on Organization, Identity, AIOrchestrator, Communication, Governance, AccessGovernance, Audit |
| Graceful degradation | `Gateway/Resilience/GracefulDegradation.cs` — used by dashboard experience handler for 5xx read-path fallbacks |
| Outbox poison | Compliance `GET /api/v1/admin/outbox/poison` + `POST /api/v1/admin/outbox/{id}/replay` (SUPER_ADMIN / ORG_ADMIN); processors skip `RetryCount >= MaxRetries` (5) |

## RPO / RTO (references Phase 6 ops)

Targets and runbooks live in Phase 6; Phase 7 does not redefine them.

| Metric | Pilot target | Evidence / ops |
|--------|--------------|----------------|
| **RPO** | ≤ 24h (nightly full) | `docs/commercial-readiness/phase-6/06-backup-recovery.md`; `scripts/ops/backup-databases.ps1`; `GET /api/v1/ops/backup/status` |
| **RTO** | Operator-driven restore | Restore order Identity → Organization → Compliance (`scripts/ops/restore-database.ps1`); rollback via `scripts/ops/rollback.ps1` |
| **DR validation** | Record achieved vs target | `scripts/ops/validate-production.ps1` check `DR-04`; matrix in `phase-6/09-production-validation.md` |
| **Foundation GA stretch** | SQL RPO ≤ 1h | `docs/commercial-readiness/foundation/11-backup-recovery.md` |

## Operator notes

1. **Circuit open:** Gateway fails fast for ~30s after repeated 5xx/network errors to a named HttpClient — expect `SERVICE_UNAVAILABLE` envelopes, not hung requests.
2. **Poison outbox:** List via admin API; after fixing root cause, `POST .../replay` resets `RetryCount` so projection/relay resume.
3. **EF retries:** Aligns SQL services with Compliance-style resilience under transient Azure SQL / failover blips.

## Exit

Backend HttpClients resilient; SQL services retry on failure; poison outbox inspectable/replayable; RPO/RTO table points at Phase 6 scripts.
