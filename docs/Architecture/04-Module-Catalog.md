# Module Catalog

[← README](./README.md) · [Readiness Matrix](./05-Module-Readiness-Matrix.md)

Index of platform modules with routes, services, and specification links.

---

## Operator Portal Modules (Angular)

| Module | Route | Primary component | Backend via Gateway |
|--------|-------|-------------------|---------------------|
| Dashboard | `/app/dashboard` | `DashboardComponent` | `/dashboard`, `/compliance/snapshots` |
| Analytics Hub | `/app/analytics` | `AnalyticsHubComponent` | `/analytics` |
| Compliance Maturity | `/app/compliance-maturity` | `ComplianceMaturityComponent` | `/compliance/maturity-dashboard` |
| Compliance Copilot | `/app/copilot` | `CopilotWorkspaceComponent` | `/copilot/*` |
| Journey | `/app/journey/*` | `JourneyFeatureShellComponent` | `/journeys`, `/journey` |
| Evidence | `/app/evidence` | `EvidenceComponent` | `/evidence` |
| Review Queue | `/app/evidence/review-queue` | `EvidenceReviewQueueComponent` | `/evidence` (review-queue) |
| Evidence Binder | `/app/evidence/binder` | `EvidenceBinderPageComponent` | `/evidence-binder` |
| Tasks | `/app/tasks` | `TaskPageComponent` | `/tasks` |
| Findings | `/app/audit/findings` | `AuditFindingsPageComponent` | `/findings` |
| Corrective Actions | `/app/audit/corrective-actions` | `CorrectiveActionsComponent` | CAPA/deficiency APIs |
| Control Center | `/app/control-center/*` | `ComplianceCommandCenterComponent` | `/control-center` |
| Reporting | `/app/reports/*` | Various | `/reports` |
| Onboarding | `/onboarding` | `AiSetupAssistantComponent` | `/onboarding` |
| Gap Analysis | `/app/gap-analysis/:id` | `GapAnalysisResultsComponent` | KnowledgeBase assessments |
| CCC | `/app/ccc/*` | `CCCComponent` | Mixed compliance APIs |

---

## Backend Microservices

| Service | Project path | Database |
|---------|--------------|----------|
| Gateway | `ComplaincePlatform/Gateway` | — |
| ComplianceService | `ComplaincePlatform/ComplianceService` | `Compliance` |
| OrganizationService | `ComplaincePlatform/OrganizationService` | `Organization` |
| IdentityService | `ComplaincePlatform/IdentityService` | `Identity` |
| AuditService | `ComplaincePlatform/AuditService` | `Audit` |
| AIOrchestratorService | `ComplaincePlatform/AIOrchestratorService` | AI DB |
| KnowledgeBase | `ComplaincePlatform/KnowledgeBase` | `KnowledgeBase` |
| OutboxRelayWorker | `ComplaincePlatform/OutboxRelayWorker` | reads Compliance outbox |

---

## Module Specifications

| Spec | Module |
|------|--------|
| [journey.md](./module-specifications/journey.md) | Journey |
| [evidence.md](./module-specifications/evidence.md) | Evidence |
| [findings-deficiencies.md](./module-specifications/findings-deficiencies.md) | Findings & DCC |
| [copilot.md](./module-specifications/copilot.md) | Compliance Copilot |
| [analytics.md](./module-specifications/analytics.md) | Analytics Hub |

---

## Shared Libraries

| Library | Role |
|---------|------|
| `Shared.Infrastructure` | Auth constants, API response envelope, lifecycle primitives |
| `Shared.Logging` | Structured logging |
| `Compliance.MasterData` | Master data definitions |
