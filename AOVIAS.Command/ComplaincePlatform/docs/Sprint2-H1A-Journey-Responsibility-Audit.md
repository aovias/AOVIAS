# PRAMAN Sprint 2 — H1A Journey Responsibility Cleanup

**Date:** 2026-06-23  
**Sprint type:** Analysis-first (no code changes)  
**Scope:** Journey only — Compliance Maturity untouched  
**Predecessor:** Sprint 1 (H2 Stage Unlock Consolidation, H8 Gap Classification Framework)

---

## Executive Summary

Journey currently operates as **three overlapping products**:

1. **Compliance Maturity mirror** — `journey-maturity-dashboard` (stage grid, unlock, deficiencies, obligation bands)
2. **Audit lifecycle dashboard** — `journey.component` (operational mode cards, KPIs, trends, accreditation card)
3. **Execution workspace** — `execution-screen` + OE wizard (chapter → standard → OE → obligation → evidence → task)

The execution workspace is the strongest fit for the target **Compliance Execution Workspace**. The two landing dashboards duplicate Compliance Maturity and program-level analytics, diluting Journey's operational purpose.

**Recommended direction:** Journey becomes the daily workspace for *doing* compliance work. Governance forecasting, maturity analytics, and predicted gaps remain on Compliance Maturity (Sprint 3 H1B). Stage **context** stays on Journey (consumption only); stage **forecasting** does not.

---

# Deliverable 1 — Journey Responsibility Audit

## 1.1 Journey Surfaces (Routed)

| Surface | Route | Primary file | Current role |
|---------|-------|--------------|--------------|
| Maturity Dashboard | `/app/journey`, `/app/journey/maturity` | `journey-maturity-dashboard.component.ts` | Stage/unlock/deficiency overview (mirrors CM) |
| Operational View | `/app/journey/operational` | `journey.component.ts` | Audit lifecycle mode selector + KPIs/trends |
| Execution Workspace | `/app/journey/execution` | `execution-screen.component.ts` | Chapter/standard/OE navigation |
| OE Wizard | `/app/journey/execution/Progress/:oeGuid` | `objective-element-execution-screen.component.ts` | OE scoring, evidence, tasks, obligation projection |

**Shell:** `journey-feature-shell.component.ts` — Maturity ↔ Operational tabs; provides `JourneyWorkspaceStore`.

## 1.2 Widget & Component Inventory

### Maturity Dashboard (`journey-maturity-dashboard`)

| Widget | Component | Classification |
|--------|-----------|----------------|
| Stage progress strip | `stage-progress-strip` | Governance + Analytics |
| Overview metrics (Overall Readiness, Core Compliance, Active Stage) | Inline | Analytics |
| 4× maturity stage cards | Inline | Governance + Analytics |
| Obligation readiness bar | `obligation-readiness-bar` | Analytics |
| Obligation band summary grid | `obligation-band-summary-card` | Analytics |
| Compliance deficiencies by stage | `deficiency-summary-card` | Governance (execution-adjacent) |

**Actions:** Full Execution Workspace → `/app/journey/execution`; Compliance Maturity Dashboard → `/app/compliance-maturity`; per-stage CTA → execution with stage filter.

### Operational View (`journey.component`)

| Widget | Component | Classification |
|--------|-----------|----------------|
| Framework context | `journey-framework-context` | Governance |
| Audit lifecycle mode cards (4) | `journey-card` | Governance |
| Accreditation progress card | `accreditation-progress-card` (shared) | Analytics / Governance |
| Action bar | `action-bar` | **Execution** |
| Stage progress strip | `stage-progress-strip` | Governance |
| KPI metric cards (4) | `metric-card` | Analytics |
| Trend chart cards (3) | `chart-card` | Analytics |

**Actions:** Continue Journey → execution; Resume Last OE → in-place chapter select; View High Risk → chapter search filter.

### Execution Workspace

| Widget | Component | Classification |
|--------|-----------|----------------|
| Stage filter strip | Inline | Governance (filter only) |
| Chapter navigation | `execution-chapter-panel` | **Execution** |
| Standards + OE list | `execution-standards-panel` → `standard-card` | **Execution** |
| OE context panel | `journey-context-panel` | **Execution** |
| Obligation block | `obligation-state-section` | Governance + Analytics |

