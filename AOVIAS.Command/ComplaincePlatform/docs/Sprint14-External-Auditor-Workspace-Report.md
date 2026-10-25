# Sprint 14 — External Auditor Workspace Report

**Date:** June 25, 2026  
**Status:** Implemented  
**Architecture constraint:** Independent assessment layer — not an execution module.

---

## 1. Vision & Objective

Sprint 14 introduces a dedicated **External Auditor Workspace** that sits above the operational PRAMAN platform. External auditors assess, observe, comment, raise findings, and recommend — but **never** become operators. Hospital teams retain ownership of remediation via the existing deficiency/CAPA pipeline.

```
Hospital Team → Always Audit Ready → External Auditor → Independent Assessment
  → Findings → Promotion (Sprint 7B) → Existing Deficiency Workflow
```

---

## 2. Architecture

### 2.1 Layering

| Layer | Responsibility | Sprint 14 Role |
|-------|----------------|----------------|
| ComplianceService | Domain SSOT | New `AuditSession`, `AuditObservation`, `AuditComment` aggregates |
| Findings (7A/7B) | Discovery → Promotion | Reused; optional `AuditSessionId` linkage |
| DCC / CAPA / Tasks | Remediation | Read-only visibility for auditors |
| Analytics (12B) | KPI SSOT | `AnalyticsAggregationService` reused for readiness |
| Copilot Drafts (13C) | Advisory documents | Report generation follows same advisory pattern |
| Gateway | BFF | `AuditorV1ProxyController` proxies `/api/v1/auditor/*` |
| WebApp | Enterprise shell | `/app/auditor/*` — no new shell |

### 2.2 Key Backend Paths

```
ComplianceService/Domain/Auditor/
  AuditSession.cs
  AuditObservation.cs
  AuditComment.cs

ComplianceService/Domain/Enums/
  AuditType.cs, AuditSessionStatus.cs, AuditSessionResult.cs
  ObservationType.cs, AuditCommentType.cs

ComplianceService/Application/Services/
  AuditorWorkspaceQueryService.cs
  AuditorTraceabilityService.cs
  AuditorReportService.cs

ComplianceService/Controllers/V1/AuditorController.cs
Gateway/Controllers/AuditorV1ProxyController.cs
```

### 2.3 Frontend Paths

```
WebApp/src/app/features/auditor/
  routes.ts
  models/auditor-workspace.model.ts
  services/auditor-api.service.ts
  pages/
    auditor-dashboard-page.component.ts
    auditor-sessions-page.component.ts
    auditor-session-workspace-page.component.ts
    auditor-assessment-page.component.ts
    auditor-reports-page.component.ts
```

---

## 3. Domain Models

### 3.1 AuditSession

| Field | Purpose |
|-------|---------|
| SessionNumber | Human-readable ID (`AUD-2026-0001`) |
| AuditType | Internal, Mock, NABH Pre/Final, Surveillance, etc. |
| Scope | Assessment scope description |
| LeadAuditor / AuditorTeamJson | Team composition |
| Planned/Actual dates | Session scheduling |
| Status | Planned → InProgress → Review → DraftReport → Submitted → Closed |
| OverallResult | Compliant / NonCompliant / etc. |
| Summary | Executive summary |

### 3.2 AuditObservation

Non-remediation capture. Types: PositivePractice, Suggestion, MinorObservation, Strength, Opportunity, GeneralComment.

**Never creates deficiencies.**

### 3.3 AuditComment

Threaded conversation on a session. Types: GeneralComment, ClarificationRequest, Recommendation, PositiveObservation, Question. Hospital users may reply; `IsAuditorAuthor` distinguishes roles.

### 3.4 Finding Linkage

`Finding.AuditSessionId` (optional) links findings raised during a session without duplicating the Findings domain.

---

## 4. Permissions (RBAC)

### 4.1 New Role

`EXTERNAL_AUDITOR` added to `ApplicationRoles` and frontend `role-definitions.ts`.

### 4.2 Capability Matrix

