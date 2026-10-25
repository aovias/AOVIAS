# Stage 5B.1 — Enterprise Configuration API

**Date:** 2026-08-02 · **Repos:** `ComplaincePlatform`, `WebApp` (uncommitted)
**Status:** API operational and verified per-tenant. Consumer migration partial.

## Headline: per-tenant configuration resolves end to end

Same category, two organizations, one API call each:

```
TENANT A (2759fad4…)  version=7A334E0AA327A92A
   InternalAudit = 'Internal Quality Audit'   order=5    override=True
   DepartmentAudit = 'Department'             order=20   override=False

TENANT B (b7632d36…)  version=4134B70C6D38AE48
   InternalAudit = 'Internal'                 order=10   override=False
```

Tenant A's override wins; tenant B sees the platform default; both receive the complete 9-value
catalog. Distinct version hashes mean a client cache invalidates per tenant, not globally.

---

## 1. Configuration Domain Matrix (Phase 1)

| Domain | Category | Storage | Rationale |
|---|---|---|---|
| **Audit Types** | **A** | Lookup | ✅ Implemented |
| Department Types, Unit Types, Membership Types | **A** | Lookup | Already present |
| Severity Categories, Risk Categories, Priority Levels | **A** | Lookup | Flat, coded, orderable |
| Document Categories, Evidence Categories, Notification Types | **A** | Lookup | Same shape |
| **Annual Programs** | **B** | Dedicated | Owns schedule, coverage, participants — an aggregate with behaviour |
| **Workflow Rules** | **B** | Dedicated | Conditions and transitions; not a flat list |
| **Approval Chains** | **B** | Dedicated | Ordered steps, delegation, quorum |
| **Escalation Policies** | **B** | Dedicated | Time-based triggers with side effects |
| **Digital Signatures** | **B** | Dedicated | Cryptographic material, audit trail |
| **Feature Flags** | **B** | Dedicated | Targeting rules, rollout percentages |
| **Organization Calendar / Financial Year** | **B** | Dedicated | Date arithmetic, recurrence |

**8 of 14 domains are Category A** and need no new schema — the point of the metadata store.

## 2. Configuration API (Phase 2)

### Schema extension — one aggregate, not fourteen tables

`2026-08-02_ExtendConfigurationMetadata.sql`, idempotent and additive.

The shipped tables were `Categories(Id, Code, Name)` and `Values(Id, CategoryId, Code, Name, SortOrder)`.
**Neither carried an `OrganizationId`** — they were platform-global lookups and could not express
"this organization configures its own audit types", which is the core Stage 5B requirement.

Added: `OrganizationId` (null = platform default), `IsActive`, `EffectiveFrom/To`, `ParentValueId`,
`Metadata` (JSON), `RowVersion`; and on categories `Description`, `IsTenantConfigurable`,
`IsSystemManaged`.

> **A defect caught only by verification.** A pre-existing unique index
> `IX_OrganizationLookupValues_CategoryId_Code` covered `(CategoryId, Code)` with no tenant column,
> which made a tenant override *impossible* — the override necessarily shares its code with the
> default it replaces. My first migration added filtered indexes but left the old one in place, so
> the design would have been silently unusable. Found by trying to insert an override and getting a
> duplicate-key error. The migration now drops it and replaces it with two filtered indexes that
> enforce uniqueness within each scope separately.

### Endpoints

| Endpoint | Purpose |
|---|---|
| `GET /internal/v1/configuration/categories` | Category codes, for administration screens |
| `GET /internal/v1/configuration/categories/{code}?organizationId=` | Resolved category with typed values |

`ConfigurationCategoryDto` / `ConfigurationValueDto` — strongly typed, no stringly-typed payloads.
Domain-specific attributes ride in `Metadata` so a new domain needs no DTO change.

### Resolution rule

Platform defaults overlaid with the organization's rows, matched on `Code`; tenant wins, default
shows through. **An organization that has configured nothing still receives a complete catalog** —
no seeding step and no empty pickers, which is what makes adoption incremental.

### Versioning

`Version` is a hash of the **resolved** values, not of row versions. A tenant override producing an
identical result therefore does not invalidate a client cache unnecessarily.

## 3. Providers (Phase 3)

**One service serves every Category A domain.** Audit types, severities and risk categories differ
only in their data; per-domain services would duplicate the resolution and the copies would drift —
the exact failure this programme has corrected repeatedly. Adding a domain is inserting a category
row.

## 4. Cache (Phase 4) — partial

| Layer | State |
|---|---|
| Client-side (`PlatformConfigurationService`) | ✅ Signal-backed, computed projections, single write point |
| Version-based invalidation | ✅ Contract in place (`Version` returned per category) |
| Server-side memory cache | ❌ Not implemented |
| Tenant-isolated cache keys | ⚠️ Design only — keys must include `OrganizationId` |

The client holds configuration in signals rather than refetching, so repeated reads cost nothing.
A server-side cache is not yet needed at this data volume but is specified.

## 5. Consumer migration (Phase 5) — 1 of 7

| Consumer | State |
|---|---|
| **Planning** | ✅ Both dropdowns configuration-driven, verified |
| Execution / Sessions | ❌ Still uses `AUDIT_TYPE_LABELS` |
| Findings, Evidence, CAPA, Reports, Executive Intelligence | ❌ Constants remain |

The Angular `PlatformConfigurationService` still reads its **seed catalog**, not the new API. The
seam exists (`applyAuditTypes()`), and wiring it is a small, isolated change — but it is not done,
and I will not describe the loop as closed when it isn't.

## 6. Runtime validation (Phase 6)

| Check | Result |
|---|---|
| `GET /configuration/categories` | ✅ 200 — AuditType, DepartmentType, MembershipType, UnitType |
| `GET /configuration/categories/AuditType` | ✅ 200 — 9 values, ordered, versioned |
| Tenant override resolution | ✅ Verified across two organizations |
| Platform default fallback | ✅ Tenant B unaffected by A's override |
| Planning dropdown | ✅ 6 options, unchanged behaviour |
| OrganizationService build | ✅ 0 errors |
| Angular typecheck | ✅ Clean |
| Platform Integrity suite | ✅ 43 / 2 / 0 |

Test override removed after verification; `RemainingOverrides = 0`.

## 7. Acceptance criteria

| Criterion | Status |
|---|---|
| Configuration API operational | ✅ |
| Metadata domains powered by OrganizationLookup | ✅ 8 of 14 classified Category A |
| Rich domains identified separately | ✅ 6 Category B |
| Cache operational | ⚠️ Client yes, server no |
| Audit consumers migrated | ❌ **1 of 7** |
| Runtime validation passes | ✅ |
| No duplicated configuration logic | ⚠️ Audit types consolidated; severity/status/SLA remain |

## 8. Next

1. **Wire the Angular service to the API** — smallest remaining step with the largest effect; the
   seam is built and the API is live.
2. **Migrate the remaining six consumers**, starting with Execution/Sessions (same audit-type
   catalog, so no new domain needed).
3. **Add Severity and RiskCategory** as Category A — a category row plus values, no code.
4. **Server-side cache with tenant-scoped keys** before the first high-traffic consumer.

### Carried debt (unchanged)

TD-13 background job contexts · TD-19 24 unprobed Gateway endpoints · TD-14 SignalR namespacing ·
TD-15 CommunicationService · TD-04 88 filtered duplicates · TD-01 66 governed bypasses.

**Both repositories remain uncommitted** — now spanning fifteen integrity waves plus two Stage 5
increments. This is well past the point where it should be versioned.
