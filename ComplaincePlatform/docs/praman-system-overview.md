## PRAMAN Healthcare Compliance Platform – System Overview (2026-03-11)

## Constitution Reference

This document is subordinate to the platform-wide architecture law:

- `V:\DevelopmentRepo\V1.0\docs\PRAMAN-Architecture-Constitution.md`

If guidance here conflicts with the constitution, the constitution takes precedence.

### 1. System Overview

- **Architecture**: Multi-service .NET 10 platform with a Gateway/BFF fronting domain services, following strict lifecycle and outbox/event rules.
- **Core services**:
  - **Gateway** – Unified API surface, MediatR-style `Application` layer, named `HttpClient` integrations to all backend services.
  - **ComplianceService** – Core compliance domain: frameworks, regulations, programs, journeys, tasks, evidences, scoring, snapshots, lifecycle management.
  - **OrganizationService** – Organizations, members, hospitals, departments, services, platform sequences.
  - **AuditService** – Audit sessions, findings, corrective actions, audit reports.
  - **AIOrchestratorService** – AI workflows and draft generation, orchestrating calls into Compliance and Audit.
  - **IdentityService** – Authentication and identity (ASP.NET Identity + Razor UI, token + auth APIs).
  - **Shared.Infrastructure** – Shared domain primitives, lifecycle state machine, DTOs, middleware, auth helpers, reasoning capture, EF helpers.

---

### 2. Backend Architecture

- **Solution projects**
  - `ComplianceService`, `Gateway`, `OrganizationService`, `AuditService`, `AIOrchestratorService`, `IdentityService`, `Compliance.MasterData`, `Shared.Infrastructure`.
- **Gateway**
  - Thin controllers → commands/queries/handlers/validators under `Application`.
  - Named `HttpClient`s via `AddBackendHttpClients` for: Compliance, Identity, Organization, AI, Audit, etc.
  - Cross-cutting: idempotency middleware, organization access middleware, authorization handlers.
- **ComplianceService**
  - Layers: `Application` (services, DTOs, contracts, events, background jobs), `Domain` (aggregates, policies, events, scoring, snapshots), `Infrastructure` (DbContext, repositories, filters, integration publisher).
  - Enforces lifecycle via `ComplianceLifecycleStateMachine` and journey policies; uses outbox + domain events.
- **OrganizationService / AuditService / IdentityService / AIOrchestratorService**
  - Each has its own `Application`, `Domain`, `Infrastructure` and EF `DbContext` with migrations.
  - Identity hosts Razor Pages UI; AI orchestrator owns workflow engine and AI client abstractions.

---

### 3. Frontend Architecture

- **Identity Razor UI**
  - ASP.NET Core Identity pages in `IdentityService/Areas/Identity/Pages` and `IdentityService/Pages` provide authentication and account management UI.
- **Angular Operator Portal (SPA)**
  - Angular application exists under `V:\DevelopmentRepo\V1.0\WebApp`.
  - Frontend architecture policy is defined in `V:\DevelopmentRepo\V1.0\WebApp\docs\architecture\PRAMAN_ANGULAR_ARCHITECTURE_RULES.md`.
  - Design/token guidance is maintained in `V:\DevelopmentRepo\V1.0\WebApp\DESIGN_RULES.md`.
  - Gateway remains the primary API boundary for frontend integration.

---

### 4. Database Entities

- **ComplianceService – `ComplianceDbContext`**
  - Key sets: `ComplianceFrameworks`, `ComplianceChapters`, `ComplianceClauses`, `ComplianceRequirements`, `Regulations`, `CompliancePrograms`, `Controls`, `ComplianceTasks`, `Evidences`, `ComplianceSnapshots`, `FrameworkChapterSnapshots`, `OrganizationComplianceOverviewSnapshots`, `Risks`, `PlatformSequences`, `OutboxMessages`, `ComplianceJourneys`, `ComplianceJourneyHistories`, `FrameworkConfigurations`, `ClauseEvidences`, `ComplianceAuditFindings`.
- **OrganizationService – `OrganizationDbContext`**
  - `Organizations`, `OrganizationMembers`, `Hospitals`, `Departments`, `Services`, `PlatformSequences`.
