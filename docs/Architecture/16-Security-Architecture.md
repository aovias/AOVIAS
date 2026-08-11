# Security Architecture

[← README](./README.md)

---

## Authentication

| Mechanism | Detail |
|-----------|--------|
| Primary | JWT access tokens (IdentityService `AuthController`) |
| Refresh | Refresh token stored server-side |
| Login API | Gateway proxies `/api/v1/identity/login` |
| SPA storage | `TokenService`, `SessionService` |

IdentityServer (Duende) present in dev for OAuth2/OIDC — SPA uses JWT path.

---

## Authorization

| Layer | Implementation |
|-------|----------------|
| API scopes | `compliance.api`, `organization.api`, `audit.api`, `ai.api` |
| Policies | `ComplianceScope`, `OrganizationScope`, `TenantScope`, `OrgAccess`, `AdminOnly` |
| Hospital context | `X-Hospital-Id` header + `ValidateHospitalId` filter |
| Organization | `OrganizationAccessHandler`, failure middleware |

---

## RBAC

- Role claims in JWT; `roleGuard` on selective routes.
- Framework guard for framework-scoped features.
- Member designations (OrganizationService migration `Sprint9B_MemberDesignation`).

---

## Audit Trails

| Domain | Trail |
|--------|-------|
| Findings | `FindingAuditTrail` |
| Deficiencies | `DeficiencyAuditTrail` |
| Copilot proposals | `CopilotProposalAuditEvents` |
| Journey | `ComplianceJourneyHistory` |
| API | `AuditLogsController` |

---

## Data Protection Readiness

| Standard | Status |
|----------|--------|
| **DPDP** (India) | **Partial** — tenant isolation, audit trails; formal DPDP module not implemented |
| **HIPAA** | **Partial** — healthcare context; BAA/encryption policies not fully documented in code |
| Encryption in transit | TLS (HTTPS in prod target) |
| Secrets | `appsettings`, env vars — **no** vault integration in repo |
| PII | User email in JWT claims; hospital identifiers in headers |

---

## Evidence Integrity

- Evidence versions tracked.
- Verification status authoritative in evidence module.
- Copilot does not delete evidence.

---

## AI Security

- Deterministic recommendations (auditable rationale JSON).
- No autonomous execution without approved proposal.
- AIOrchestrator separate from Copilot write path.

---

## Hardening Gaps

- Rate limiting on Gateway (**present** — verify config for prod).
- Master data seeder DI error on startup (non-fatal).
- Activity `ActorId` as string inconsistency (TD-L03).