| Capability | External Auditor | Hospital Operator |
|------------|------------------|-------------------|
| Read Journey / Evidence / Tasks / Deficiencies / CAPA | ✓ | ✓ |
| Create Findings | ✓ (existing API) | ✓ |
| Create Observations | ✓ | — |
| Create Comments | ✓ | ✓ (reply) |
| Generate Reports | ✓ | ✓ |
| Approve / Verify / Assign / Close / Unlock | ✗ | ✓ (role-dependent) |

API gate: `ComplianceScope` policy + role checks on mutating session/observation endpoints.

---

## 5. APIs

Base: **`GET/POST /api/v1/auditor/*`** (via Gateway)

| Method | Route | Purpose |
|--------|-------|---------|
| GET | `/auditor/dashboard` | Read-only KPI dashboard |
| GET | `/auditor/sessions` | List sessions |
| POST | `/auditor/sessions` | Create session |
| GET | `/auditor/session/{id}` | Session detail |
| POST | `/auditor/observations` | Create observation |
| GET | `/auditor/traceability/{objectiveElementId}` | Evidence lineage timeline |
| POST | `/auditor/comments` | Create comment |
| GET | `/auditor/report/{sessionId}` | Generate audit report |
| GET | `/auditor/assessment-context` | Framework → OE navigation tree |

Findings continue via **`/api/v1/findings`** with optional `auditSessionId` on create.

---

## 6. Traceability

Evidence Trace View composes existing entities into a read-only timeline:

```
Objective Element → Obligations → Evidence → Verification
  → Deficiencies → CAPA → Current Readiness
```

Implemented in `AuditorTraceabilityService` — no duplicate data stores.

---

## 7. Audit Report Generation

`AuditorReportService` assembles:

- Audit Summary
- Strengths (from positive observations)
- Observations
- Findings (session-linked)
- Risk Summary
- Recommendations
- Evidence Gaps
- Readiness Summary (via `AnalyticsAggregationService`)

Output: structured DTO + exportable Markdown. Advisory only — no workflow mutations.

---

## 8. Analytics Integration

Dashboard KPIs delegate to existing services:

- **Readiness:** `AnalyticsAggregationService.GetReadinessAsync`
- **Evidence counts:** `ObjectiveElementEvidences` query
- **Finding density:** session-scoped `Finding` counts
- **Coverage:** OE review coverage percentage

No duplicate KPI calculations in the frontend.

---

## 9. Audit Lifecycle

```
Planned → In Progress → Review → Draft Report → Submitted → Closed
```

Status stored on `AuditSession.Status`. Report generation available at any post-Planned state; formal lifecycle transitions can be extended in future sprints.

---

## 10. Validation Checklist

| Criterion | Status |
|-----------|--------|
| Auditor can review evidence | ✓ Assessment workspace + traceability |
| Auditor can trace evidence lineage | ✓ Timeline API |
| Auditor can create findings | ✓ Existing Findings API + session link |
| Observations remain independent | ✓ Separate aggregate, no deficiency creation |
| Finding Promotion still works | ✓ Unchanged Sprint 7B path |
| No duplicate CAPA workflow | ✓ Read-only CAPA in trace |
| No duplicate task workflow | ✓ Task counts read-only |
| No duplicate evidence workflow | ✓ Reuses OE evidence links |
| Permissions enforced | ✓ Role + scope policies |
| Enterprise layout maintained | ✓ `/app/auditor` in existing shell |

---

## 11. Future: Consultant Support

The `AuditType.ConsultantReview` enum value and session model support future consultant personas without schema changes. Consultant users could share the same workspace with differentiated RBAC policies.

Legacy `AuditService` remains separate; Sprint 14 builds on **ComplianceService** as SSOT per architecture direction.

---

## 12. Migration

`20260625120000_Sprint14_ExternalAuditorWorkspace.cs`:

- Creates `AuditSessions`, `AuditObservations`, `AuditComments`
- Adds `Findings.AuditSessionId`

Run: `dotnet ef database update` on ComplianceService.

---

## 13. Success Criteria

✓ Independent auditor workspace at `/app/auditor`  
✓ No duplication of Journey, DCC, CAPA, or Evidence workflows  
✓ Existing Findings, Analytics, and report patterns reused  
✓ External auditor read-first experience  
✓ Hospital team remains responsible for remediation  
✓ Platform supports complete accreditation lifecycle