### OE Wizard

| Widget | Component | Classification |
|--------|-----------|----------------|
| Wizard steps | `oe-execution-wizard` | **Execution** |
| Create & assign task drawer | `create-assign-task-drawer` | **Execution** |
| Remediation sidebar | `oe-remediation-context` | Governance + **Execution** |

## 1.3 Journey APIs

| Endpoint | Service method | Returns | Classification |
|----------|----------------|---------|----------------|
| `GET /journeys/{id}/session` | `GetSessionAsync` | `JourneySessionDto` | Execution context |
| `GET /journeys/{id}/dashboard` | `GetDashboardAsync` | `JourneyDashboardDto` | **Mixed** (see §6) |
| `GET /journeys/{id}/chapters` | `GetChaptersIndexAsync` | `JourneyChaptersIndexDto` | Execution navigation |
| `GET /chapters/{id}/standards` | `GetChapterStandardsAsync` | `ComplianceStandardDto[]` | Execution |
| `GET /objective-elements/{id}` | `GetObjectiveElementDetailAsync` | `ObjectiveElementDto` | Execution |
| `PATCH /objective-elements/{id}` | `PatchObjectiveElementAsync` | Score/NA update | Execution |
| Evidence CRUD/verify | Various | Evidence DTOs | Execution |
| `GET/PUT/POST …/oe-execution-wizard` | `ObjectiveJourneyAppService` | Wizard state | Execution |
| `PATCH /session/operational-mode` | `PatchOperationalModeAsync` | Mode progression | Governance |
| `GET /journeys/{id}/workspace` | `GetWorkspaceAsync` | Legacy monolith snapshot | Legacy |
| `GET /journey/overview`, `/readiness` (Gateway) | Internal obligations | Obligation analytics | Analytics |

**Gateway:** Pass-through proxy via MediatR (`Gateway/Controllers/JourneysController.cs`); no separate Gateway DTO layer for dashboard.

## 1.4 Journey Calculations & Projections

| Calculation | Location | Owner today |
|-------------|----------|-------------|
| Framework aggregation (chapter/standard scores) | `AggregationService` | Backend — execution + analytics |
| Overview KPIs (`BuildOverview`) | `JourneyWorkspaceAppService` | Analytics |
| Stage breakdown (OE score %) | `BuildStageProgress` | Analytics |
| Obligation readiness | `ObligationReadinessCalculator` | Analytics / execution guidance |
| Accreditation eligibility (NABH rules) | `NabhAccreditationEvaluator` | Governance |
| Headline readiness % | `IComplianceScoreService.GetJourneyScoreAsync` | Governance |
| Stage unlock state | `IStageUnlockingService` → `MaturityStagePresentationMapper` | Governance (SSOT) |
| Trend series | `BuildTrendsAsync` (ComplianceSnapshots) | Analytics |
| Client NABH eligibility | `nabh-accreditation-decision.util.ts` → `store.accreditation` | **Duplicate** client analytics |
| Operational mode locks | `journey-operational-mode.policy.ts` | Governance (execution context) |
| Wizard eval score | Client checklist → score formula | Execution |
| OE progress `(score/5)×100` | `journeyContextPanelVm` | Analytics display |

**Audit flag:** `OeScoreTrend`, `ChapterImprovementTrend`, and `RiskEvolutionTrend` on dashboard all receive the **same** snapshot series from `BuildTrendsAsync`.

## 1.5 Classification Summary

| Category | Journey presence | Examples |
|----------|------------------|----------|
| **Execution** | Strong in execution + wizard | OE nav, wizard, evidence upload, task create, obligation next-action |
| **Governance** | Heavy on landings | Stage unlock display, operational mode, accreditation eligibility, deficiency counts |
| **Analytics** | Heavy on landings | KPI cards, trends, obligation bands, readiness % |
| **Forecast** | **None by name** | Closest: accreditation card subtitle "Estimated Audit Readiness" |
| **Prediction** | **None** | Correctly absent (CM owns Predicted Gaps) |

## 1.6 Legacy / Orphan Code (not routed)

