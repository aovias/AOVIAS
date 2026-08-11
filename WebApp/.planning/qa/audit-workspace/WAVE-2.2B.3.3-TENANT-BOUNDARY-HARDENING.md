# Wave 2.2B.3.3 — ComplianceService Tenant Boundary Hardening

**Date:** 2026-08-02 · **Repo:** `ComplaincePlatform` (uncommitted)
**Suite:** `Passed! Failed: 0, Passed: 39, Skipped: 2, Total: 41` · **All baselines held**

## Headline finding — bypasses are contagious

The most important result of this wave is not the count reduction. It is this:

> **`IgnoreQueryFilters()` applies to the entire LINQ query, not to the entity it is called on.**

The Deficiency Command Center *workspace grid* proved it. After Wave 2.2B.3.1 the dashboard header
correctly reported **88**, while the grid beneath it still listed **176** — under the same card, on
the same screen. The root deficiency query was already tenant-filtered; the disagreement came from a
**join** to `OrganizationComplianceObligations.IgnoreQueryFilters()`, which silently disabled
filtering for *every* entity in that query, deficiencies included.

This invalidates the per-entity classification I built in Phase 1 and changes the risk model:

| Old model | Corrected model |
|---|---|
| A bypass endangers the entity it is called on | A bypass endangers **every entity in that query** |
| Bypasses on unfiltered lookup tables are harmless no-ops | They are **only** harmless if no filtered entity appears anywhere in the same query |
| 8 of 81 were "no-op" | Some of those 8 were actively disabling filters on their query's root |

Any future audit of the remaining 73 must be **per query**, not per entity.

---

## 1. IgnoreQueryFilters Audit (Phase 1)

81 call sites across 35 files at wave start. Classified by target DbSet against the 18 entities that
actually carry a tenant filter:

| Target DbSet | Count | Filtered? | Class |
|---|---|---|---|
| `OrganizationComplianceObligations` | 30 | ✅ Yes | **Bug / Risk** |
| `ComplianceDeficiencies` | 23 | ✅ Yes | **Bug / Risk** |
| `ComplianceJourneys` | 13 | ✅ Yes | **Bug / Risk** |
| `ObjectiveJourneys` | 1 | ✅ Yes | **Bug / Risk** |
| `ComplianceStandards` | 2 | ❌ No | Legacy — but contagious in context |
| `ComplianceChapters` | 2 | ❌ No | Legacy — but contagious in context |
| `ComplianceTasks`, `Evidences`, `ObjectiveElementEvidences`, `WizardOnboardingAssessments` | 4 | ❌ No | Legacy |

**18 filtered entities:** AuditComment, AuditObservation, AuditSession, ClinicalIndicator,
ComplianceDeficiency, ComplianceJourney, ControlledDocument, DocumentAcknowledgement,
DocumentAuthoringCheckpoint, DocumentAuthoringComment, DocumentCrossReference,
DocumentLifecycleAuditEntry, DocumentReview, Incident, LessonLearned, ObjectiveJourney,
OrganizationComplianceObligation, PatientFeedback.

## 2. Removals (Phase 3)

**81 → 73 call sites.** All in `DeficiencyCommandCenterQueryService`, removed incrementally with a
build + runtime check after each step:

| Removed | Target | Effect |
|---|---|---|
| 2 × `ComplianceChapters` joins | Unfiltered | Removed contagion source from two queries |
| 2 × `ComplianceStandards` joins | Unfiltered | Same |
| 1 × `ComplianceDeficiencies` root (workspace) | Filtered | Direct boundary fix |
| 2 × `OrganizationComplianceObligations` joins | Filtered | **The fix that made the grid agree with the header** |
| (1 further site) | — | Consolidated during the above |

**Result — the DCC is now internally consistent:**

```
dashboard total = 88
workspace total = 88     consistent = True
```

Previously 88 vs 176.

## 3. Internal Query Hardening (Phase 4)

| Surface | State |
|---|---|
| DCC dashboard aggregation | ✅ Tenant-scoped, verified 88/82 |
| DCC workspace grid | ✅ Now agrees with the dashboard |
| Control Center KPIs / audit-readiness | ✅ 200, unchanged |
| Findings / Reports / Sessions / Schedule | ✅ Unchanged |
| Analytics, Executive Intelligence | ⚠️ Not individually hardened — 6 bypasses remain in `Application/Analytics` |
| Obligation & Journey repositories | ❌ 30 + 13 bypasses remain |

## 4. Runtime Validation (Phase 5)

