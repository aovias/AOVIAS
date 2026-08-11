# Wave 2.1 — Business Integrity Report

**Date:** 2026-08-02 · **Repos:** `WebApp` + `ComplaincePlatform` (working trees, uncommitted)
**Method:** runtime + direct SQL against `localhost\SQLEXPRESS.Compliance`. Evidence over assumption.

## Verdict

Four of the five remaining defects are closed. **Investigation overturned the premise of DEF-010**
and surfaced **two new Critical findings** that were invisible until the data was queried directly.

| Item | Outcome |
|---|---|
| DEF-010 CAPA KPIs = 0 | ⚪ **Not a defect** — the values are arithmetically correct |
| DEF-007 Implementation-name leakage | ✅ Fixed (2 screens) |
| DEF-014 Report default period | ✅ Fixed — empty → 24 findings |
| RaisedBy = 0 | ✅ **Migrated** (24 rows) + CHECK constraint + domain guard |
| Technical debt | ✅ Dead guard deleted, ambiguous identifier renamed, deprecated alias removed |
| **NEW-05** Deficiency data duplicated | 🔴 **Critical — open** |
| **NEW-06** Internal endpoints have no tenant isolation | 🔴 **Critical — open** |

---

## 1. CAPA Investigation Report (Phase 1)

### DEF-010 is not a bug. The CAPA KPIs are correct.

`CapaQueryService.GetMetricsAsync` computes open CAPAs as:

```csharp
open.Count(d => d.CorrectiveActionStatus != CapaActionStatus.NotStarted
             || d.PreventiveActionStatus != CapaActionStatus.NotStarted
             || !string.IsNullOrWhiteSpace(d.CorrectiveActionSummary));
```

Direct query over all 176 deficiency rows for this hospital:

| Column | Rows with a non-default value |
|---|---|
| `CorrectiveActionStatus <> 0` (NotStarted) | **0** |
| `PreventiveActionStatus <> 0` | **0** |
| `CorrectiveActionSummary` non-empty | **0** |

**Not one CAPA has ever been started on any deficiency in this dataset.** `Open CAPAs = 0`,
`Overdue CAPAs = 0` and all four ageing buckets = 0 are the *correct* answers. The apparent
contradiction with "164 open deficiencies" is real but not a fault: a deficiency is an identified
gap; a CAPA is the remedial action raised against it. 164 gaps exist, none has been actioned.

The API, business rules, aggregation, repository, projection, DTO and caching layers were all
examined and are sound. **No code change was warranted, and none was made.** Patching the UI here
would have manufactured numbers that do not exist.

> **Recommendation (product, not engineering):** the CAPA panel should distinguish "0 because
> nothing is actioned" from "0 because nothing loaded". An empty-state such as *"No CAPAs raised
> yet — 164 open deficiencies are awaiting action"* would remove the ambiguity that produced this
> defect report.

### 🔴 NEW-05 [Critical] — Deficiency data is duplicated; every DCC count is ~2× reality

Querying the same table exposed a genuine integrity failure:

```
Rows for hospital d9f5554a-…         : 176
Distinct ObjectiveElementIds          :  40
Rows by TenantId:
  2759FAD4-…098 (= OrganizationId)    :  88   ← correct
  D9F5554A-…A06 (= HospitalId)        :  88   ← corrupt
Exact duplicate pairs (same OE + Title): 88
```

A seed ran twice — three days apart — and the second run wrote the **hospital id into both
`TenantId` and `OrganizationId`**. Every row has an exact twin.

**Impact:** the true figure is **88 deficiencies, 82 open** — not 176/164. Every KPI on the Issues &
Risks screen is doubled, and Executive Intelligence's "58 critical open deficiencies" and "82
overdue" inherit the same inflation.

**Not remediated.** Deleting 88 business records is a destructive, irreversible action and your call.
Proposed cleanup (verify in a restored backup first):

```sql
-- Corrupt batch is identifiable with certainty: TenantId equals the HospitalId.
SELECT COUNT(*) FROM ComplianceDeficiencies WHERE TenantId = HospitalId;   -- expect 88
DELETE FROM ComplianceDeficiencies WHERE TenantId = HospitalId;
ALTER TABLE ComplianceDeficiencies WITH CHECK
  ADD CONSTRAINT CK_ComplianceDeficiencies_TenantNotHospital CHECK (TenantId <> HospitalId);
```