| Component | Risk |
|-----------|------|
| `journey-tabs` (Findings, Analytics tabs) | Re-introduces duplicate analytics if wired |
| `accreditation-decision-panel` | Governance panel dormant |
| `journey-nabh-metrics-bar`, `journey-trends-mini` | Duplicate KPI/trend widgets |
| `journey-execution` (deprecated page) | Legacy layout |
| `oe-detail-drawer`, `journey-main-workspace` | Alternate execution UX |

---

# Deliverable 2 — Ownership Matrix

## Target: Journey = Compliance Execution Workspace

| Responsibility | Journey (target) | Compliance Maturity | Dashboard | Command Center |
|----------------|:------------------:|:-------------------:|:---------:|:--------------:|
| Current stage **context** (display) | ✓ consume | ✓ consume | — | — |
| Stage unlock **forecasting** | — | ✓ | — | — |
| Unlock audit history | — | ✓ (when built) | — | — |
| Predicted gaps | — | ✓ | — | — |
| CMI / maturity index | — | ✓ | — | — |
| Readiness forecast | — | ✓ | Partial | Partial |
| Operational mode selection | ✓ | — | — | — |
| Chapter → OE navigation | ✓ | — | — | — |
| OE execution wizard | ✓ | — | — | — |
| Obligation state per OE | ✓ | — | — | — |
| Deficiency **remediation** context | ✓ | View only | — | Partial |
| Evidence register/verify (OE-scoped) | ✓ | — | — | — |
| Task create/assign (OE-scoped) | ✓ | — | Partial | Partial |
| Current deficiencies (action list) | ✓ | Summary | Alert | KPI |
| Audit findings | Link only | — | — | ✓ |
| Program-level KPIs | Minimal | — | ✓ | ✓ |
| Multi-framework trends | — | — | ✓ | ✓ |
| AI / ops intelligence | — | — | — | ✓ |

## Journey Owns (post-refactor)

- Current stage context, readiness, status, blocking reasons (**consume** `StageUnlockingService`)
- Current deficiencies blocking execution
- Current tasks assigned to OEs/chapters
- Current evidence status per OE
- Current obligations and next actions
- Objective element execution state
- Execution progress within active operational mode
- Remediation actions (launch Tasks, Evidence, Issues & Risks)

## Journey Does NOT Own

- Readiness forecast, predicted opportunities, unlock audit history
- Long-term maturity analytics, CMI, chapter heatmaps
- Governance reporting, accreditation forecast panels
- Stage forecasting, future-stage prediction

---

# Deliverable 3 — Execution Workflow Mapping

## 3.1 Canonical Chain

```
Stage (maturity band context)
  ↓
Objective Element (catalog + score + workflow status)
  ↓
Obligation (OrganizationComplianceObligation projection)
  ↓
Deficiency (ComplianceDeficiency engine)
  ↓
Task (ComplianceTask)
  ↓
Evidence (ObjectiveElementEvidence)
  ↓
Verification (Evidence Verification Pipeline)
```

## 3.2 Journey Coverage Today

| Step | Backend source | Journey UI | Action links |
|------|----------------|------------|--------------|
| **Stage** | `MaturityStages` on dashboard DTO | Maturity dashboard cards, stage strip, execution stage filter | → `/app/journey/execution?stage=` |
| **Objective Element** | `ObjectiveElementDto`, chapter standards API | `standard-card`, `journey-context-panel` | → wizard `/execution/Progress/:id` |
| **Obligation** | `ObligationStateDto` on OE | `obligation-state-section`, `obligation-readiness-bar` | Next action label (no deep link yet) |
| **Deficiency** | `DeficiencyDataService` | `deficiency-summary-card`, `oe-remediation-context` | — (no Issues & Risks deep link) |
| **Task** | `ComplianceTaskApiService`, `TaskApiService` | Wizard drawer, remediation links | → `/app/tasks?search=` |
| **Evidence** | Evidence API on OE | Wizard upload, `oe-card` status | → `/app/evidence?source=oe-wizard&…` |
| **Verification** | Evidence `verificationStatus` | Wizard evidence table, obligation section | Inline verify in wizard |

## 3.3 Gaps in Workflow Visualization

