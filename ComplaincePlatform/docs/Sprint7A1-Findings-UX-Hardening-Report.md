# PRAMAN Sprint 7A.1 — Findings Workbench UX Hardening

**Date:** 2026-06-23  
**Scope:** Strengthen Findings discovery domain before full promotion rollout  
**Goal:** Structured audit capture, lifecycle visibility, promotion readiness indicators — **no remediation workflows**

---

## Executive Summary

Sprint 7A.1 hardens the Findings Workbench for real-world audit workflows while preserving strict domain separation. Findings remain discovery-only; Deficiency Command Center remains the sole remediation hub.

| Deliverable | Status |
|-------------|--------|
| Structured finding creation | ✓ 6-section capture panel |
| Lifecycle visibility | ✓ Stepper + timestamps + status PATCH |
| Promotion readiness | ✓ Badges (no promote action in UI) |
| Dashboard KPIs | ✓ 7 KPIs + classification/source breakdown |
| Enhanced detail panel | ✓ 8 sections |
| Search & filtering | ✓ Extended filters + quick chips |
| Cross navigation | ✓ Journey, Standards, Compliance Maturity (read-only) |
| Migration | ✓ `Sprint7A1_FindingsUxHardening` |

---

## Phase 1 — Finding Creation Enhancement

**Component:** `findings-create-panel.component.ts`

| Section | Fields |
|---------|--------|
| Finding Information | Title, Classification, Source, Severity, Department |
| Compliance Context | OE search/picker → Chapter, Standard, OE |
| Audit Context | Audit Date, Auditor, Audit Cycle |
| Observation Details | Observation, Requirement, Impact, Recommendation |
| Reference | Clause/Reference, Summary Description |
| Attachments | URL metadata (Document, Image, Screenshot, etc.) |

**API:** Extended `POST /findings` with optional audit fields + `objectiveElementId` + nested `attachments[]`.

**New entity columns (nullable):** `AuditDate`, `AuditorName`, `AuditCycle`, `Observation`, `Requirement`, `Impact`, `Recommendation`.

---

## Phase 2 — Lifecycle Visibility

- Detail panel lifecycle stepper: **Draft → Open → Reviewed → Accepted → Promoted → Closed**
- Timestamps from audit trail + created date
- Actor user IDs on history entries
- **`PATCH /findings/{id}/status`** with `StatusChanged` audit trail logging

---

## Phase 3 — Promotion Readiness (Visibility Only)

**Values:** Not Eligible · Awaiting Acceptance · Eligible · Promoted

Computed by `FindingPromotionReadiness` (no promotion execution in this sprint UI).

Displayed on:
- Grid column
- Detail panel badge
- Dashboard KPI (Eligible For Promotion)
- Filter dropdown

Promote button **removed** from detail panel per 7A.1 scope (readiness only).

---

## Phase 4 — Dashboard Enhancement

**KPIs:** Total · Open · Accepted · Awaiting Review · Eligible For Promotion · Promoted · Critical

**Breakdowns:**
- By Classification (Audit Finding, Observation, NC, OFI, Good Practice)
- By Source
- By Chapter

---

## Phase 5 — Detail Panel Sections

Summary · Compliance Context · Audit Context · Observation · Recommendation · Attachments · Comments · History · Cross Navigation

Lifecycle status dropdown for discovery workflow transitions only.

---

## Phase 6 — Search & Filtering

**Filters:** Status, Promotion Readiness, Classification, Source, Severity, Department, Chapter code, Standard code, Search

**Quick chips:** Needs Review · Ready For Promotion · Recently Added · Critical

**API:** Extended `GET /findings/workspace` query parameters.

**Compliance picker:** `GET /findings/workspace/compliance-context?search=`

---

## Phase 7 — Cross Navigation

Read-only links from detail panel:
- Journey (OE context)
- Standards & Chapters (chapter search)
- Compliance Maturity

No task, evidence, or deficiency remediation controls in Findings.

---

## Phase 8 — Validation

| Rule | Verified |
|------|----------|
| Findings remain discovery-only | ✓ |
| No task ownership | ✓ |
| No evidence verification | ✓ |
| No deficiency editing | ✓ |
| No remediation workflows | ✓ |
| Backward compatible (nullable fields) | ✓ |
| Frontend build | ✓ |
| Backend migration generated | ✓ |

---

## Key Files

| Layer | Files |
|-------|-------|
| Domain | `Finding.cs` (+ audit narrative fields) |
| Services | `FindingPromotionReadiness.cs`, `PatchFindingStatusCommandHandler.cs`, `FindingsWorkspaceQueryService.cs` |
| API | `FindingsController.cs` (status PATCH, compliance-context) |
| UI | `findings-create-panel`, updated dashboard/grid/detail/page |
| Migration | `Sprint7A1_FindingsUxHardening` |

---

## Next: Sprint 7B

With OE mapping on create and Accepted lifecycle transitions, findings can reach **Eligible** promotion readiness. Sprint 7B promotion command (already implemented backend-side) can be re-enabled in UI when ready.