- **AuditService – `AuditDbContext`**
  - `AuditSessions`, `AuditFindings`, `CorrectiveActions`, `AuditReports`, `PlatformSequences`.
- **AIOrchestratorService – `AIOrchestratorDbContext`**
  - `DraftArtifacts`.
- **IdentityService – `IdentityDbContext`**
  - ASP.NET Identity tables + `PlatformSequences`.
- **Shared.Infrastructure domain**
  - Shared entities: `Organization`, `OrganizationMember`, `Hospital`, `Department`, `Service`, `PlatformSequence`, `PlatformEntity`, `BaseEntity`, lifecycle enums and state machine.

---

### 5. API Endpoints (High-Level)

- **Gateway controllers**
  - `CommonController`, `DashboardController`, `FrameworkController`, `IdentityController`, `JourneysController`, `OrganizationsRegulatoryController`, `TenantController`, `AIController`.
- **ComplianceService controllers**
  - `JourneysController`, `SnapshotsController`, `EvidenceController`, `FrameworkController`, `RegulationsController`, `TasksController`, `ProgramsController`, `ControlsController`, plus `InternalJourneysController` and nested controllers for audit/compliance intelligence/accreditations.
- **Other service controllers**
  - OrganizationService: `OrganizationsController`, `CommonController`, `InternalOrganizationController`.
  - AuditService: `AuditController`.
  - AIOrchestratorService: `DraftsController`, `AiController` (plus `BaseController`).
  - IdentityService: `AuthController`, `TokenController`, `InternalUserController`, Identity Razor endpoints.

---

### 6. Domain Modules

- **ComplianceService domain**
  - Aggregates/entities: `ComplianceJourney` (aggregate root), `HospitalComplianceAggregate`, `ComplianceProgram`, `ComplianceFramework`, `ComplianceChapter`, `ComplianceClause`, `ComplianceRequirement`, `Control`, `ComplianceTask`, `Evidence`, `ClauseEvidence`, snapshot entities, `Risk`/`RiskSnapshot`, gaps/gap analysis types, `ComplianceAuditFinding`, `FrameworkConfiguration`, `Regulation`.
  - Lifecycle & policies: `ComplianceLifecycleStateMachine`, policies (`DefaultJourneyPolicy`, `NabhJourneyPolicy`, `NablJourneyPolicy`, `FireNocJourneyPolicy`, `PollutionJourneyPolicy`).
  - Domain events: enrollment, submission, approval, rejection, suspension, expiry, renewal, lifecycle change, framework enrolled, audit completed, compliance updated, compliance score updated.
  - Scoring & context: `ComplianceScoringContext`, `ComplianceScoreResult`, `ChapterScoreResult`, `AuditSimulationResult`, `IComplianceScoringService`.
  - Copilot/AI explanation: `CopilotExplanationResult`, `CopilotIntent`, `CopilotQuestionType`, `CopilotResponse`, `CopilotSafetyResult`, `ExplanationInput`, `TargetEntityType`.
- **Shared.Infrastructure domain**
  - Shared entities and enums, especially `ComplianceLifecycleState` and `ComplianceLifecycleStateMachine`, plus `NonConformanceType`.
- **OrganizationService / AuditService / AIOrchestratorService / IdentityService**
  - Each has its own local domain (organizations, audits, drafts/workflows, app users) built on top of shared primitives.

---

### 7. Infrastructure & Integrations

- **Persistence and repositories**
  - Per-service EF `DbContext` and migrations; repositories/UoW per service; common EF helpers in `Shared.Infrastructure`.
- **Outbox and messaging**
  - `OutboxMessage` in `ComplianceDbContext`, outbox processors (`OutboxDomainEventDispatcher`, `OutboxProcessor`, `InProcessOutboxProcessor`), domain event dispatcher.
  - `IntegrationEventPublisher` abstraction exists; no concrete RabbitMQ adapter in this repo yet.
- **HTTP integration**
  - Gateway: named `HttpClient`s (`ComplianceService`, `IdentityService`, `IdentityClient`, `OrganizationClient`, `AIService`, `AuditService`).
  - AIOrchestrator: `ComplianceClient` and `AuditClient` calling backend APIs via `"InternalClient"`.