| Gap | Detail |
|-----|--------|
| Context panel stubs | Upload Evidence, Add Task, View History, View Timeline — **no-op** on execution screen (wizard implements fully) |
| No deficiency deep link | Remediation shows deficiencies but does not link to Issues & Risks |
| OE row missing deficiency/task counts | `ExecutionOeItemVm` has evidence label, journey status — **no deficiencyCount or taskCount** |
| Obligation → deficiency chain not visualized | Shown as separate panels, not as connected workflow |
| Findings not linked | No route to Findings module from Journey |

## 3.4 Recommended Execution-First Landing

Post-refactor default flow:

```
Journey landing → Execution Workspace (with stage context strip)
  ├── Left: chapters needing action
  ├── Center: OEs with status chips (obligation, evidence, deficiency, task)
  └── Right: context panel with action links
        ├── Start/Resume OE wizard
        ├── Upload evidence → Evidence module
        ├── Add/view tasks → Tasks module
        └── View deficiencies → Issues & Risks
```

Maturity dashboard content **demoted** to Compliance Maturity or folded into a compact stage context bar on execution landing.

---

# Deliverable 4 — Non-Journey Responsibility Report

## 4.1 Items on Journey That Belong Elsewhere

| Item | Current Journey location | Future owner | Relocate in |
|------|-------------------------|--------------|-------------|
| 4× maturity stage cards + obligation bands | `journey-maturity-dashboard` | Compliance Maturity | Sprint 3 H1B |
| Duplicate stage progress strip (analytics copy) | Maturity + Operational landings | CM for full timeline; Journey keeps compact context | Sprint 3 / H1A trim |
| CMI-adjacent readiness KPIs | JMD overview metrics | Compliance Maturity | Sprint 3 |
| Predicted opportunities | Not on Journey ✓ | Compliance Maturity | N/A |
| 7-day trend charts (OE/chapter/risk) | `journey.component` chart-cards | Dashboard or CM trends | Sprint 3+ |
| Accreditation forecast card | Operational `accreditation-progress-card` | Compliance Maturity or Command Center | Sprint 3 / 5 |
| NABH eligibility panel | Client `store.accreditation` + dormant `accreditation-decision-panel` | Compliance Maturity governance | Sprint 3 |
| Obligation band summary (maturity analytics) | JMD `obligation-band-summary-card` | Compliance Maturity | Sprint 3 |
| Chapter heatmap | Not on Journey ✓ | CM (already) | N/A |
| Program-level task KPIs | Operational metrics | Dashboard / Command Center | Out of scope |

## 4.2 Items Correctly on Journey (keep)

| Item | Rationale |
|------|-----------|
| Operational mode cards | Execution context — drives catalog filter and read-only behavior |
| Execution workspace tree | Core execution UX |
| OE wizard | Core execution UX |
| Stage filter on execution | Scopes work to active maturity band |
| Compact stage context (status, readiness, blockers) | Answers "what is blocking me?" |
| Deficiency list **for active work** | Execution remediation, not governance summary |
| Obligation next-action on OE | Drives daily work |

## 4.3 Relocation Strategy (plan only — do not move in Sprint 2)

1. **Phase A (H1A implementation):** Slim Journey landings; redirect maturity dashboard CTAs; enrich execution context.
2. **Phase B (H1B):** CM absorbs trimmed widgets; remove duplicate strips from Journey operational view.
3. **Phase C:** Remove orphan legacy components or archive under `_deprecated/`.

---

# Deliverable 5 — Dashboard Overlap Report

## 5.1 Overlap Matrix

| Widget / KPI | Journey | Compliance Maturity | Dashboard | Command Center | Overlap severity |
|--------------|---------|---------------------|-----------|------------------|------------------|
| Maturity stage strip | ✓ (2 landings) | ✓ timeline | — | — | **HIGH** |
| Stage cards / unlock blockers | ✓ JMD | ✓ sidebar + timeline | — | — | **HIGH** |
| Core readiness % | ✓ | ✓ executive KPI | ✓ gauge | ✓ KPI bar | **HIGH** (J↔CM) |
| Open deficiencies | ✓ per-stage cards | ✓ core panel | alerts | audit readiness | **HIGH** (J↔CM) |
| CMI | — | ✓ | — | — | CM-only ✓ |
| Predicted gaps | — | ✓ | — | — | Correct ✓ |
| Trend charts | ✓ operational | — | ✓ | ✓ analytics | **MEDIUM** |
| Task pressure | ✓ open plans | ✓ next actions | ✓ overdue | ✓ action center | **MEDIUM** |
| Chapter heatmap | — | ✓ (shared component) | ✓ | — | CM↔Dashboard |
| AI intelligence | — | — | — | ✓ | CC-only ✓ |