### 🔴 NEW-06 [Critical] — Internal endpoints run with no tenant isolation

`DeficiencyCommandCenterQueryService` queried with `IgnoreQueryFilters()` while scoping only on
`HospitalId + FrameworkId` — no tenant predicate at all. **Removed** (both call sites), and the same
bypass removed from `DeficiencyMetricsService.QueryScopedAsync` so header totals and the panels
below them are scoped identically.

That fix is necessary but **not sufficient**, and this is the important part:

```csharp
// ComplianceDbContext — filters are registered CONDITIONALLY
if (_currentTenantId.HasValue && _currentTenantId.Value != Guid.Empty) {
    builder.Entity<ComplianceDeficiency>().HasQueryFilter(d => d.TenantId == _currentTenantId.Value);
    // … 8 more entities
}
```

`_currentTenantId` comes from `IUserContextService.OrganizationId`. The Deficiency Command Center is
reached through `internal/v1/deficiencies/**`, authenticated by `X-Internal-Api-Key` with **no user
JWT** — so there is no organization, **no filter is registered at all**, and the query returns every
tenant's rows. Verified at runtime: the screen still reports 176 after the fix.

**This is a tenant-isolation gap, not just a counting bug.** Any tenant-scoped entity read through an
internal endpoint is unfiltered. It is only invisible today because the second "tenant" is corrupt
seed data rather than a real customer.

**Correct fix (not attempted — it is a cross-repo contract change I would not want to half-land):**
thread the authenticated `organizationId` from the Gateway into the internal endpoints and filter on
it explicitly, rather than relying on an ambient filter that silently disables itself. Explicit
scoping is the right pattern for service-to-service calls; the ambient filter should be treated as a
second line of defence, not the only one.

Scoping by `OrganizationId` would work today — the corrupt batch has the hospital id in that column
too, so the correct 88 rows are cleanly selectable.

---

## 2. Executive Intelligence Review (Phase 2) — DEF-007 ✅

The API's `source` field is **backend provenance** (service + method name). Two screens rendered it
as user-facing copy:

| Location | Was | Now |
|---|---|---|
| Audit → Intelligence, Compliance readiness | `IComplianceScoringService.CalculateFrameworkScoreAsync` | "Weighted score across all chapters in the active framework" |
| Audit → Intelligence, Accreditation readiness | `IComplianceScoreService.GetJourneyScoreAsync` | "This hospital's progress through its current accreditation journey" |
| Analytics Hub, both readiness panels | same two strings | business descriptions |

Every remaining Executive KPI — CAPA effectiveness, CAPA completion, SLA health, critical/high-risk/
overdue deficiencies, recurring issues, pending reviews — previously had **no** description at all;
each now carries a business definition. Runtime check: no `IComplianceScor…`, `…Async` or `Service.`
token appears anywhere in the rendered page.

## 3. Reporting Validation Report (Phase 3) — DEF-014 ✅

The default was a rolling 30-day window. Audits are planned, scheduled and reported against a
**programme year** (`programYear` / `programMonth` on the schedule), so a rolling window lands
between audit cycles and reports nothing.

Measured against the live API:

| Range | Findings |
|---|---|
| Rolling 30 days (old default) | **0** |
| Calendar/programme year | **24** |
| No date filter | 24 |

New `programYear` period (1 Jan – 31 Dec, whole-day bounds) is now the default. Options relabelled
honestly: *Current programme year* / *Last 3 months* / *Last 30 days* — the old "This Month" label
was inaccurate for a rolling window. **Verified: the screen now opens on 24 findings
(0 critical / 4 major / 20 minor) instead of an empty report.**

## 4. Data Integrity Report + Migration Report (Phase 4) — RaisedBy ✅

### Investigation

`FindingRaisedBy` is explicitly numbered from **1**; 24 rows held **0**, which is not a member, and
`RaisedBy.ToString()` emitted the literal `"0"` to the UI.

Evidence gathered **before** touching data:

| Check | Result |
|---|---|
| Rows affected | 24 |
| Insert timestamps | all identical — `2026-07-13 11:59:12.8166667` (one batch) |
| `FindingNumber` convention | `FND-2026-001` vs `FND-2026-00001` for app-created rows |
| Descriptions mentioning "internal audit" | **24 / 24** — all share one template |
| `Classification` | NULL on all 24 |
| `CreatedByType` | 0 on all 24 |
| C# property default | `FindingRaisedBy.InternalAudit` |

