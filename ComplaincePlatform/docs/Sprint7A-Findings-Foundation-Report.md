# PRAMAN Sprint 7A — Findings Foundation & Audit Workbench

**Date:** 2026-06-23  
**Scope:** Establish Findings as the discovery domain — no promotion, no remediation, no evidence verification  
**Goal:** *"What did we discover?"* — separate from Deficiency Command Center (*"What prevents accreditation?"*)

---

## Executive Summary

Sprint 7A transforms `/app/audit/findings` from a basic list into the **Findings Workbench** with dashboard KPIs, enriched workspace grid, in-page detail panel (attachments, comments, history), and promotion-ready metadata — while preserving strict separation from deficiencies, tasks, and evidence verification.

| Area | Result |
|------|--------|
| **Taxonomy** | 5 classifications, 8 sources, 5 severities, 5-state lifecycle (extended enums) |
| **Backend** | `FindingsWorkspaceQueryService`, comments/attachments, `PromotedDeficiencyId` placeholder |
| **Frontend** | Dashboard + grid + detail panel (mirrors DCC pattern) |
| **Promotion** | Architecture documented; **not implemented** (Sprint 7B) |

---

# Deliverable 1 — Findings Domain Audit

## Entity: `Finding` (unified TPH table)

| Field | Purpose |
|-------|---------|
| Scope | Organization registry vs Audit (journey-linked) |
| Title, Description, ClauseReference | Discovery content |
| Severity, Status, Department, RaisedBy | Classification & lifecycle |
| ObjectiveElementId | OE linkage (audit findings) |
| **FindingNumber** | Human-readable ID (Sprint 7A) |
| **Classification** | Sprint 7A taxonomy |
| **PromotedDeficiencyId** | Sprint 7B readiness (nullable) |
| Type (legacy) | NonConformanceType for journey audit findings |

## Dual tracks (pre-Sprint 7A)

| Track | API | Gap |
|-------|-----|-----|
| Organization registry | `GET/POST /findings` | No workspace projection |
| Journey audit | `GET/POST /journeys/{id}/audit-findings` | Different DTO shape |

## Sprint 7A resolution

`FindingsWorkspaceQueryService` queries **all findings** for `TenantId = organizationId` (both scopes), enriched with OE/chapter/standard joins.

## Sub-resources

| Resource | Pre-7A | Post-7A |
|----------|--------|---------|
| Attachments | Schema only (`FindingEvidence` ≠ audit attachments) | `FindingAttachment` entity + POST API |
| Comments | None | `FindingComment` entity + POST API |
| History | Write-only audit trail | GET via detail projection |

---

# Deliverable 2 — Findings Taxonomy

Enum: `FindingClassification`

| Value | Definition |
|-------|------------|
| AuditFinding | Issue identified during assessment |
| Observation | Minor concern requiring attention |
| NonConformity | Requirement not met |
| OpportunityForImprovement | Potential enhancement |
| GoodPractice | Positive compliance example |

Legacy `NonConformanceType` mapped: MajorNC/MinorNC → NonConformity; Observation → Observation; OFI → OFI.

---

# Deliverable 3 — Source Framework

Extended `FindingRaisedBy`:

| Source | Enum |
|--------|------|
| Internal Audit | InternalAudit |
| External Audit | ExternalAudit |
| Consultant Assessment | ConsultantAssessment |
| NABH Assessment | NabhAssessment |
| Management Review | ManagementReview |
| Department Assessment | DepartmentAssessment |
| Incident Review | IncidentReview |
| Compliance Team | ComplianceTeam (legacy) |

Every finding captures source via `RaisedBy` on create.

---

# Deliverable 4 — Severity Framework

Extended `FindingSeverity`:

| Severity | Definition |
|----------|------------|
| Critical | Highest impact |
| Major | Significant compliance risk |
| Moderate | Process weakness |
| Minor | Low impact |
| Informational | Awareness only |

Reuses shared enum — no duplicate severity logic on deficiencies.

---

# Deliverable 5 — Lifecycle Model

Extended `FindingStatus`:

```
Draft → Open → Reviewed → Accepted → Closed
```

Legacy states preserved: `InProgress`, `Verified` (mapped in UI to Open/Reviewed).

Detail panel renders lifecycle stepper. **No promotion transition yet** — `Accepted → Promoted To Deficiency` reserved for Sprint 7B.

---

# Deliverable 6 — Findings Workspace

## Route

`/app/audit/findings` (menu: **Findings** — unchanged)

## Grid columns

Finding Number · Type · Source · Severity · Chapter · Standard · OE · Department · Owner · Status · Created · Updated

## Features

Search · Sort · Filter (status, severity, type, source, department) · Grouping · Export CSV · Pagination

## Frontend structure

| Path | Role |
|------|------|
| `audit/pages/audit-findings-page.component.ts` | Workbench shell |
| `audit/findings-workspace/components/findings-dashboard.component.ts` | KPIs |
| `audit/findings-workspace/components/findings-grid.component.ts` | Grid |
| `audit/findings-workspace/components/findings-detail-panel.component.ts` | Detail panel |
| `audit/findings-workspace/services/findings-workspace.service.ts` | API client |

