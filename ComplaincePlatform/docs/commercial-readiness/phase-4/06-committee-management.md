# Phase 4 — Committee / Management Review (Sprint 5)

**Sprint:** 5  
**Status:** Delivered  
**Principle:** EXTEND GovernanceService committees — do **not** introduce a parallel MR module.

## Model (SSOT)

| Entity / field | Purpose |
|----------------|---------|
| `AgendaTemplate` | Reusable agenda sections (`Standard`, `ManagementReview`, `PatientSafety`) |
| `CommitteeMeeting.AgendaTemplateCode` | Selected template at schedule time |
| `CommitteeMeeting.AttendanceJson` | Structured attendance payload |
| `CommitteeMeeting.MinutesApproved` (+ At / By) | Minutes approval gate |
| `CommitteeActionItem` | Action tracking with optional `TaskId` → ComplianceTask |
| `CommitteeDecision.DeficiencyId` / `RiskId` / `TaskId` | Typed decision links |
| Lookup `CommitteeType.ManagementReview` | Management Review committee type |

### Business rules (enforced)

1. Attendance / decisions / new actions rejected after minutes approved.  
2. Minutes approval requires completed meeting + non-empty minutes.  
3. Closed action items cannot be re-linked or status-changed.  
4. Action items store `TaskId` (created via Gateway `/api/v1/tasks` or supplied by client).

## Management Review lite

- Seeded `CommitteeType` = `ManagementReview` + agenda template.  
- `GET /governance/management-review/agenda` returns section list + placeholder incident/CAPA/risk/audit blocks (Gateway/Compliance enrichment optional).

## APIs (GovernanceService + Gateway)

| Method | Route |
|--------|--------|
| GET/POST | `/api/v1/committees/{id}/meetings` |
| GET | `/api/v1/committees/{id}/meetings/{meetingId}` |
| POST | `.../attendance`, `.../complete`, `.../approve-minutes` |
| POST | `.../decisions`, `.../actions` |
| POST/PATCH | `.../actions/{id}/link-task`, `.../status` |
| GET | `/api/v1/governance/agenda-templates` |
| GET | `/api/v1/governance/management-review/agenda` |
| GET | `/api/v1/governance/lookups/{category}` |

## Graph

- Risk nodes + search added to `EnterpriseComplianceGraphService` (coordinates with Risk sprint — `Risk.TenantId` present).  
- `GovernanceTraceLinks` table projects Committee/Meeting ↔ Risk/Document/Task/Deficiency edges (Gateway/internal upsert).  
- Compliance Graph nav shown for QM/manager roles (`shouldShowComplianceGraphNav`).

## WebApp

- Committee type / meeting type / decision type / role **selects** (lookups) replace raw GUID placeholders where feasible.  
- Meetings return real `meetingId`; MR template + action tracking UI.  

## Migration

`20260726200000_Phase4_CommitteeManagement` (Governance)  
`20260726201000_Phase4_GovernanceTraceLinks` (Compliance)

## Exit criteria

Schedule → attendance → decisions with typed links → action + TaskId → complete → approve minutes is operable; MR template available; graph nav for QM.
