# PRAMAN Enterprise Security Gap Analysis — EIWAM Phase 7

| Field | Value |
|-------|-------|
| **Phase** | EIWAM Phase 7 — Enterprise Security |
| **Prior work** | M6 Identity Security Foundation |
| **Status** | Investigation complete — implementation authorized |
| **Bounded context** | IdentityService (evolve in place) |

## Executive summary

EIWAM Phase 7 evolves **IdentityService** into an enterprise security platform with adaptive authentication, threat detection, risk scoring, device intelligence, and operational dashboards. **Do not create a competing SecurityService.**

Authentication (JWT, MFA, sessions, password) remains stable. Phase 7 adds **security intelligence** on top of existing M6 primitives.

---

## Investigation scope

| Area | Locations reviewed |
|------|-------------------|
| Sessions | `IdentityService/Domain/Entities/UserSession.cs`, `SessionService.cs` |
| Devices | `TrustedDevice.cs` (schema only) |
| Policies | `UserSecurityPolicy.cs`, `SecurityPolicyService.cs` |
| Audit | `IdentityAuditLog.cs`, `IdentityAuditService.cs` |
| Login telemetry | `LoginAttempt.cs` |
| Auth flows | `AuthenticationService.cs`, `AuthController.cs` |
| Outbox | `OutboxMessage.cs`, `IdentityOutboxProcessor.cs` |
| Gateway | `Gateway/Controllers/IdentityController.cs` |
| Angular | `WebApp/.../account-security.component.ts` |

---

## Already implemented (M6)

| Capability | Implementation |
|------------|----------------|
| JWT + refresh rotation | `TokenService`, `SessionService` with token family reuse detection |
| Sessions | Create, list, revoke, concurrent limit, `LastActivityAt` |
| MFA | TOTP setup/verify, recovery codes, login challenge |
| Password | History, reset, change, policy-driven complexity |
| Lockout | ASP.NET Identity lockout + policy thresholds |
| Security policy | Platform `UserSecurityPolicy` (password, MFA admin, session limits) |
| Audit | `IdentityAuditLog` with event codes, IP, UA, OS |
| Login attempts | `LoginAttempt` on every login success/failure |
| Client context | `ClientContextParser` (IP, UA, OS) |
| Outbox infrastructure | Table + processor (mark processed) |
| Cleanup jobs | Session/token/MFA challenge purge |
| Gateway auth proxy | `api/v1/identity/*` |

---

## Reusable (extend, do not duplicate)

| Asset | Reuse pattern |
|-------|---------------|
| `UserSession` | Extend with fingerprint, geo, risk score |
| `TrustedDevice` | Wire lifecycle service; extend fields |
| `UserSecurityPolicy` | Add risk/device/session thresholds |
| `IdentityAuditLog` | Keep canonical audit; dual-write to `SecurityEvent` |
| `LoginAttempt` | Feed risk engine + timeline |
| `ClientContextParser` | Extend browser parsing |
| `IdentityOutboxProcessor` | Same mark-processed pattern as Phase 5/6 |
| `account-security` Angular | Link to new security dashboard |

---

## Needs enhancement

| Area | Current | Phase 7 target |
|------|---------|----------------|
| **UserSession** | Basic device/IP/UA | Fingerprint, geo, risk score, suspicious flag |
| **TrustedDevice** | Unused table | Trust/register/revoke/expire APIs |
| **UserSecurityPolicy** | Password/MFA/lockout | Risk thresholds, device trust, impossible travel |
| **Security events** | Audit write-only | Queryable `SecurityEvent` stream |
| **Risk scoring** | None | `UserRiskScore` + deterministic engine |
| **Threat detection** | Refresh reuse only | Impossible travel, concurrent distant login |
| **Adaptive auth** | Static policy gates | Risk-based step-up MFA |
| **Dashboard** | None | `/security/dashboard` KPIs |
| **Timeline** | No read API | `/security/timeline` |
| **Outbox** | `UserRegistered` only | Full security event catalog |
| **Gateway** | Auth routes only | `/security/*` proxy |
| **Angular** | Password + sessions | Full security admin screens |

---

## Missing

| Capability | Impact |
|------------|--------|
| `ENTERPRISE_SECURITY_GAP_ANALYSIS.md` | This document |
| `ENTERPRISE_SECURITY_DOMAIN_MODEL.md` | Phase 7 framing |
| `SecurityEvent`, `UserRiskScore`, `SessionHistory`, `SecurityIncident` | Core telemetry |
| Security application services | Engines + dashboard |
| `/api/v1/security/*` APIs | Operational visibility |
| MediatR security module | CQRS for new endpoints |
| Security Angular feature | Enterprise dashboards |
| Security unit/integration tests | Validation |

---

## Technical debt

1. **TrustedDevice schema-without-behavior** — table exists, zero service/API references.
2. **No security read APIs** — audit and login data trapped in DB.
3. **Outbox stub** — no RabbitMQ relay (platform-wide).
4. **Policy cache** — `IdentitySecurityPolicyState.Current` in-memory; extend for new fields.
5. **No geo provider** — IP geolocation stubbed for Phase 7.
6. **Access vs security boundary** — AccessGovernance owns permissions; Identity owns authentication telemetry.

---

## Definition of Done checklist

- [ ] Gap analysis and domain model complete
- [ ] Sessions provide security intelligence
- [ ] Trusted device lifecycle operational
- [ ] Configurable security policies extended
- [ ] User risk scoring operational
- [ ] Security events queryable + auditable
- [ ] Threat detection extensible
- [ ] Dashboard + Angular UI
- [ ] Tests + sequence docs

---

## Related documents

- [ENTERPRISE_SECURITY_DOMAIN_MODEL.md](./ENTERPRISE_SECURITY_DOMAIN_MODEL.md)
- [16-Security-Architecture.md](./16-Security-Architecture.md)
- [ACCESS_GOVERNANCE_GAP_ANALYSIS.md](./ACCESS_GOVERNANCE_GAP_ANALYSIS.md)
