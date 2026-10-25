# Wave 2 — Business Consistency Report

**Date:** 2026-08-01 · **Repos:** `WebApp` + `ComplaincePlatform` (uncommitted working trees)
**Scope:** identifier audit, enum/status normalization, KPI single-source-of-truth, data integrity.

## Headline

Wave 2 caught **a Critical regression that Wave 1 introduced** — exactly what Phase 1 exists for —
and eliminated three classes of duplicated business logic. Four defects fixed and runtime-verified.

---

## 1. Identifier Audit Report (Phase 1)

Every `getFrameworkId()` / `getJourneyId()` consumer was classified. Method: read the call site, then
**test the endpoint against both ids** and compare responses — runtime over assumption.

### 🔴 Critical regression found and fixed

**`features/journey/services/journey-context-resolver.service.ts`** — `resolveJourneyId()` read
`getFrameworkId()` and matched it against `j.journeyId`. Before Wave 1 that worked by accident
(the framework slot held a journey id). After the split it stopped matching, fell through its
`isUuid()` escape hatch, and **returned the framework id as the journey id** — so the entire Journey
module would have called `/journeys/{frameworkId}/…`.

Fixed to read `getJourneyId()`. Verified at runtime — requests now carry the journey id:

```
GET /api/v1/journeys/e1460468-…1ea/session     ← journey id, correct
GET /api/v1/journeys/e1460468-…1ea/dashboard
GET /api/v1/journeys/e1460468-…1ea/chapters
```
Journey Execution renders full data (Maturity Stage Progress, Core 6% readiness, OE scores).

> This is why Phase 1 was worth doing as its own phase. The type system cannot catch it — both are
> `string | null` — and the fallback made it fail silently rather than loudly.

### Consumer classification

| Consumer group | Correct id | Verified how |
|---|---|---|
| `journey-context-resolver` → `/journeys/{id}/**` | **Journey** | Path segment is the journey; **fixed** |
| `audit-management-api.service` (schedule/calendar/session) | **Framework** | Wave 1: journey→0 rows, framework→3 rows |
| `execution-screen`, `journey-maturity-dashboard`, `oe-remediation-context` → deficiency/task APIs | **Framework** | Param is literally `frameworkId`; both ids return identical results (endpoint resolves from hospital) |
| `evidence-api`, `evidence-binder-api` → `/evidence/**` | **Framework** | Both ids return byte-identical summaries — param ignored server-side |
| `tasks`, `control-center`, `reports`, `dashboard`, `copilot`, `department-head`, `qm-home`, `consultant` | **Framework** | Param named `frameworkId`; endpoints filter by hospital, unaffected |
| Hospital/framework selector dialogs (sidebar, profile menu) | **Journey** for preselection, resolves **Framework** on confirm | Wave 1 |

**No ambiguity remains.** One genuine mismatch existed; it is fixed.

### Debt recorded, not changed

- **`core/guards/framework.guard.ts` is dead code** — defined, exported, **zero usages**. Its
  semantics are now ambiguous (does it gate on framework or journey?). Recommend deleting, or
  rewriting against an explicit id, before anyone wires it to a route.
- **`HospitalFrameworkSelection.frameworkId` carries a journey id.** The field name is now actively
  misleading. Rename to `journeyId` — mechanical, but it touches the dialog's public contract.

---

## 2. Enum Mapping Report (Phase 4)

**Root cause of DEF-004 / DEF-011 / DEF-013:** the platform emits one enum in several shapes —
`InternalAudit`, `internalAudit`, and the bare ordinal `"0"` — while each screen did its own exact
lookup or none at all. One enum, three different wrong renderings.

### New single source of truth

**`shared/utils/enum-label.util.ts`**

- `resolveEnumLabel(value, labels, ordinals?, fallback?)` — matches case- and separator-insensitively,
  supports explicitly-numbered enums via an ordinal map, humanises unknown members
  (`draftReport` → `Draft Report`) so a new server-side value is visible rather than blank, and falls
  back for out-of-range numbers instead of printing a bare digit. Index built once per label map.
- `enumEquals(value, expected)` / `enumIn(value, expected[])` — replaces raw `===` comparisons.

### Applied

| Screen | Before | After |
|---|---|---|
| Execution → Active Audits | Type + Status **blank** | **Internal** / **In Progress**, **Closed** |
| Reports → Session Reports | raw `internalAudit`, `inProgress` | **Internal** / **In Progress** — now identical to Execution |
| Findings grid — SOURCE | bare `0` | **—** (data is out of enum range; see §4) |
| Findings grid — OWNER | raw GUID `96de7cba-…` | **Reyansh Das** |
| Findings grid — STATUS | ad-hoc `if (v === 'InProgress')` | shared `FINDING_STATUS_LABELS` |
| Surveillance Closed KPI | permanently **0** (`=== 'Closed'`) | `enumIn` — correct |
| Overview "delayed" count | **10** (every *completed* audit miscounted) | **0** — correct |

New canonical maps: `FINDING_STATUS_LABELS`, `FINDING_SOURCE_LABELS`, `FINDING_SOURCE_ORDINALS`
(mirrors `FindingRaisedBy.cs`, documented as needing to stay in sync).

**No per-component enum helpers remain in the audit module.**

---

## 3. KPI Validation Report (Phase 3)

### DEF-012 fixed — "Open findings" now has one definition

The Overview derived it from a **fallback chain across unrelated payloads**:

