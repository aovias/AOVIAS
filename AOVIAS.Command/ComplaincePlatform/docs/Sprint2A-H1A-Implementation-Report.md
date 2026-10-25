# PRAMAN Sprint 2A — H1A Implementation Summary

**Date:** 2026-06-23  
**Scope:** Journey Execution Workspace Consolidation  
**Compliance Maturity:** Not modified

---

## 1. Implementation Summary

| # | Requirement | Status |
|---|-------------|--------|
| 1 | Default landing = Execution Workspace | ✓ `/app/journey` → `/app/journey/execution` |
| 2 | Slim Journey Maturity Dashboard (Execution Launchpad) | ✓ Removed stage grid, obligation bands, governance analytics |
| 3 | Context panel wiring | ✓ Evidence, Tasks, Deficiencies, History, Timeline |
| 4 | OE execution signals on rows | ✓ Def/task counts, evidence, verification, obligation, readiness |
| 5 | Remove client accreditation calculation | ✓ `store.accreditation` replaced with `accreditationSnapshot` (server DTO) |
| 6 | Readiness label consistency | ✓ Compliance Readiness, Stage Readiness, Readiness |

---

## 2. Modified Screens

| Screen | Route | Changes |
|--------|-------|---------|
| **Execution Workspace** | `/app/journey/execution` | Primary landing; stage context strip; execution summary KPIs; context panel actions wired |
| **Execution Launchpad** | `/app/journey/maturity` | Renamed/slimmed; stage strip + blocking deficiencies only |
| **Operational View** | `/app/journey/operational` | Server-only compliance readiness (no client NABH calc fallback) |
| **Journey shell nav** | All journey routes | Execution Workspace / Execution Launchpad / Operational View tabs |
| **Issues & Risks** | `/app/audit/corrective-actions` | Accepts `?search=` from Journey deep links |
| **OE standard rows** | Execution workspace | Execution signal chips per OE |

**Unchanged:** Compliance Maturity, Standards & Chapters (`/app/journey/execution`), menu structure.

---

## 3. Modified APIs

**No backend API changes.** All work uses existing endpoints:

| API | Usage |
|-----|-------|
| `GET /journeys/{id}/dashboard` | `maturityStages`, `accreditationReadinessPercent`, `accreditation` |
| `GET /deficiencies` | OE deficiency counts (execution screen) |
| `GET /deficiencies/summary` | Launchpad blocking summary |
| `GET /tasks` (paged) | OE open task counts |
| OE `history` on workspace tree | History dialog |

**New frontend utilities only:**

- `journey-execution-navigation.util.ts` — deep link builders
- `oe-history-dialog.util.ts` — history dialog from existing OE projection

---

## 4. Validation Results

| Check | Result |
|-------|--------|
| `npm run build` | ✓ Success |
| Stage state from `StageUnlockingService` | ✓ Unchanged (consumption only) |
| No client unlock thresholds | ✓ No new client logic |
| No Compliance Maturity changes | ✓ No CM files modified |
| Execution default landing | ✓ `routes.ts` redirect |
| Context panel actions | ✓ Navigate to Evidence, Tasks, I&R, binder, history dialog |

---

## 5. Screenshots Before/After

Screenshots were not captured in this environment. Recommended manual capture:

1. `/app/journey` — should land on Execution Workspace with summary strip
2. `/app/journey/maturity` — compact launchpad (no 4× stage grid / obligation bands)
3. Execution OE row — execution signal chips visible when expanded
4. Context panel — View Tasks / View Deficiencies / Upload Evidence functional

---

## 6. Remaining H1B Dependencies (Sprint 3)

| Item | Owner | Notes |
|------|-------|-------|
| Remove duplicate stage strip from Operational View | H1B | Optional trim after CM owns full timeline |
| Operational KPI/trend cards | H1B / Dashboard | Program analytics relocation |
| Predicted gaps / CMI / forecast panels | H1B | Already CM-only; Journey launchpad links out |
| Distinct trend series on dashboard API | Backend | `OeScoreTrend` / `ChapterImprovementTrend` still same source |
| OE-level deficiency filter on Issues & Risks API | H5 | Currently client search on mock CAPA list |
| Task count at scale | Performance | Execution loads 500 tasks/deficiencies once — paginate if needed |

---

## Readiness Terminology Changes

| Before | After | Location |
|--------|-------|----------|
| Overall Readiness | Compliance Readiness | Launchpad, execution summary |
| Active Stage | Current Stage | Launchpad |
| (core % as readiness) | Stage Readiness | Launchpad (from `maturityStages`) |
| Obligation Readiness (band analytics) | Removed from launchpad | H1B → CM |

---

## Key Files

- `WebApp/src/app/features/journey/routes.ts`
- `WebApp/src/app/features/journey/pages/execution-screen.component.ts`
- `WebApp/src/app/features/journey/pages/journey-maturity-dashboard.component.ts`
- `WebApp/src/app/features/journey/state/journey-workspace.store.ts`
- `WebApp/src/app/features/journey/components/journey-context-panel/`
- `WebApp/src/app/features/journey/components/standard-card/`
