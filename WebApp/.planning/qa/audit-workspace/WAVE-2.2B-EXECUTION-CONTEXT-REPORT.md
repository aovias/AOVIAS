# Wave 2.2B — Enterprise Execution Context & Tenant Isolation (partial)

**Date:** 2026-08-02 · **Repo:** `ComplaincePlatform` (uncommitted)
**Implements:** ADR-002 (Wave 2.2A) — foundation + highest-risk security fix.

## Status — honest summary

Wave 2.2B as specified is a platform-wide refactor across 15 services, 63 endpoints, 84 filter
bypasses, background jobs and SignalR. **This session delivered the foundation and closed the most
severe security hole, with runtime proof. It did not complete the wave.**

| Phase | Status |
|---|---|
| 1 — Enterprise Execution Context | ✅ **Implemented** (shared library, builds clean) |
| 2 — Gateway hardening | ⚠️ **Partial** — 25 of 63 endpoints; mechanism reusable for the rest |
| 3 — ComplianceService refactor | ❌ **Not started** — 81 bypasses, conditional registration unchanged |
| 4 — Internal API standardization | ⚠️ Contract defined; no service consumes it yet |
| 5 — Tenant security hardening | ✅ **Critical R-02 closed and verified** on the two worst controllers |
| 6 — Background execution | ❌ Not started |
| 7 — Platform validation | ⚠️ Regression spot-check only |

---

## 1. Execution Context Implementation Report (Phase 1)

Four new files in `Shared.Infrastructure/Execution/`, referenced by **19 projects**:

| File | Purpose |
|---|---|
| `PlatformExecutionContext.cs` | Immutable record: `OrganizationId` (required), `HospitalId`, `JourneyId`, `FrameworkId`, `UserId`, `Mode`, `CorrelationId`, `RequestId`, `Source`, `Caller`. All six execution modes from ADR-002. Factories `ForBackgroundJob()` / `ForSystem()`. |
| `ExecutionContextHeaders.cs` | Wire contract (`X-Aovias-*`) + the `All` list used to strip client-supplied values at the edge. |
| `ExecutionContextAccessor.cs` | Resolves context: cached → propagated headers → validated token claims. `OrganizationId` returns `Guid.Empty` when unresolved so callers **fail closed**. |
| `TenantAuthority.cs` | Decides which organization a request may act on; separates tenant-scoped from cross-tenant endpoints and logs spoofing attempts. |

**Design notes.**
- Named `PlatformExecutionContext` to avoid colliding with `System.Threading.ExecutionContext`.
- `record` with `init`-only members — immutable for the request lifetime, per the wave's requirement.
- `IsValid` (a tenant exists) is deliberately distinct from `IsCrossTenant` (System mode), so the two
  can never be conflated at a call site.
- Cross-tenant roles are the nine AOVIAS staff roles, matching `ApplicationRoles.CommercialAdminRoles`.

Builds clean; no existing code changed by its introduction.

## 2. Gateway Security Report (Phases 2 & 5)

### `[ValidateTenantScope]` — one implementation, not 24

Wave 2.2A found 24 endpoints across Workforce Intelligence and Enterprise Directory accepting
`organizationId` from the query string behind a bare `[Authorize]` plus a permission check.
**Permission was verified; tenant was not.**

Rather than hand-editing 24 expression-bodied actions — which duplicates the same logic 24 times and
leaves the *next* endpoint unprotected by default — enforcement is an action filter applied at
controller level:

- Rejects with `403` + the standard failure envelope when the requested org ≠ the token's org.
- **Rewrites** the action argument to the authoritative value on success, so an action cannot act on
  a client-supplied tenant even if the comparison is later loosened.
- Logs `TENANT SPOOFING REJECTED` with both ids — attributable, not just refused.
- `CrossTenant = true` opts an endpoint into staff-console behaviour, still gated on a cross-tenant role.

Applied to `EnterpriseDirectoryController` (9 endpoints) and `WorkforceIntelligenceController` (15).

### Runtime verification [VERIFIED]

Same account, same permission, same endpoint — only `organizationId` differs:

| Endpoint | Own tenant `2759fad4…` | Other tenant `11111111…` |
|---|---|---|
| `GET /api/v1/directory/search` | **200 OK** | **403 Forbidden** |
| `GET /api/v1/directory/facets` | **200 OK** | **403 Forbidden** |

