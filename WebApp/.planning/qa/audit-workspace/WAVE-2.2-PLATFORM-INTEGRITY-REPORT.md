# Wave 2.2 — Platform Data Integrity & Tenant Isolation

**Date:** 2026-08-02 · **Repos:** `WebApp` + `ComplaincePlatform` (uncommitted)
**Method:** code trace → direct SQL → runtime verification.

## Summary

The seed-corruption root cause is **found and fixed at both ends**. The tenant-isolation weakness is
**larger than Wave 2.1 suggested** and is scoped here with an ADR. The duplicate-row cleanup was
built, guarded, and **deliberately not executed** — the safety check refused, correctly.

| Phase | Status |
|---|---|
| 1 — Seed data recovery | ✅ Root cause fixed · ⚠️ cleanup blocked by its own safety gate (by design) |
| 2 — Tenant isolation architecture | ⚠️ Entry point fixed + ADR written; platform-wide implementation **not** done |
| 3 — Platform tenant audit | ✅ Completed — 84 bypasses catalogued, design flaw identified |
| 4 — Enterprise data integrity audit | ❌ Not done (deficiencies + findings only) |
| 5 — Platform regression | ⚠️ Partial — builds + typecheck clean, services restored |

---

## 1. Seed Data Recovery Report (Phase 1)

### Root cause — a client-supplied tenant id on a write endpoint

The corruption was not a seed script. It was **the running application**, and it can recur on every
page visit.

```ts
// journey-maturity-dashboard.component.ts (before)
const organizationId = this.complianceContext.getOrganizationId() ?? hospitalId!;
return this.deficiencyService.bootstrapFromOnboarding(organizationId, hospitalId!, frameworkId!)
```

Whenever organization context had not hydrated — a reload, a deep link, the exact conditions
DEF-001 created before Wave 1 — the dashboard sent the **hospital id as the organization id**.

```csharp
// Gateway/Controllers/DeficienciesController.cs (before)
[HttpPost("bootstrap-from-onboarding")]
public async Task<IActionResult> BootstrapFromOnboarding([FromQuery] Guid organizationId, …)
```

The Gateway accepted it verbatim. `bootstrap-from-onboarding` then wrote a full deficiency set
stamped `OrganizationId = TenantId = HospitalId` — exactly the 88-row signature found in Wave 2.1.

**Answers to the questions posed:**

| Question | Answer |
|---|---|
| How did duplication occur? | Bootstrap ran with a hospital id in the organization slot |
| Which process produced it? | `POST /deficiencies/bootstrap-from-onboarding`, called by Journey Maturity dashboard |
| Is duplicate execution possible? | **Yes — it fires on every visit to that dashboard** |
| Are production deployments vulnerable? | **Yes.** Any tenant whose org context fails to hydrate corrupts its own data; and any client could previously write into *any* tenant |
| Were other modules seeded incorrectly? | No other client-supplied `organizationId` write path found (see §3) |

### Fixes applied

| Layer | Change |
|---|---|
| **WebApp** | `organizationId$` is now a **precondition** in the `combineLatest` filter. The `?? hospitalId` fallback is gone — a bootstrap under the wrong tenant is far worse than one that runs a moment later. |
| **Gateway** | `BootstrapFromOnboarding` derives the organization from the caller's **token claim**. The query parameter is retained for contract compatibility but is no longer authoritative; a mismatch is logged, and a missing claim returns `401` instead of proceeding. |

Both compile clean; Gateway rebuilt and restarted.

### Cleanup script — built, guarded, and correctly refused to run

`ComplianceService/Infrastructure/Persistence/Scripts/2026-08-02_RemoveDuplicateDeficiencies.sql`

Idempotent, transactional, quarantines before deleting, asserts its post-condition, and adds
`CK_ComplianceDeficiencies_TenantNotHospital`. Identification rule is exact: *a deficiency whose
`TenantId` equals its `HospitalId` is invalid by definition.*

**It aborted at the pre-flight gate, and I did not override it:**

```
Total deficiencies            : 176
Rows where TenantId=HospitalId:  88
Corrupt rows carrying CAPA progress or closure: 6
ABORT: corrupt-tenant rows carry CAPA progress. Review manually before deleting.
```

Investigating those 6:

| Batch | Rows | With CAPA progress | Closed |
|---|---|---|---|
| Corrupt (`TenantId = HospitalId`) | 88 | **0** | 6 |
| Correct (`TenantId = OrganizationId`) | 88 | **0** | 6 |

