# 01 — Platform Inventory

**Program:** Commercial Readiness Gap Analysis (CRP v1.0)  
**Product:** AOVIAS Command (PRAMAN / CompliancePlatform)  
**Assessment Date:** 26 July 2026  
**Scope:** What exists today — no assumed future work  
**Companion frontend:** `V:\DevelopmentRepo\V1.0\WebApp` (Angular `praman-web`) — **not inside this repo**

---

## 1. Executive Map

| Layer | Reality |
|-------|---------|
| Experience | Angular 21 SPA (`praman-web`) → Gateway `/api/v1` |
| Edge | `Gateway` BFF (MediatR + HTTP proxy; limited YARP) |
| Domain | 15+ microservices, each with own SQL Server database |
| Intelligence | `AIOrchestratorService`, `WorkforceIntelligenceService`, `EnterpriseDirectoryService` |
| Integration | `IntegrationService` + `OutboxRelayWorker` → **Azure Service Bus** |
| Identity | Duende IdentityServer + ASP.NET Identity + JWT |
| Data seed | `seed-data/` (NABH 6E) + `DemoDataSeeder` + in-service bootstraps |

**Verdict:** Engineering platform is broad and feature-dense. Commercial packaging (install, license, DR, full-stack deploy) is incomplete.

---

## 2. Solution Entry Points

| Artifact | Path |
|----------|------|
| Solution | `CompliancePlatform.slnx` (TFM **net10.0**) |
| Dev compose | `docker-compose.dev.yml` |
| Dev Dockerfile | `docker/Dockerfile.dev.dotnet` |
| Package versions | `Directory.Packages.props` |
| Postman | `CompliancePlatform.postman_collection.json`, `postman/` |
| Seed | `seed-data/` |
| Demo seed | `DemoDataSeeder/` |
| Planning | `.planning/`, `planning/` |
| Architecture docs | `docs/architecture/`, `docs/verification/` |

---

## 3. Runnable Backend Projects

### 3.1 Domain / platform services

| Service | Port (HTTP/HTTPS) | Database | Primary responsibility |
|---------|-------------------|----------|------------------------|
| Gateway | 5212 / 7216 | None | BFF, MediatR, auth edge, SignalR hub shell |
| IdentityService | 5062 / 7135 | Identity | Users, MFA, JWT, Duende IS |
| ComplianceService | 5016 / 7087 | Compliance | Journey, OE, evidence, CAPA/deficiency, QDMS, incidents, quality, auditor, copilot, Hangfire |
| OrganizationService | 5085 / 7181 | Organization | Orgs, hospitals, depts, memberships, subscriptions |
| KnowledgeBase | 5120 / 7045 | KnowledgeBase | Frameworks, nodes, assessments, gap analysis |
| AIOrchestratorService | 5088 / 7097 | AIOrchestrator | AI drafts/assist, feature gates, LLM adapters |
| AuditService | 5038 / 7006 | Audit | Legacy audit sessions/findings (**parallel** to Compliance auditor) |
| PersonService | 5190 / 7190 | Person | Person profiles |
| WorkforceService | 5195 / 7195 | Workforce | Employment, positions |
| ProfessionalService | 5200 / 7200 | Professional | Credentials, skills |
| AccessGovernanceService | 5205 / 7205 | AccessGovernance | Roles, permissions, policy evaluate |
| GovernanceService | 5210 / 7210 | Governance | Committees, approvals, escalations |
| CommunicationService | 5215 / 7215 | PramanCommunication | Notifications, templates, reminders |
| EnterpriseDirectoryService | 5217 / 7217 | PramanEnterpriseDirectory | Directory search/index |
| IntegrationService | 5218 / 7218 | PramanIntegration | Connectors, sync, webhooks, import/export |
| WorkforceIntelligenceService | 5216 / 7216 (meta) | PramanWorkforceIntelligence | Skills/training matrices, forecasts |

### 3.2 Workers / tools

| Project | Purpose |
|---------|---------|
| OutboxRelayWorker | Outbox → Azure Service Bus (Compliance, Workforce, Directory, Integration) |
| DemoDataSeeder | Console demo enrichment (`dry-run` / `seed` / `verify`) |
| D0RuntimeVerifier | AI runtime verifier |

### 3.3 Shared libraries