Legitimate access preserved; cross-tenant access blocked. Because permission and endpoint are held
constant, the 403 is attributable to the tenant check alone.

> **Note on Workforce Intelligence:** this account returns `403` for *both* tenants there, with an
> empty body — that is `RequirePermission` denying before any action filter runs (the account lacks
> `Intelligence.Dashboard.Read`). The filter is applied but **unverified on that controller**;
> verifying it needs an account holding that permission.

### Endpoint classification (63 total)

| Class | Count | Treatment |
|---|---|---|
| Cross-tenant by design (CommercialAdmin / CommercialOps / Licenses) | 12 | Leave as-is; already gated by `CommercialAdmin` / `AdminOnly`. Should adopt `[ValidateTenantScope(CrossTenant = true)]` for consistent logging |
| **Hardened this wave** | **25** | 24 via the filter + `deficiencies/bootstrap-from-onboarding` (Wave 2.2) |
| Remaining tenant-scoped | 26 | `OrganizationPhase4Controllers` (6), `IntegrationController` (6), `Employment`/`Assignment` (6), `Sla` (2), Subscriptions, Reporting, ProfessionalProfiles, Licensing, Hospitals, Governance |

The remaining 26 are the same one-line change per controller; they were not applied because each
needs its own runtime verification and an account with the relevant permission.

## 3. What is NOT done

| Item | Why it matters |
|---|---|
| **Conditional query-filter registration unchanged** | R-01 remains open. Internal API-key paths still register **no** filter, so ComplianceService still aggregates across tenants. This is the single highest-value remaining change and the riskiest — flipping it can zero out any path lacking context. It needs the feature flag and the two-tenant test suite before it is safe. |
| 81 of 84 `IgnoreQueryFilters()` bypasses | R-03 |
| Internal controllers do not bind the context | The headers contract exists; nothing reads it yet |
| Background jobs / SignalR / caches | R-05–R-07, still hypotheses from 2.2A |
| Two-tenant integration suite | The prerequisite for safely doing any of the above |

**No service consumes `PlatformExecutionContext` yet.** It is the foundation, registered nowhere —
deliberately, so its introduction changes no behaviour.

## 4. Cross-Repository Change Summary

| Repo | Files | Breaking? |
|---|---|---|
| `ComplaincePlatform` / Shared.Infrastructure | 4 new | No — purely additive |
| `ComplaincePlatform` / Gateway | 2 controllers, +1 attribute each | **Behavioural**: cross-tenant callers now correctly receive 403 |
| `WebApp` | None | No |
| Database | None this wave | No |

## 5. Regression [VERIFIED]

| Check | Result |
|---|---|
| `Shared.Infrastructure` build | ✅ 0 errors |
| `Gateway` build | ✅ 0 errors |
| Gateway restart + login | ✅ |
| Directory endpoints, own tenant | ✅ 200 |
| Audit → Findings | ✅ 25 rows, `Internal Audit` / `Reyansh Das` |
| Context restore after re-login | ✅ hospital + framework correct |

No regressions observed. No new Critical or High defects.

## 6. Acceptance criteria

| Criterion | Status |
|---|---|
| Enterprise Execution Context implemented | ✅ Library complete |
| Gateway owns tenant resolution | ⚠️ 25/63 endpoints |
| Client-controlled tenant context removed | ⚠️ Removed where applied; 26 remain |
| ComplianceService aligned with platform architecture | ❌ |
| Internal APIs standardized | ⚠️ Contract only |
| Background execution tenant-safe | ❌ |
| SignalR tenant-safe | ❌ |
| Security validation completed | ⚠️ Two endpoints proven |
| Runtime regression passed | ✅ |
| No cross-tenant access possible | ❌ Not platform-wide |

## 7. Recommended next steps

1. **Two-tenant integration suite first.** Everything remaining is either low-risk-and-repetitive
   (the 26 controllers) or high-risk (filter registration). The suite is what makes the second
   category safe, and it is currently the gating dependency.
2. **Then flip filter registration behind a feature flag** — one release with it off, telemetry on
   contexts that resolve empty, then on.
3. **Then the remaining 26 controllers** — mechanical, one attribute each, but each needs an account
   with the right permission to verify.
4. **Then background jobs and SignalR** — R-05 to R-07 are still unproven hypotheses; they deserve
   investigation before implementation, as in 2.2A.

Wave 2.2C (data cleanup) should still wait: while ComplianceService can aggregate across tenants,
cleaned data can be miscounted again immediately.