## 5.2 Primary Duplicate Surface

**`journey-maturity-dashboard.component.ts`** functions as a second Compliance Maturity dashboard:

- Title: "Compliance Maturity Journey"
- Link to `/app/compliance-maturity`
- Duplicate stage/unlock/deficiency/obligation views
- Same backend source (`maturityStages` from `StageUnlockingService`)

## 5.3 Removal Candidates (recommendation — not executed)

| Candidate | Reason | Risk |
|-----------|--------|------|
| JMD stage cards grid | Duplicates CM timeline | Medium — hospital users use as landing |
| JMD obligation band summary | Maturity analytics | Low |
| Operational trend chart-cards | Program analytics | Medium — auditors use operational view |
| Duplicate stage strip on operational view | Keep one compact strip on execution | Low |
| Client `store.accreditation` | Duplicates server accreditation DTO | Low |
| Orphan `journey-tabs` Analytics/Findings | Dead code | None |

**Do NOT remove:** Stage visibility entirely (success criterion); execution workspace; operational mode selector.

---

# Deliverable 6 — API Review

## 6.1 `JourneyDashboardDto` Property Analysis

| Property | Used by Journey UI | Classification | Recommendation |
|----------|-------------------|----------------|----------------|
| `Overview` | KPI cards, JMD metrics | Analytics | **Keep** — execution-relevant subset only on Journey |
| `Overview.StageBreakdown` | Stage scores (OE-based) | Analytics | Keep for OE score display; distinct from unlock state |
| `Overview.ObligationBreakdown` | Obligation bands | Analytics | **Move display to CM**; keep on API for execution obligation context |
| `Overview.ObligationReadinessScore` | Readiness bars | Analytics | Same |
| `Accreditation` | Eligibility violations | Governance | **Trim from Journey landing**; keep for CM/CC |
| `AccreditationReadinessPercent` | Accreditation card | Governance | Display on CM; Journey execution may show as single KPI |
| `OeScoreTrend` | Chart card | Analytics | **Deprecate on Journey** or fix to distinct series |
| `ChapterImprovementTrend` | Chart card | Analytics | Same — currently **identical data** to OeScoreTrend |
| `RiskEvolutionTrend` | Chart card | Analytics | Same |
| `CurrentStage` | Stage context | Governance (context) | **Keep** — consume only |
| `MaturityStages` | Stage strip/cards | Governance (context) | **Keep compact subset** on execution |

## 6.2 Unused / Underused Properties

| Property | Issue |
|----------|-------|
| `ChapterImprovementTrend` vs `RiskEvolutionTrend` | Backend populates all three from same `BuildTrendsAsync` output — semantic mismatch |
| `JourneyChapterIndexItemDto.MaturityLabel` | Phase 48 stub ("Maturity L3") — not real analytics |
| `ChangedSinceLastAudit`, `PassFailLabel` | Mock/null in mapper — execution placeholders |
| `GET /workspace` monolith | Legacy; v2 split API preferred |

## 6.3 Non-Breaking API Recommendations

1. **Document** `MaturityStages` as authoritative for unlock; `StageBreakdown` as OE score aggregation (already distinct concepts).
2. **Fix trend series** — populate three arrays from distinct snapshot dimensions (backend, Sprint 3+).
3. **Optional:** Add `JourneyExecutionSummaryDto` with execution-only fields (open deficiencies count, tasks due, evidence gaps) without removing dashboard fields.
4. **Optional:** Add `deficiencyCount`, `openTaskCount` to `ObjectiveElementDto` or `ObligationStateDto` for OE row display (projection only, no new entities).
5. **Do not remove** dashboard properties in H1A — deprecate consumption first.

## 6.4 Gateway

No schema changes required. Journey dashboard fields pass through JSON as-is. New optional fields can be added to existing DTOs with defaults.

---

# Deliverable 7 — Refactor Plan