---

# Deliverable 7 — Analytics Dashboard

KPIs on findings page:

- Total · Open · Accepted · Closed · Critical · Draft
- Breakdown: By Source · By Department · By Chapter

Lightweight aggregates from `FindingsWorkspaceQueryService.GetDashboardAsync`.

---

# Deliverable 8 — Promotion Readiness Report

## Future flow (Sprint 7B — NOT implemented)

```
Finding (Accepted)
  ↓ Promote
ComplianceDeficiency
  ↓ auto
Primary Remediation Task
```

## Prepared metadata

| Finding field | Maps to Deficiency |
|---------------|-------------------|
| Title, Description | Title, Description |
| Severity | Severity |
| ObjectiveElementId | ObjectiveElementId |
| Classification | DeficiencyType (mapping TBD) |
| Source | Deficiency.Source context |
| PromotedDeficiencyId | Back-link after promotion |

## Dependencies for 7B

- `DeficiencyRemediationTaskService` — create primary task
- Idempotent promotion guard via `PromotedDeficiencyId`
- DCC grid shows promoted state (`isPromoted` flag in workspace DTO)

---

# Deliverable 9 — API Review

## New endpoints (additive)

| Method | Path | Purpose |
|--------|------|---------|
| GET | `/findings/workspace/dashboard` | KPIs |
| GET | `/findings/workspace` | Paginated grid |
| GET | `/findings/workspace/{id}` | Detail + comments + attachments + history |
| POST | `/findings/{id}/comments` | Add review comment |
| POST | `/findings/{id}/attachments` | Add audit attachment (URL metadata) |

## Extended create

`POST /findings` accepts `classification`, `status` (optional).

## Migration

`20260623143000_Sprint7A_FindingsFoundation.cs` — FindingNumber, Classification, PromotedDeficiencyId, FindingComments, FindingAttachments tables.

## Not added (by design)

- PATCH status transitions (future)
- Finding → Deficiency POST
- Evidence verification on findings
- Task assignment on findings

---

# Deliverable 10 — Validation Report

## Build validation

| Check | Result |
|-------|--------|
| `npm run build` (WebApp) | ✓ Pass |
| `dotnet build` (ComplianceService) | ✓ Pass |

## Separation validation

| Rule | Status |
|------|--------|
| Findings remain discovery-focused | ✓ No remediation controls in detail panel |
| Deficiencies remain remediation-focused | ✓ DCC unchanged |
| No evidence verification in Findings | ✓ Attachments are audit artifacts only |
| No task ownership in Findings | ✓ No task CRUD |
| Cross-nav to DCC is read-only link | ✓ |
| Promotion not implemented | ✓ `PromotedDeficiencyId` nullable only |

## Success criteria

| Criterion | Status |
|-----------|--------|
| Findings become first-class domain | ✓ |
| Discovery vs remediation separation | ✓ |
| Promotion-ready architecture | ✓ |
| No duplication with DCC / Evidence / Tasks | ✓ |
| Ready for Sprint 7B | ✓ |

## Manual test checklist

- [ ] Open `/app/audit/findings` — dashboard + grid load
- [ ] Filter by type, source, severity, status
- [ ] Select row — detail panel with lifecycle
- [ ] Add comment and attachment (URL-based)
- [ ] Cross-nav: Journey, Standards, DCC
- [ ] Export CSV
- [ ] Create finding with classification + source
- [ ] Run migration: `dotnet ef database update`

---

## Files changed (Sprint 7A)

### Backend
- `Shared.Infrastructure/Domain/Enums/` — FindingStatus, FindingSeverity, FindingRaisedBy extended; FindingClassification, FindingAttachmentType (new)
- `Domain/Finding.cs` — FindingNumber, Classification, PromotedDeficiencyId
- `Domain/FindingComment.cs`, `Domain/FindingAttachment.cs` (new)
- `Application/DTOs/FindingsWorkspaceDtos.cs` (new)
- `Application/Services/FindingsWorkspaceQueryService.cs` (new)
- `Application/Services/FindingCommentAttachmentHandlers.cs` (new)
- `Controllers/V1/FindingsController.cs` — workspace endpoints
- `Gateway/Controllers/FindingsV1ProxyController.cs` — proxy routes
- `Migrations/20260623143000_Sprint7A_FindingsFoundation.cs`

### Frontend
- `audit/findings-workspace/**` (new)
- `audit/pages/audit-findings-page.component.ts` — workbench shell
- `audit/models/finding.models.ts`, `finding-labels.ts` — extended enums

---

## Sprint 7B readiness

Promotion command will:
1. Validate finding status = Accepted
2. Create `ComplianceDeficiency` from finding metadata
3. Set `Finding.PromotedDeficiencyId`
4. Trigger `DeficiencyRemediationTaskService` for primary task
5. Surface promoted link in DCC detail (read-only back-link to finding)
