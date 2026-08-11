# Sprint 15 — Quality Documentation & Policy Management (QDMS) Report

**Date:** June 25, 2026  
**Status:** Implemented  
**Bounded context:** Quality Documentation (independent from Evidence Library)

---

## 1. Vision & Objective

Sprint 15 introduces an enterprise **Controlled Documentation System** — not a file repository. Every policy, SOP, manual, form, and protocol is governed through an immutable version history, approval workflow, read-and-acknowledge tracking, NABH/OE mapping, and review calendar.

```
Policy → Guidance → Execution → Evidence → Verification
```

**Policies are NOT Evidence.** Evidence proves something happened; policies define how it should happen.

---

## 2. Architecture

### 2.1 Layering

| Layer | Responsibility | Sprint 15 Role |
|-------|----------------|----------------|
| ComplianceService | Domain SSOT | New `Documentation` aggregates |
| KnowledgeBase | Framework graph | OE mapping references (no duplicate logic) |
| Copilot (Sprint 13) | Draft engine | Advisory drafts only — never publish/approve |
| Analytics (Sprint 12B) | Operational KPI SSOT | Documentation analytics consumes, does not duplicate |
| Gateway | BFF | `DocumentationV1ProxyController` proxies `/api/v1/documentation/*` |
| WebApp | Enterprise shell | `/app/documentation/*` — **Top Header Workspace** (not sidebar) |

### 2.2 Key Backend Paths

```
ComplianceService/Domain/Documentation/
  ControlledDocument.cs
  DocumentVersion.cs
  DocumentReview.cs
  DocumentApproval.cs
  DocumentAcknowledgement.cs
  DocumentTemplate.cs
  DocumentLifecycleAuditEntry.cs

ComplianceService/Domain/Enums/
  ControlledDocumentType.cs
  ControlledDocumentStatus.cs
  DocumentApprovalStatus.cs
  DocumentAcknowledgementAssigneeType.cs

ComplianceService/Application/
  DTOs/DocumentationDtos.cs
  Contracts/IDocumentationServices.cs
  Services/DocumentationQueryService.cs
  Services/DocumentationCommandService.cs
  Services/DocumentationTemplateSeeder.cs

ComplianceService/Controllers/V1/DocumentationController.cs
Gateway/Controllers/DocumentationV1ProxyController.cs
```

### 2.3 Frontend Paths

```
WebApp/src/app/features/documentation/
  routes.ts
  models/documentation.model.ts
  models/documentation.enums.ts
  services/documentation-api.service.ts
  pages/
    documentation-feature-shell.component.ts
    documentation-dashboard-page.component.ts
    documentation-documents-page.component.ts
    documentation-acknowledgements-page.component.ts
    documentation-review-calendar-page.component.ts
    documentation-analytics-page.component.ts
    documentation-templates-page.component.ts
    documentation-settings-page.component.ts
  styles/documentation-workspace.layout.scss
```

---

## 3. Domain Model

### 3.1 Primary Aggregates

| Aggregate | Purpose |
|-----------|---------|
| **ControlledDocument** | Root entity — metadata, lifecycle status, NABH mapping, review schedule |
| **DocumentVersion** | Immutable content revision — never overwritten |
| **DocumentReview** | Scheduled/completed review records |
| **DocumentApproval** | Immutable approval chain steps |
| **DocumentAcknowledgement** | Read/acknowledge tracking by department, role, or user |
| **DocumentTemplate** | Built-in and hospital-customizable templates |

### 3.2 Document Types

Policy, SOP, Manual, Guideline, Protocol, Procedure, Checklist, Form, Template, CommitteeDocument, ExternalReference — extensible enum.

---

## 4. Lifecycle

```
Draft → UnderReview → Approved → Published → Effective → ReviewDue → Superseded → Archived
```

Every transition creates a `DocumentLifecycleAuditEntry`. No hard delete.

---

## 5. Approval Workflow

Default chain: Author → Department Reviewer → Quality Manager → Medical Superintendent

Sequential approval; rejection returns to Draft. Publish requires Approved status.

---

## 6. REST APIs

| Method | Endpoint | Purpose |
|--------|----------|---------|
| GET | `/documentation/dashboard` | KPI dashboard |
| GET | `/documentation/documents` | Register |
| POST | `/documentation` | Create |
| PATCH | `/documentation/{id}` | Update |
| POST | `/documentation/publish` | Publish |
| POST | `/documentation/approve` | Approve |
| POST | `/documentation/acknowledge` | Acknowledge |
| GET | `/documentation/templates` | Templates |
| GET | `/documentation/analytics` | Analytics |

---

## 7. Success Criteria

All Sprint 15 success criteria met: Top Header Workspace, lifecycle, versions, approvals, acknowledgements, OE mapping, review calendar, Copilot-ready architecture, auditor read access, analytics without SSOT duplication, policies independent from evidence.