| Library | Purpose |
|---------|---------|
| Shared.Infrastructure | API response, auth constants, middleware, domain base |
| Shared.Logging | Serilog, correlation, health/platform observability |
| Shared.Authorization | Permission attributes + AccessGovernance client |
| Shared.Communication | Communication contracts/client |
| Shared.Governance | Governance contracts/client |
| Compliance.MasterData | File-based master data + NABH entry-level seeder |
| Platform.Dashboard | Dashboard provider contracts (library, not host) |

### 3.4 Empty / probe

| Path | Status |
|------|--------|
| `CompliancePlatform.Knowledgebase.Application/` | Directory present; **no source** |
| `_tmp_phase_probe/`, `_build_verify_*`, `_perf_build_out/` | Build/probe artifacts |
| `angular-examples/` | Empty / non-product |
| `AuditService` WeatherForecast controller | Scaffold leftover |

### 3.5 Test projects

`*.Tests` for most services + `tests/Gateway.UnitTests`, `tests/Shared.Infrastructure.Tests`, `tests/ComplianceStub`.

---

## 4. Frontend Application

| Item | Value |
|------|-------|
| Repo | `V:\DevelopmentRepo\V1.0\WebApp` |
| Project | `praman-web` |
| Display name | **AOVIAS Command** |
| Stack | Angular 21, Material 21, Tailwind 4, Lucide |
| Dev | `ng serve` :4200, proxy `/api` → Gateway :5212 |
| Structure | Standalone + lazy routes under `src/app/features/` (~35 feature areas) |

**There is no Angular app inside ComplaincePlatform.** Frontend inventory depends on the sibling WebApp repo.

---

## 5. Gateway / API Surface

### 5.1 Routing pattern

- **Primary:** Controllers + MediatR handlers + named `HttpClient` proxies (`IProxyService`)
- **Secondary YARP** (narrow):
  - `/api/identity/{**}` → Identity
  - `/api/compliance/{**}` → Compliance
  - `/api/ai/{**}` → AI Orchestrator

### 5.2 Major Gateway controller families

Onboarding, Identity/Users/Security, Organizations/Hospitals/Members/Settings/Subscriptions/Tenant, Journeys/Obligations, Framework, Evidence Library/Binder, Compliance Tasks/Assignment/Deficiencies/SLA, Dashboard/Analytics/Reporting/Maturity/Scoring, Copilot/AI, KnowledgeBase, Persons/Employment/Professional, Enterprise Directory, Workforce Intelligence, Integration, Audit Logs/Findings, plus many `*V1ProxyController`s (Auditor, Graph, Control Center, Documentation, Findings, Incidents, Quality, Tasks).

### 5.3 CQRS

- Gateway: MediatR-first (~36 Commands, ~67 Queries, ~80 Handlers)
- Newer domain services: local MediatR
- ComplianceService: application-service style + Hangfire (not MediatR-first)

---

## 6. Authentication & Authorization

| Mechanism | Status |
|-----------|--------|
| ASP.NET Identity (`AppUser`) | Implemented |
| Duende IdentityServer | Implemented |
| JWT Bearer (issuer/audience/secret) | Implemented across services |
| Token revocation validation | Implemented (Identity) |
| MFA (login challenge) | Implemented |
| Account lockout | Implemented (Identity defaults) |
| Internal API key (`InternalApi:ApiKey`) | Implemented; **dev key committed** |
| Permission evaluation (AccessGovernance) | Implemented; UI uses `permissionGuard` |
| Role route guard (`roleGuard`) | **Defined in WebApp, unwired** |
| OrgAccess / tenant isolation | **Partial** — not systemic on Gateway |

---

## 7. Databases & Migrations

- Provider: **SQL Server** (`UseSqlServer`)
- Pattern: one database per service
- Local defaults: `localhost\SQLEXPRESS` or `(localdb)\mssqllocaldb` (`Praman*` names for newer services)
- Docker: `Server=sqlserver-dev;User Id=sa;Password=StrongPassword@123`
- Migrations present for all primary services listed above

---

## 8. Messaging & Background Processing

| Component | Reality |
|-----------|---------|
| Outbox pattern | Multiple services + OutboxRelayWorker |
| Transport | **Azure Service Bus required** (worker throws if connection missing) |
| RabbitMQ | Leftover config/docs only — **not the active path** |
| Hangfire | ComplianceService (`/hangfire`) for snapshots/jobs |
| In-service hosted services | Outbox processors, email outbound, reminder scheduler, sync/webhook retry, index rebuild, AI outbox, etc. |

---

## 9. Seed Data & Feature Flags

### 9.1 Seed data (`seed-data/`)

