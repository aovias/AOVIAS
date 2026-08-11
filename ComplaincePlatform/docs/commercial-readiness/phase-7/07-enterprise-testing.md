# 07 — Enterprise Testing

**Phase:** 7.7  
**Sprint:** 6  
**Date:** 2026-07-26

## Purpose

Close Phase 7 with repeatable **load**, **chaos**, and **security lite** artifacts so operators can validate enterprise hardening before pilot — without claiming a full capacity study or paid pen-test.

## Artifacts

| Area | Path |
|------|------|
| k6 load scripts | [`tests/load/`](../../../tests/load/) |
| Load runbook | [`tests/load/README.md`](../../../tests/load/README.md) |
| Chaos drills | [`tests/load/chaos-checklist.md`](../../../tests/load/chaos-checklist.md) |
| OWASP ASVS lite | [`tests/security/owasp-asvs-lite-checklist.md`](../../../tests/security/owasp-asvs-lite-checklist.md) |
| Header scan | [`scripts/ops/scan-security-headers.ps1`](../../../scripts/ops/scan-security-headers.ps1) |
| Validation hook | [`scripts/ops/validate-production.ps1`](../../../scripts/ops/validate-production.ps1) (`-RunLoad`) |

## Load (k6)

| Script | Target | Notes |
|--------|--------|-------|
| `auth-login.js` | `POST /api/v1/identity/login` or `GET /health` | Login when `LOGIN_EMAIL`/`LOGIN_PASSWORD` set; else health smoke (MFA/org complexity) |
| `dashboard.js` | `/health` + `/api/v1/dashboard/summary` + `/api/v1/role-experience/home` | Thresholds on failure rate / p95; `TOKEN` enables auth paths |
| `search.js` | `GET /api/v1/compliance/search` | Requires `TOKEN` for meaningful load |
| `soak.js` | Lower rate, longer duration | `SOAK_DURATION` / `SOAK_RATE` tunable |

```powershell
$env:BASE_URL = "http://localhost:5000"
$env:TOKEN = "<jwt>"
k6 run tests/load/dashboard.js

# Best-effort via ops script (skips if k6 missing)
.\scripts\ops\validate-production.ps1 -RunLoad
```

## Chaos

Manual drills in `chaos-checklist.md`:

1. **Redis down** — cache/SignalR backplane loss; Gateway must not crash.
2. **AIOrchestrator kill** — AI paths degrade; core compliance remains usable.
3. **SQL restart** — EF retry window absorbs bounce; readiness fails on prolonged outage.

## Security lite

| Check | How |
|-------|-----|
| Headers AUTO | `scan-security-headers.ps1` asserts `X-Content-Type-Options`, `X-Frame-Options`, `Content-Security-Policy` |
| Soft-fail offline | Script exits 0 with warning if Gateway unreachable |
| Hard-fail missing headers | Exit 1 when Gateway responds without required headers |
| Middleware | `Gateway/Middleware/SecurityHeadersMiddleware.cs` (Phase 7.1) |
| Manual ticks | ASVS lite checklist (auth, tenant isolation, secrets, logging) |

```powershell
.\scripts\ops\scan-security-headers.ps1
.\scripts\ops\scan-security-headers.ps1 -BaseUrl http://localhost:5000
```

## Exit

Operators can run k6 smoke/soak, execute chaos drills, and pass the header scan against a live Gateway. Full ASVS L2, paid pen-test, and multi-region HA proof remain follow-on.