Conclusion: `0` means *the seed never set it*, not a distinct historical meaning. The rows are
internal audit findings and self-describe as such. Migration is safe.

### Migration applied

`ComplianceService/Infrastructure/Persistence/Scripts/2026-08-02_FixFindingRaisedByZero.sql` —
idempotent, transactional, asserts the post-condition before commit.

```
Rows with RaisedBy = 0 before migration: 24
(24 rows affected)
Rows migrated: 24
Added CHECK constraint CK_Findings_RaisedBy_Range (1..8).
Migration committed.
```

Post-state: `RaisedBy` = 1 × 30, 8 × 2. **No invalid values remain.**

### Prevention (defence in depth)

1. **Database** — `CK_Findings_RaisedBy_Range CHECK (RaisedBy BETWEEN 1 AND 8)`.
2. **Domain** — `Finding.RaisedBy` setter coerces undefined values via `Enum.IsDefined`, so a caller
   that never assigns cannot persist the CLR default.

**Runtime confirmation:** Findings *BY SOURCE* now reads **"Internal Audit 25, Incident Review 2"** —
the unlabelled 24-row bucket is gone, and the grid shows `Internal Audit` instead of `0`.

## 5. Technical Debt Report (Phase 5) ✅

| Item | Action |
|---|---|
| `core/guards/framework.guard.ts` | **Deleted.** Defined, exported, zero usages; its semantics were ambiguous after the Wave 2 split and it was a trap for the next developer. |
| `HospitalFrameworkSelection.frameworkId` | **Renamed to `journeyId`.** It carried a journey id under a framework name — the exact confusion that caused DEF-003. `selectedFrameworkId` → `selectedJourneyId` throughout the dialog; both call sites updated; `FrameworkSelectorItem.id` documented as a journey id. |
| `OrganizationService.getLoggedInMemberDisplayName` | Callers migrated to `getMemberDisplayName`; the misleading alias remains only as a thin `@deprecated` shim. |

No ambiguous identifier remains in the framework/journey area.

## 6. Regression Results (Phase 7)

| Area | Result |
|---|---|
| Overview / Planning / Execution / Evidence / Findings / CAPA / Reports / Intelligence / Administration | ✅ all render |
| Findings SOURCE + OWNER | ✅ "Internal Audit" / real names |
| Reports default period | ✅ 24 findings |
| Executive Intelligence | ✅ no implementation leakage |
| Context restore after re-login | ✅ hospital + journey + framework all correct |
| Angular typecheck | ✅ clean |
| ComplianceService build | ✅ 0 errors |
| CAPA screen | ⚠️ unchanged at 176 — see NEW-05/06 |

**No new Critical or High defects introduced by these changes.** The two Criticals are
pre-existing conditions that this investigation *uncovered*.

## 7. Exit criteria

| Criterion | Status |
|---|---|
| CAPA KPIs correct | ✅ verified correct as computed — but headline deficiency counts are inflated by NEW-05 |
| Executive Intelligence free of implementation leakage | ✅ |
| Reports use meaningful default periods | ✅ |
| RaisedBy integrity resolved | ✅ migrated + constrained |
| Database constraints added | ✅ `CK_Findings_RaisedBy_Range` |
| Technical debt reduced | ✅ |
| Documentation completed | ⚠️ this report; ERD / Business Rule Catalog / Status Matrix still outstanding |
| Runtime regression passed | ✅ |
| No Critical defects | ❌ **2 open** (NEW-05, NEW-06) |
| No High defects | ✅ |
| Business model fully consistent | ❌ blocked on NEW-05 |

## 8. Recommendation

**Do not open Wave 3 yet.** Wave 2.1 closed its named scope, but the investigation it mandated
turned up a duplicated dataset and an unfiltered tenant boundary. Wave 3 is user experience —
polishing screens that currently present doubled numbers would bake the wrong figures into design
decisions.

Two decisions are yours:
1. **Approve the duplicate-deficiency cleanup** (destructive; 88 rows).
2. **Approve the cross-repo change** to thread `organizationId` into the internal API contract.

Both are small, well-understood pieces of work. With them done, the CAPA screen reports 88/82, the
tenant boundary is explicit, and the business model is genuinely consistent.