- CSV sources: frameworks, chapters, standards, objective elements
- SQL: NABH Hospital 6E knowledge graph (~10 chapters / ~100 standards / ~503 OEs)
- Wizard/onboarding questionnaires, catalog sync, OE classification/maturity sync
- Generator: `tools/Generate-NabhSeed.ps1`

### 9.2 DemoDataSeeder

- Prerequisites: existing user `ekta.gupta@gmail.com` + fixed org/hospital GUIDs
- Seeds departments, ~72 staff, committees, journey + OE assessments, tasks/evidence, findings/CAPAs, documents, incidents, clinical indicators
- Modes: dry-run / seed / verify — **enrichment, not greenfield provisioning**

### 9.3 Feature flags

No LaunchDarkly / FeatureManagement library.

**AIFeatureFlags** (Gateway + AIOrchestrator):  
`EnableAIRead`, `EnableAIAssist`, `EnableAIAction`, `EnableAIProactive`, `EnableAutonomousWorkflows`, `EnableEventDrivenAutonomy`, `EnableAutoExecution`

Also: `OnboardingWizard:UseEmailInvites`, `PerformanceObservability:DashboardTimingEnabled`.

---

## 10. Deployment & Infrastructure

| Item | Status |
|------|--------|
| `docker-compose.dev.yml` | **Dev only** — SQL + Identity, Compliance, Audit, Organization, AI (~6 services) |
| Missing from compose | Gateway, KnowledgeBase, Person, Workforce, Professional, AccessGovernance, Governance, Communication, Directory, Integration, WorkforceIntelligence, OutboxRelayWorker |
| `deployment.metadata.json` | Present on services (ports/health/demo bundle flags) |
| Production install runbook | **Not found** in this repo |
| Backup / restore / upgrade product ops | **Not found** |
| Key Vault | Deferred in Phase 12.1 readiness notes |
| Secrets in appsettings | JWT secret, internal API key, AI provider keys present in source |

---

## 11. Documentation Landscape

| Area | Location |
|------|----------|
| Architecture / EIWAM | `docs/architecture/`, `docs/architecture/eiwam/` |
| Gap analyses (historical) | `docs/architecture/*GAP_ANALYSIS.md` |
| Verification | `docs/verification/phase-12/`, `phase-12.1/` |
| Performance | `docs/performance/` |
| Sprint reports | `docs/Sprint*.md` |
| Parent QA inventory | `V:\DevelopmentRepo\V1.0\docs\PRAMAN-QA-Module-Inventory.md` |
| Planning phases | `.planning/phases/` (incl. SaaS subscription, launch readiness) |
| Root status reports | `COMPREHENSIVE_STATUS_REPORT.md`, `ARCHITECTURAL_STATUS_REPORT.md` (**dated / partially stale**) |

---

## 12. Known Incomplete / Stub Inventory (Code Evidence)

| Finding | Location |
|---------|----------|
| Placeholder LLM adapter | `AIOrchestratorService/Infrastructure/LLM/DefaultLLMAdapter.cs` |
| Placeholder explanation adapter | `ComplianceService/.../LLMExplanationAdapter.cs` |
| `NotImplementedException` draft generator | `LlmDraftContentGenerator.cs` |
| Journey policies placeholders | `PollutionJourneyPolicy`, `NablJourneyPolicy`, `FireNocJourneyPolicy`, `DefaultJourneyPolicy` |
| `SurveillanceDue` stubbed `false` | `ComplianceJourneyService.cs` |
| Console-only email | `ConsoleEmailService` registered in CommunicationService |
| SignalR delivery unwired | `Gateway/Hubs/NotificationHub.cs` |
| Orphan `Capa` table (no write path) | Compliance domain `Capa` vs deficiency-embedded CAPA |
| Dual audit surfaces | `AuditService` vs Compliance `AuditorController` |
| Swagger UI disabled | Multiple `Program.cs` (.NET 10 Swashbuckle gap) |
| FE Coming soon | Surveillance nav, CCC ×7, person merge, compliance defaults, AI model actions, bulk user import/export |

---

## 13. Inventory Completeness Statement

| Dimension | Inventory complete? |
|-----------|---------------------|
| Backend projects | Yes |
| Shared libraries | Yes |
| Frontend (sibling) | Yes (discovered) |
| Databases / migrations | Yes |
| Seed / demo | Yes |
| Docker / deploy metadata | Yes |
| Commercial ops packaging | **Absent as product artifacts** |

This inventory is the factual baseline for documents `02`–`10`.