| Probe | Baseline | After | Result |
|---|---|---|---|
| DCC dashboard | 88 / 82 | 88 / 82 | ✅ |
| **DCC workspace** | **176** | **88** | ✅ **Corrected** |
| Findings dashboard | 27 | 27 | ✅ 87 ms |
| Auditor sessions | 16 | 16 | ✅ 98 ms |
| Audit schedule | 15 | 15 | ✅ 48 ms |
| Audit report | 24 | 24 | ✅ 102 ms |
| Control Center KPIs | 200 | 200 | ✅ |
| Integration suite | 39 / 2 / 0 | 39 / 2 / 0 | ✅ |

No regressions, no unexpected empty datasets, no performance change (48–102 ms, matching or better
than the pre-wave range).

## 5. Remaining 73 bypasses — governance

Each remaining site falls into one of three groups. **None is yet formally justified in code**, and
that is the honest state:

| Group | Count | Disposition |
|---|---|---|
| Obligation queries (`OrganizationComplianceObligations`) | ~30 | Largest cluster; obligations are tenant-owned, so most are probably removable the same way the DCC ones were. Needs per-query review. |
| Deficiency queries | ~21 | Same pattern as the DCC fixes |
| Journey queries (`ComplianceJourneys`, `ObjectiveJourneys`) | ~14 | Journey resolution sometimes runs before tenant context exists (bootstrap) — some may be genuinely required |
| Analytics / snapshot / background | ~8 | Cross-tenant aggregation may be legitimate for platform-level analytics; needs product input |

**Recommended governance mechanism (not implemented):** require every remaining call site to carry a
justification, and enforce it with an analyzer or architecture test so new bypasses cannot be added
silently. `ExecutionContextTelemetry.FilterBypass` (Wave 2.2B.3.2) already exists to inventory them
from real traffic — which, given how badly static analysis over-counted in 2.2A, is the more
trustworthy source.

## 6. Updated registers

### Risk Register

| ID | Risk | Severity | Status |
|---|---|---|---|
| R-01 | Conditional filter registration | Critical | ✅ **Closed** (2.2B.3.1) |
| R-02 | Client-supplied `organizationId` | Critical | ⚠️ 24 hardened, 15 verified safe, 24 unprobed |
| **R-03** | **Filter bypasses** | **High** | ⚠️ 81 → **73**; risk model corrected (contagion) |
| R-04 | Bootstrap tenant corruption | Critical | ✅ Closed (2.2B) |
| R-05–R-07 | Caches / SignalR / background jobs | High | ⚠️ SignalR assessed (incidentally safe); others open |
| R-08 | CommunicationService has no tenant references | Medium | ⚠️ Open — investigate before assuming safe |

### Technical Debt

| ID | Item | Status |
|---|---|---|
| TD-01 | 81 bypasses | ⚠️ **73 remain** — per-*query* review required, not per-entity |
| TD-10 | Fail-closed telemetry | ✅ Closed (2.2B.3.2) |
| TD-11 | Partial proxy propagation | ✅ Closed (2.2B.3.2) |
| TD-16 | **No bypass governance mechanism** | 🆕 Open — analyzer/architecture test |
| TD-13/14/15 | Background jobs, SignalR harness, Communication | Open |

## 7. Wave 2.2B Completion Assessment

| Sub-wave | Outcome |
|---|---|
| 2.2B — Foundation + Gateway hardening | ✅ Complete |
| 2.2B.1 — Integration suite | ✅ Complete |
| 2.2B.2 — Coverage expansion | ✅ Complete (41 tests) |
| 2.2B.3.1 — ComplianceService context adoption | ✅ Complete — CAPA corrected 176→88 |
| 2.2B.3.2 — Propagation + observability | ✅ Complete |
| 2.2B.3.3 — Boundary hardening | ⚠️ **Partial** — 8 removed, 73 remain |

**Wave 2.2B is substantially complete but not fully done.** The architecture is in place and
enforced; the residual work is the 73-site cleanup, which is mechanical but must be per-query and
individually verified.

## 8. Recommendation on Wave 2.2C

The brief says do not begin 2.2C until this wave completes. Two observations:

1. **2.2C is no longer needed for correctness.** The 88 duplicate rows are now *filtered out*
   everywhere the boundary is enforced — dashboard and grid both read 88. Deleting them is hygiene,
   not a fix.
2. **The 73 remaining bypasses are a better use of the next wave.** Each is a path where the
   boundary does not hold, and — given the contagion finding — each can silently unfilter an entire
   query, not just one entity.

If the goal is the strongest platform, I would do the bypass cleanup before the data cleanup. That
said, the sequencing call is yours; both are now safe to do in either order, which was not true
before this wave.