## HIGH Priority

| # | Item | Current owner | Future owner | Reason | Risk | Dependencies |
|---|------|---------------|--------------|--------|------|--------------|
| H1 | Reposition default landing to **Execution Workspace** | JMD | Execution screen + compact stage bar | Journey = daily workspace | Medium — user habit | None |
| H2 | Slim **journey-maturity-dashboard** to stage context + CTAs only | JMD full dashboard | Execution entry point | Removes CM duplication | Medium | H1 |
| H3 | Wire **context panel actions** (evidence, task, deficiencies) | Stubs on execution | Deep links to Evidence, Tasks, Issues & Risks | Orchestration without duplication | Low | None |
| H4 | Add **OE row execution signals** (deficiency count, task count, evidence/verification) | Partial on `oe-card` | `standard-card` / execution list | Answers "what requires action?" | Low | Optional API fields |
| H5 | Remove **client-side NABH accreditation** calc | `store.accreditation` | Server `Accreditation` DTO only | Single truth | Low | None |
| H6 | Fix **Overall Readiness label** on JMD | Uses `coreCompliancePercent` | Use `accreditationReadinessPercent` or stage readiness | Label accuracy | Low | None |

## MEDIUM Priority

| # | Item | Current owner | Future owner | Reason | Risk | Dependencies |
|---|------|---------------|--------------|--------|------|--------------|
| M1 | Consolidate **stage strip** to execution landing only | 2 landings | Execution + optional operational | Reduce duplicate governance UI | Low | H1 |
| M2 | Trim **operational view** KPI/trend cards | Operational landing | Dashboard/CC | Program analytics off Journey | Medium | User research |
| M3 | **Deficiency summary** on JMD → execution sidebar | JMD cards | `oe-remediation-context` pattern | Execution-centric deficiencies | Low | H3 |
| M4 | Connect **obligation next-action** to routes | Label only | Task/Evidence/I&R links | Close workflow loop | Low | H3 |
| M5 | Delete or archive **orphan components** | Legacy folder | Removed | Prevent re-wiring duplicates | None | H2 |
| M6 | Align **role redirect** | Hospital → JMD | Hospital → Execution | Reinforce workspace identity | Low | H1 |

## LOW Priority

| # | Item | Current owner | Future owner | Reason | Risk | Dependencies |
|---|------|---------------|--------------|--------|------|--------------|
| L1 | Fix backend **trend series** semantics | Same data × 3 | Distinct series | Accurate analytics | Low | Sprint 3+ |
| L2 | Remove **Phase 48 mock fields** or implement | Mock hash/modulo | Real audit diff API | Data integrity | Medium | Backend |
| L3 | **`JourneyExecutionSummaryDto`** | N/A | New optional endpoint slice | Cleaner API contract | Low | API review |
| L4 | Wizard **View Timeline** stub | No-op | OE history panel | UX completeness | None | None |

## Implementation Sequence (Sprint 2 implementation phase — after approval)

```
Week 1: H3 (action links) + H5 (client accreditation removal) + H6 (label fix)
Week 2: H1/H2 (landing reposition + JMD slim) + M1 (stage strip consolidation)
Week 3: H4 (OE execution signals) + M3/M4 (deficiency/obligation links)
Week 4: M2/M5/M6 + validation against success criteria
```

---

# Deliverable 8 — Risk Assessment

## 8.1 Risks

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| Hospital users lose familiar JMD landing | High | Medium | Phased slim-down; keep stage visibility; add "Maturity View" link to CM |
| Auditors depend on operational KPI/trends | Medium | Medium | Retain operational view tab; defer M2 until auditor sign-off |
| Action link deep links break without query contract | Medium | Low | Define stable query params with Evidence/Tasks/I&R teams |
| Removing JMD widgets before H1B leaves CM gap | Low | High | **Do not remove CM**; only trim Journey; H1B follows |
| Stage context regression | Low | High | Keep `StageUnlockingService` consumption; regression test Journey = CM stage state |
| Legacy component re-wiring during refactor | Medium | Medium | Archive orphans in M5 before other changes |
| API field deprecation breaks Gateway/clients | Low | Medium | Deprecate consumption only; no breaking DTO removals in H1A |

