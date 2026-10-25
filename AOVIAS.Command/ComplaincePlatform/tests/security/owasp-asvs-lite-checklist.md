# OWASP ASVS Lite checklist (Phase 7)

Lightweight control spot-check for commercial pilot — **not** a full ASVS Level 2 assessment and **not** a paid penetration test.

**Scope:** Gateway edge + Identity auth surfaces already present in AOVIAS Command.  
**Date baseline:** 2026-07-26 (Phase 7 Sprint 6).

## How to use

1. Run automated header scan (when Gateway is up):

```powershell
.\scripts\ops\scan-security-headers.ps1
.\scripts\ops\scan-security-headers.ps1 -BaseUrl http://localhost:5000
```

Soft-fail (exit 0 + warning) when Gateway is unreachable; hard-fail (exit 1) when Gateway responds without required headers.

2. Walk MANUAL rows on a demo stack; tick Pass / Fail / N/A.

---

## V1 — Architecture & threat modeling (lite)

| ID | Control | Mode | Evidence / how | Status |
|----|---------|------|----------------|--------|
| L-1.1 | Trusted boundary at Gateway | MANUAL | SPA → Gateway only; services not exposed publicly in compose | |
| L-1.2 | Correlation ID propagated | MANUAL | `X-Correlation-ID` on requests; logs enriched | |
| L-1.3 | Secrets not in repo base appsettings | MANUAL | ProductionSecretsValidator + Key Vault path (Phase 7.1) | |

## V2 — Authentication

| ID | Control | Mode | Evidence / how | Status |
|----|---------|------|----------------|--------|
| L-2.1 | Login over TLS in non-dev | MANUAL | HSTS + HTTPS redirect outside Development | |
| L-2.2 | Admin MFA enforceable | MANUAL | `RequireMfaForAdminRoles` default true | |
| L-2.3 | Session / refresh token logout | MANUAL | `/api/v1/identity/logout` + sessions API | |
| L-2.4 | JWT rotation window | MANUAL | Dual-key `PreviousSecretKey` runbook in `01-enterprise-security.md` | |

## V3 — Session management

| ID | Control | Mode | Evidence / how | Status |
|----|---------|------|----------------|--------|
| L-3.1 | Bearer tokens not in query string | MANUAL | Authorization header only on APIs | |
| L-3.2 | Logout-all available | MANUAL | `POST .../logout-all` | |

## V4 — Access control

| ID | Control | Mode | Evidence / how | Status |
|----|---------|------|----------------|--------|
| L-4.1 | Tenant isolation | MANUAL / AUTO | OrgAccess handler + Phase 6 unit tests | |
| L-4.2 | Privilege audit export | MANUAL | AccessGovernance privilege-audit export | |
| L-4.3 | License entitlement gate | MANUAL | LicenseEntitlementMiddleware | |

## V5 — Validation / business logic (lite)

| ID | Control | Mode | Evidence / how | Status |
|----|---------|------|----------------|--------|
| L-5.1 | ApiResponse envelope (no raw stack to clients) | MANUAL | ExceptionHandlingMiddleware + ApiResponseFactory | |
| L-5.2 | Idempotency on mutating Gateway routes | MANUAL | IdempotencyMiddleware (+ Redis when configured) | |

## V7 — Error handling & logging

| ID | Control | Mode | Evidence / how | Status |
|----|---------|------|----------------|--------|
| L-7.1 | Structured logging | MANUAL | Serilog / Praman logging | |
| L-7.2 | Health vs readiness separation | MANUAL | `/health`, `/readiness` | |

## V14 — Configuration

| ID | Control | Mode | Evidence / how | Status |
|----|---------|------|----------------|--------|
| L-14.1 | Security headers on Gateway | **AUTO** | `SecurityHeadersMiddleware` + `scan-security-headers.ps1` | |
| L-14.2 | Maintenance mode | MANUAL | `MAINTENANCE_MODE` / `maintenance.lock` → 503 (Phase 7.6 when present) | |

### Required security headers (AUTO)

| Header | Expected (Gateway) |
|--------|--------------------|
| `X-Content-Type-Options` | `nosniff` |
| `X-Frame-Options` | `DENY` |
| `Content-Security-Policy` | present (baseline API CSP) |

Also set by middleware (informational): `Referrer-Policy`, `Permissions-Policy`, `Cross-Origin-Opener-Policy`.

## Explicitly out of scope (honest)

| Item | Why |
|------|-----|
| Full OWASP ASVS L2 workbook | Lite checklist only |
| Paid third-party pen-test | Commercial / Phase 8+ follow-on |
| Multi-region HA security review | Not implemented in Phase 7 |
| Full blob storage cutover security review | Local storage remains default; Azure Blob stub/config only |

## Exit

Operators can run the header scan against a live Gateway and complete MANUAL ticks before pilot. Residual risk accepted until paid pen-test and full ASVS engagement.
