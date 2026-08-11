# Phase 2 — Audit Management System (Sprint 2)

**Sprint:** 2  
**Status:** Delivered  
**Principle:** Extend ComplianceService `AuditSession` + `Finding` + `ScheduledAudit` as SSOT. Facade legacy `AuditService` for AIOrchestrator. Do **not** introduce a new audit microservice.

## Model (SSOT)

| Entity | Change |
|--------|--------|
| `ScheduledAudit` | Thin program metadata: `ProgramYear`, `ProgramMonth`, `AuditType?`, `ProgramName?` (no separate AuditProgram aggregate) |
| `AuditType` | Commercial core: Internal, Department, Follow-up, Mock NABH (`MockAudit`), Focused (+ legacy NABH/consultant/surveillance) |
| `AuditObservation` | Checklist keys: `ChapterCode`, `OeCode` (plus existing Chapter/OE Guids) |
| `AuditSession` | Optional `ScheduledAuditId`, `ComplianceProgramId` (AI generate facade); close via status transition |
| `Finding` | Close gate uses `AuditSessionId` + Resolved / Closed / Promoted |

### Audit type mapping

`AuditTypeMapping` maps commercial labels ↔ enum:

| Label | Enum |
|-------|------|
| Internal | `InternalAudit` |
| Department | `DepartmentAudit` |
| Follow-up | `FollowUpAudit` |
| Mock NABH | `MockAudit` |
| Focused | `FocusedAudit` |

### Session close gate

`AuditSessionCloseGate` blocks close when any finding linked to `AuditSessionId` is not:

1. **Closed** — `FindingStatus.Closed`, or  
2. **Resolved** — `IsResolved == true`, or  
3. **Promoted** — `PromotedDeficiencyId != null`

API: `POST /api/v1/auditor/sessions/{id}/close`

## APIs

### ComplianceService

| Method | Route | Purpose |
|--------|--------|---------|
| GET/POST | `/api/v1/audit-schedule`, `/calendar` | List/create scheduled audits; calendar query by year/month/range |
| POST | `/api/v1/auditor/sessions/{id}/close` | Close with open-findings gate |
| POST | `/api/v1/auditor/simulation` | Live `AuditSimulationService` against hospital context |
| GET | `/api/v1/auditor/report/{sessionId}` | Session markdown report (`AuditorReportService`) |
| GET | `/api/v1/reports/audit` (+ export) | Hospital findings rollup (`AuditReportService`) |
| POST/GET | `/api/v1/audit/generate`, `/audit/{id}` | Facade replacing legacy AuditService generate |

### Gateway (thin proxy)

| Method | Route |
|--------|--------|
| * | `/api/v1/audit-schedule/**` |
| POST | `/api/v1/auditor/sessions/{id}/close` |
| POST | `/api/v1/auditor/simulation` |
| * | `/api/v1/audit/generate`, `/api/v1/audit/{id}` |
| Existing | `/api/v1/auditor/**`, `/api/v1/reports/audit` |

### AIOrchestrator

`AuditClient` posts to Compliance `/api/v1.0/audit/generate` (InternalClient → Compliance BaseUrl) and unwraps `ApiResponse.data.id`.

## Frontend (WebApp)

| Route | Behavior |
|-------|----------|
| `/app/audit` | Live KPIs from Control Center + auditor dashboard + audit report |
| `/app/audit/simulation` | Gateway → Compliance simulation |
| `/app/audit/reports` | Live `/reports/audit` export + session list |
| `/app/audit/planning` | Scheduled audit list/create (calendar) |
| `/app/auditor/*` | Unchanged external auditor workspace |

## Tests

- `AuditSessionCloseGateTests` — Resolved / Closed / Promoted allow; open blocks  
- `AuditTypeMappingTests` — commercial label ↔ enum mapping  

## Migration

`20260726150000_Phase2_AuditManagement` — observation codes, session program/schedule FKs, scheduled audit program year/month metadata.