The two batches are symmetric, and every divergent row inspected has an identical twin
(same status `6`, same `ClosedDate`). The mismatch is an artefact of my matching key: `Title` is
**not unique within a batch** — "Evidence Missing" appears three times — so a join on
`ObjectiveElementId + Title` cannot pair rows 1:1.

**I stopped here deliberately.** The batches almost certainly *are* duplicates, but "almost
certainly" is not the standard for an irreversible delete of closed business records, and your own
rule is *"Only remove duplicated records after verification."* I could not produce a row-for-row
proof with the available keys.

**To finish this safely, one of:**
1. Confirm the seed is reproducible and the corrupt batch carries no unique history → run the script
   with the `ClosedDate` clause removed from the pre-flight guard.
2. Add a stable natural key (a seed batch id or deterministic `DeficiencyNumber`) so 1:1 pairing is
   provable, then re-run unchanged.
3. Restore a pre-2026-06-23 backup into a scratch database and diff.

Until then the CAPA screen keeps reporting 176/164 rather than the true 88/82.

---

## 2. Tenant Isolation — Internal API Audit (Phase 3)

### The design flaw

```csharp
// ComplianceDbContext.OnModelCreating
if (_currentTenantId.HasValue && _currentTenantId.Value != Guid.Empty) {
    builder.Entity<ComplianceDeficiency>().HasQueryFilter(d => d.TenantId == _currentTenantId.Value);
    // … 8 further tenant-scoped entities
}
```

`_currentTenantId` comes from `IUserContextService.OrganizationId`. **Internal routes authenticate
with `X-Internal-Api-Key` and carry no user JWT**, so there is no organization, the `if` is false,
and **no filter is registered at all**. Isolation silently disables itself exactly where it is least
observable — service-to-service calls.

This is worse than a missing filter, because the code *reads* as though isolation is enforced.

### Bypass inventory — `IgnoreQueryFilters()`

**84 occurrences, all in ComplianceService.** Top concentrations:

| File | Count |
|---|---|
| `DeficiencyCommandCenterQueryService.cs` | 12 → **10 after this wave** |
| `ObligationRepository.cs` / `ObligationQueryService.cs` / `InternalJourneyObligationsController.cs` | 5 each |
| `DeficiencyRepository.cs` | 5 |
| `DeficiencyQueryService.cs` | 4 |
| `StageUnlockingService.cs`, `PatchDeficiencyCapaCommandHandlers.cs`, `InternalObligationsController.cs`, `AnalyticsQueryService.cs` | 3 each |
| 20+ further files | 1–2 each |

Every one is on a tenant-scoped entity. Combined with the conditional registration above, the
practical position today is: **on internal paths, tenant isolation is not enforced.**

### Remediated this wave

| Location | Change |
|---|---|
| `DeficiencyCommandCenterQueryService` (×2 queries) | `IgnoreQueryFilters()` removed |
| `DeficiencyMetricsService.QueryScopedAsync` | `IgnoreQueryFilters()` removed |
| `Gateway DeficienciesController.BootstrapFromOnboarding` | Organization from token, not query string |

Necessary but **not sufficient** — with no filter registered on internal paths, removing the bypass
changes nothing there. Verified at runtime: the CAPA screen still reports 176. The real fix is the
ADR below.

### Security assessment

| Finding | Severity | Status |
|---|---|---|
| Client-supplied `organizationId` on a write endpoint | **Critical** | ✅ Fixed |
| Tenant filters silently disabled on internal routes | **Critical** | ⚠️ Scoped, not implemented |
| 84 explicit filter bypasses on tenant-scoped entities | **High** | ⚠️ Catalogued |
| Cross-tenant aggregation demonstrable today | **Critical** | ⚠️ Blocked on cleanup + ADR |

Today's exposure is limited because the only "second tenant" is corrupt seed data. **That is luck,
not architecture** — the same code paths would aggregate two real customers.

---

## 3. ADR-001 — Internal Execution Context

**Status:** Proposed · **Date:** 2026-08-02 · **Supersedes:** ambient `_currentTenantId` filters for
service-to-service calls

### Context
Tenant scope is currently inferred from the authenticated user, via an EF global query filter that
is registered only when a user context exists. Internal API-key routes and background jobs have no
user, so the filter is absent and every tenant-scoped query is unfiltered. 84 call sites bypass the
filter explicitly in addition.

### Decision
Every internal request carries an **explicit execution context**, propagated as a first-class
parameter rather than inferred from ambient state:

```csharp
public sealed record InternalExecutionContext(
    Guid OrganizationId,   // required — the tenant boundary
    Guid? HospitalId,
    Guid? JourneyId,
    Guid? FrameworkId,
    string CorrelationId,
    string RequestId);
```

1. **Transport** — `X-Aovias-Organization-Id` (+ optional hospital/journey/framework) on every
   internal call, set by the Gateway from the caller's **validated token claims**, never from client
   input. Background jobs construct it explicitly per tenant.
2. **Binding** — a model binder / middleware materialises it and **rejects the request** when
   `OrganizationId` is absent. No defaulting, no inference, no `?? somethingElse`.
3. **Registration** — query filters are registered **unconditionally**. When no tenant is resolvable
   the filter compares against `Guid.Empty` and returns nothing. *Failing closed is the point:* a
   query that returns zero rows is a visible bug; one that returns every tenant's rows is a breach.
4. **Bypass discipline** — `IgnoreQueryFilters()` is permitted only in migrations, admin
   cross-tenant tooling, and explicitly-scoped repository methods that take an `OrganizationId`
   argument. Enforced by an analyzer or an architecture test.

### Consequences
- *Positive:* tenant scope becomes explicit and testable; background jobs and internal APIs get the
  same guarantees as user requests; the failure mode inverts from silent over-fetch to loud
  under-fetch.
- *Negative:* touches ~84 call sites plus internal controller signatures — a deliberate, staged
  refactor, not a drop-in change.
- *Migration:* additive headers first (both paths work), then flip registration to unconditional,
  then remove bypasses file-by-file with the architecture test as the ratchet.

### Rejected alternatives
- *Keep ambient context, add a null check* — same latent failure, now with a thrown exception.
- *Scope by `HospitalId` only* — hospital ids are not a tenant boundary; this is precisely the
  confusion that produced the corruption.

---

## 4. What was NOT done

Stated plainly, because the acceptance criteria imply otherwise:

| Item | Status |
|---|---|
| Duplicate rows removed | ❌ Blocked by the safety gate — needs your decision |
| Internal Execution Context implemented | ❌ Designed only (ADR above) |
| 84 bypasses remediated | ❌ 3 fixed, 81 remain |
| Query-filter registration made unconditional | ❌ Not changed — unsafe without the ADR in place |
| Phase 4 entity integrity audit | ❌ Only deficiencies + findings examined; Organizations, Hospitals, Journeys, Sessions, Evidence, CAPAs, Risks, Templates, Notifications, Approvals, Tasks, Analytics **not** audited |
| Phase 5 full platform regression | ❌ Builds/typecheck clean and services restored; no functional sweep run |
| Seed process made idempotent | ❌ Root cause fixed so it cannot corrupt; true idempotency (upsert on a natural key) not implemented |

Wave 2.2 as specified is a multi-week platform refactor. What is delivered is the investigation, the
root-cause fixes at the entry point, the audit, and the architecture decision — the parts that had
to come first and that make the rest safe to do.

## 5. Acceptance criteria

| Criterion | Status |
|---|---|
| Duplicate seed data eliminated | ❌ |
| Seed process idempotent | ⚠️ Cannot corrupt; not idempotent |
| Cleanup scripts verified | ✅ Written, gated, dry-run proven |
| Internal execution context implemented | ❌ ADR only |
| Tenant isolation enforced platform-wide | ❌ |
| Internal APIs no longer depend on JWT | ❌ |
| Every endpoint audited | ⚠️ Bypasses catalogued; per-endpoint classification not completed |
| No cross-tenant leakage | ❌ |
| Data integrity verified | ⚠️ Deficiencies + findings only |
| Constraints prevent recurrence | ⚠️ `CK_Findings_RaisedBy_Range` live; tenant constraint ships with the cleanup |
| Runtime regression passed | ⚠️ Partial |
| No Critical defects | ❌ 2 open |

## 6. Recommendation

Wave 3 remains blocked, and the reason has sharpened: this is no longer an Audit-module data problem
but a **platform tenancy boundary** problem.

Decisions needed from you:
1. **How to prove the duplicate batch is safe to delete** — options 1–3 in §1.
2. **Approve ADR-001** and schedule the staged refactor. Until filter registration is unconditional,
   any new internal endpoint inherits the same gap.

The two code fixes shipped here stop the bleeding: no client can write into another tenant through
bootstrap, and the web app can no longer corrupt its own. The historical rows and the wider
isolation gap remain.