```ts
kpis?.openFindings ?? report?.totalFindings ?? auditor?.findingsRaised ?? 0
```

When the KPI feed omitted the field it silently substituted `report.totalFindings` — a **total** where
an **open** count belongs. Hence Overview `15` vs Findings Workbench `11` for the same measure.

Now both read `FindingsWorkspaceService.getDashboard()`, the authoritative findings service:

| KPI | Overview (before → after) | Findings Workbench | Agree? |
|---|---|---|---|
| Open findings | 15 → **11** | 11 | ✅ |
| Critical findings | (3-way fallback incl. `auditor.criticalRisks` — *risks*, not findings) → **0** | 0 | ✅ |
| Delayed audits | 10 → **0** | — | ✅ correct |

### Verified-consistent

| KPI | Sources | Result |
|---|---|---|
| Upcoming scheduled | Overview vs schedule calendar | ✅ 5 = 5 (moved 4→5 after the Wave 1 test audit) |
| Corrective actions pending | Overview vs CAPA | ✅ 82 = 82 |
| Findings by chapter | Overview vs report analytics | ✅ PSQ 16 / ROM 8 |
| Active sessions | Overview vs Execution | ✅ 4 = 4 |

### Still open (not addressed this wave)

- **DEF-010** — CAPA screen: `176 deficiencies / 164 open` but every CAPA-specific counter is `0`
  (`OPEN CAPAS`, `OVERDUE CAPAS`, all ageing buckets), and SLA buckets (`0/0/0/82`) don't sum to open.
  Needs backend investigation — the CAPA entity appears unwired from deficiencies.
- **DEF-014** — Audit Reports defaults to "This Month", which is empty for this dataset.
- **DEF-007** — Executive Intelligence still shows `IComplianceScoringService.CalculateFrameworkScoreAsync`.

---

## 4. Data Integrity Report (Phase 5)

### Finding: 24 of 27 findings hold an out-of-range `RaisedBy`

`Shared.Infrastructure/Domain/Enums/FindingRaisedBy.cs` is **1-based** (`InternalAudit = 1` … `IncidentReview = 8`).
`FindingsWorkspaceQueryService.cs:490` serialises with `RaisedBy.ToString()`, so a row holding `0` —
not a member — emits the literal `"0"`, which reached the user as a bare `0` in the SOURCE column and
as an unlabelled 24-row bucket in the *BY SOURCE* breakdown.

**This is invalid data, not just a display bug.** The UI now renders `—`, but the underlying rows
remain out of range.

**Recommended remediation (needs your decision — it is a data migration):**
```sql
-- 24 rows; C# default for the property is FindingRaisedBy.InternalAudit
UPDATE Findings SET RaisedBy = 1 WHERE RaisedBy = 0;
```
Plus a CHECK constraint or EF value-converter guard so `0` cannot be written again. **I have not run
this** — mutating 24 business records is your call, not mine.

### Verified clean

| Check | Result |
|---|---|
| Schedule rows with journeyId in `frameworkId` | **0 / 15** ✅ |
| Newly created schedule row's `frameworkId` | correct ✅ |
| Journey→framework mapping (org-detail) | consistent ✅ |
| Orphaned findings (no chapter/standard/OE) | none observed ✅ |

---

## 5. Regression Results (Phase 7)

| Area | Result |
|---|---|
| All 9 audit tabs render | ✅ |
| Journey module (post-resolver fix) | ✅ full data, correct journey id |
| Findings grid — 25 rows, labels resolved | ✅ |
| Execution sessions — 16 rows, labels resolved | ✅ |
| Reports session table | ✅ |
| Overview KPIs | ✅ and now internally consistent |
| CAPA | ✅ 176 deficiencies |
| Surveillance zeros | ✅ genuine — no surveillance-typed data exists |
| Angular typecheck | ✅ clean (4 passes) |
| Console | ✅ no new errors |

**No new Critical or High defects introduced.**

---

## 6. Acceptance criteria

| Criterion | Status |
|---|---|
| Every FrameworkId/JourneyId consumer validated | ✅ |
| No incorrect identifier usage remains | ✅ (1 found, fixed) |
| Business rules centralized | ⚠️ Partial — enum/label + finding KPIs done; CAPA & scoring rules not |
| KPI calculations consistent | ⚠️ Partial — findings KPIs unified; DEF-010/014 open |
| Enum mappings standardized | ✅ |
| No duplicate business logic | ✅ for enums/labels/owner resolution |
| Runtime regression passed | ✅ |
| No new Critical defects | ✅ |
| No new High defects | ✅ |
| Documentation updated | ⚠️ This report + identifier reference; ERD and full Business Rule Catalog **not** produced |

**Wave 2 is substantially but not fully complete.** Phases 1, 4, 7 are done; Phases 3 and 5 are
partially done with the remainder specified above; Phase 8's ERD/rule catalog is outstanding.

## 7. Remaining work before Wave 3

1. **DEF-010** — CAPA counters all zero (needs backend investigation).
2. **DEF-014** — Reports default period.
3. **DEF-007** — Executive Intelligence label leak.
4. **Data migration** for `RaisedBy = 0` (decision required).
5. Delete or rewrite dead `framework.guard.ts`; rename `HospitalFrameworkSelection.frameworkId`.
6. Phase 8 deliverables: ERD, KPI Definitions, Status Definitions, Business Rule Catalog.
7. Wave 1 carry-overs: NEW-01/02 (read-only license UX), NEW-03 (`/hubs` allowlist), NEW-04 (cold start).