- **AI / LLM**
  - `ILLMAdapter` + `DefaultLLMAdapter` (placeholder) in AIOrchestrator; `ILLMExplanationAdapter`, `LLMExplanationAdapter`, `CopilotExplanationService` in ComplianceService.
  - `ReasoningCapture` service in Shared.Infrastructure.
- **Cross-cutting**
  - Shared middleware (global exception, validation), auth helpers and policies, email abstraction, platform id generation, metrics-ready logging patterns (foundation present, full observability stack not yet wired).

---

### 8. Project Status

#### 8.1 Completed features

- Multi-service backend with clear boundaries, per-service DbContexts, migrations, and Gateway as the single external API surface.
- Rich compliance domain with journeys, frameworks, programs, tasks, evidences, snapshots, scoring, lifecycle rules, outbox, and background jobs.
- Organization, Audit, AI orchestrator, and Identity services implemented with their own domains, application services, and persistence layers.
- Shared infrastructure for lifecycle, shared entities, DTOs, middleware, auth utilities, and reasoning capture.

#### 8.2 Partially implemented features

- **Compliance Copilot / AI explanations**
  - Domain types and services are in place, but rely on placeholder LLM adapters; no real AI provider wired yet.
- **Compliance audit ↔ journey integration**
  - Audit and compliance domains are connected conceptually and via some services, but the end-to-end loop (journey → audits → findings → tasks/snapshots) is not fully closed or hardened.
- **AI workflows**
  - Workflow engine and at least one workflow exist; additional workflows and richer branching still to be added.
- **Dashboards and analytics**
  - Snapshot projector and dashboard endpoints exist, but not all required dashboard slices/filters appear complete.

#### 8.3 Missing critical systems

- **Production LLM integration**
  - No concrete Azure OpenAI/OpenAI/Claude client; `DefaultLLMAdapter` is a placeholder.
- **Message bus adapter and workers**
  - Outbox is implemented, but there is no visible RabbitMQ (or equivalent) publisher/consumer implementation.
- **Unified operator-facing frontend**
  - No main SPA/UI project; operators would currently use API tools (e.g., Postman) against Gateway.
- **Centralized observability**
  - Logging and error handling exist, but no full metrics/tracing stack is wired across services.

---

### 9. Architectural Risks

- **AI behavior gap**
  - Architecture assumes powerful, explainable AI features, but placeholder adapters mean production behavior will lag design until real providers are integrated and validated.
- **Event-driven architecture not fully realized**
  - Without a concrete message bus adapter and workers, cross-service communication risks remaining HTTP-centric, reducing decoupling and resilience.
- **Missing primary frontend**
  - Lack of a canonical UI risks fragmented operator flows, inconsistent enforcement of auth/roles at the UX level, and limited user validation of lifecycle design.
- **Authorization consistency**
  - Foundational auth components exist, but there is ongoing risk that new application service methods may miss consistent role/tenant checks.

---

### 10. Recommended Next Milestone

**Milestone: “Productionize AI and messaging, ship MVP operator portal”**

- **P0 Critical**
  - Implement a real LLM provider behind `ILLMAdapter` and `ILLMExplanationAdapter` (e.g., Azure OpenAI), with configuration, secrets, safety/guardrails, and at least one E2E AI feature (explanations or drafting) in production quality.
  - Implement an outbox worker that publishes `OutboxMessage` records to RabbitMQ (or chosen broker), plus at least one consumer in another service to validate end-to-end event flow.
  - Complete the compliance journey ↔ audit loop so that lifecycle transitions, audits, findings, tasks, risks, and snapshots form a coherent, test-backed flow.
- **P1 Important**
  - Stand up an MVP SPA frontend that authenticates via Identity/Gateway and surfaces core operator workflows (journey dashboard, program/framework overview, tasks and evidence, basic AI Copilot panel).
  - Harden dashboards and snapshot APIs into a minimal but trustworthy overview, and tighten authorization enforcement in application services for high-risk operations.