## 8.2 Success Criteria Mapping

| Criterion | Audit status | Action |
|-----------|--------------|--------|
| Journey = Compliance Execution Workspace | **Not yet** — landings are analytics-heavy | H1, H2, H3 |
| Stage visibility remains | ✓ Present | Preserve compact strip |
| StageUnlockingService SSOT | ✓ Sprint 1 | Verify no new client thresholds |
| No menu changes | ✓ Plan compliant | Route defaults only |
| No screen redesigns | ✓ Plan compliant | Content relocation, not layout overhaul |
| Standards & Chapters untouched | ✓ | Execution launcher unchanged |
| No CM duplication | **Partial** — JMD duplicates CM | H2, M1 |
| Clear ownership boundaries | ✓ Documented | This report |
| Ready for Sprint 3 H1B | ✓ | Relocation report §4.3 |

## 8.3 Validation Checklist (post-implementation)

- [ ] Journey default route lands on execution workspace (or slim execution-oriented landing)
- [ ] Stage strip on Journey matches Compliance Maturity stage state for same org
- [ ] No client-side 70/75/85 or unlock thresholds
- [ ] Context panel actions navigate to Evidence, Tasks, Issues & Risks
- [ ] JMD no longer duplicates CM stage grid + obligation bands
- [ ] Operational mode selection still functional
- [ ] OE wizard evidence/task flows unchanged
- [ ] `dotnet test` + `npm run build` pass

---

# Appendix A — Key File Reference

### Backend
- `ComplianceService/Application/DTOs/JourneyScreenDtos.cs`
- `ComplianceService/Application/DTOs/JourneyWorkspaceDtos.cs`
- `ComplianceService/Application/Services/JourneyWorkspaceAppService.cs`
- `ComplianceService/Application/Maturity/MaturityStagePresentationMapper.cs`
- `Gateway/Controllers/JourneysController.cs`

### Frontend
- `WebApp/src/app/features/journey/pages/journey-maturity-dashboard.component.ts`
- `WebApp/src/app/features/journey/pages/journey.component.ts`
- `WebApp/src/app/features/journey/pages/execution-screen.component.ts`
- `WebApp/src/app/features/journey/state/journey-workspace.store.ts`
- `WebApp/src/app/features/journey/mappers/journey-workspace.mapper.ts`
- `WebApp/src/app/features/journey/components/journey-context-panel/`
- `WebApp/src/app/features/journey/components/oe-execution-wizard/`

### Related (read-only for overlap)
- `WebApp/src/app/features/compliance-maturity/pages/compliance-maturity.component.ts`
- `WebApp/src/app/features/dashboard/pages/dashboard.component.ts`
- `WebApp/src/app/features/control-center/pages/compliance-command-center.component.ts`

---

# Appendix B — Action-Centric UX Audit (Phase 6)

| Action | Belongs in Journey? | Launch target | Current state |
|--------|--------------------|--------------| --------------|
| Start/Resume OE | ✓ Yes | Wizard route | ✓ Working |
| Upload evidence | ✓ Orchestrate | `/app/evidence?…` | Wizard ✓; context panel stub |
| Add/view tasks | ✓ Orchestrate | `/app/tasks?…` | Wizard drawer ✓; remediation links ✓ |
| View deficiencies | ✓ Orchestrate | `/app/issues-risks` (or equivalent) | **Missing link** |
| View audit findings | Link only | `/app/audit/findings` | **Not linked** |
| View compliance maturity | Link only | `/app/compliance-maturity` | ✓ JMD header |
| Continue to execution | ✓ Yes | `/app/journey/execution` | ✓ Action bar |
| Switch operational mode | ✓ Yes | In-place PATCH | ✓ Mode cards |
| Score/patch OE | ✓ Yes | PATCH API / wizard | ✓ Working |
| Verify evidence | ✓ Yes | Wizard inline | ✓ Working |
| View program trends | ✗ No | Dashboard | On operational view — relocate |
| View predicted gaps | ✗ No | Compliance Maturity | Correctly absent |

**Principle:** Journey orchestrates; Tasks, Evidence, Findings, Issues & Risks execute in their modules.

---

*End of Sprint 2 H1A analysis. No code was modified. Implementation awaits approval.*
