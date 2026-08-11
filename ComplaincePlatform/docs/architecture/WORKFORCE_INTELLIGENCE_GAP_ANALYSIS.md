# PRAMAN Workforce Intelligence Gap Analysis — EIWAM Phase 10

| Field | Value |
|-------|-------|
| **Phase** | EIWAM Phase 10 — Workforce Intelligence |
| **Prior work** | Phases 0–9 EIWAM (Identity through AI Identity) |
| **Status** | Investigation complete — implementation authorized |
| **Bounded context** | WorkforceIntelligenceService (new greenfield) |

## Executive summary

PRAMAN has a **complete operational workforce foundation** (Person → Employment → Professional → Organization → Governance → Communication → AI) but **no analytical intelligence layer**. Operational data lives in bounded contexts; dashboards are compliance-centric only. Phase 10 introduces **WorkforceIntelligenceService** as the enterprise intelligence read-model layer — consuming source services via HTTP and hybrid event projections without duplicating operational tables.

**Do not embed intelligence in WorkforceService or ComplianceService. Create a dedicated intelligence bounded context.**

---

## Investigation scope

| Area | Locations reviewed |
|------|-------------------|
| Workforce | `WorkforceService/Controllers/V1/EmploymentController.cs`, `EmploymentAppService.cs` |
| Professional | `ProfessionalService/Controllers/V1/ProfessionalProfilesController.cs`, `ProfessionalProfileAppService.cs` |
| Organization | `OrganizationService/Controllers/V1/OrganizationPhase4Controllers.cs` |
| Governance | `GovernanceService/Controllers/V1/GovernanceControllers.cs` |
| Communication | `CommunicationService/Controllers/V1/CommunicationControllers.cs` (availability) |
| Person | `PersonService/Controllers/V1/PersonsController.cs` |
| AI | `AIOrchestratorService/Application/AI/Orchestration/AIOrchestrationService.cs` |
| Compliance analytics | `ComplianceService/Application/Analytics/AnalyticsQueryService.cs` |
| Event relay | `OutboxRelayWorker/OutboxRelayBackgroundService.cs` |
| Angular | `WebApp/src/app/features/workforce/`, `professional/`, `ai-identity/` |

---

## Already exists

| Capability | Implementation |
|------------|----------------|
| Employment lifecycle | `WorkforceService` — create, transfer, promote, org-chart |
| Professional credentials | Licenses, certifications, training, competencies, renewals API |
| Organization structure | Membership, departments, org tree, graph |
| Governance accountability | Responsibilities, committees, meetings, decisions, analytics summary |
| Person profiles | Demographics, contacts, documents, search |
| Communication availability | Person-scoped availability, working hours, vacation |
| AI execution runtime | Orchestrator + AI Identity profiles/policies |
| Compliance analytics | Journey scoring, maturity, executive dashboards (compliance-only) |
| Gateway proxy pattern | MediatR + `IProxyService` for all M6+ services |
| Outbox pattern | Per-service `OutboxMessage` tables |

---

## Reusable (extend, do not duplicate)

| Asset | Reuse pattern |
|-------|---------------|
| `CommunicationService` scaffold | Greenfield service template (Program, DI, migrations) |
| `AnalyticsQueryService` | KPI aggregation + scope resolution pattern |
| `OutboxRelayBackgroundService` | Extend for `workforce-events` topic relay |
| `ApiResponseFactory` | Standard envelope for all intelligence APIs |
| Gateway MediatR proxy | Phase 9 AI Identity handler pattern |
| `ComplianceEventConsumerHostedService` | Service Bus consumer pattern for intelligence events |
| Internal APIs | `internal/employment`, `internal/professional`, `internal/governance` |
| `PramanServiceCatalog` | Service registration + AOVIAS platform hosting |
| Angular `ai-identity` feature | Route structure, API service, permission guards |

---

## Needs enhancement

| Area | Current | Phase 10 target |
|------|---------|----------------|
| **Outbox relay** | Compliance-only → `compliance-events` | Relay Workforce/Professional/Governance → `workforce-events` |
| **Professional skills** | `ProfessionalSkill` entity exists, not in profile DTO | Expose in `ProfessionalProfileDto` for skill matrix |
| **Renewal dashboard** | Registrations + licenses only | Include certifications |
| **Headcount APIs** | Per-employment list only | Intelligence aggregates by dept/profession/type |
| **Availability** | Split: Workforce placeholder vs Communication engine | Intelligence consumes Communication availability |
| **Organization outbox** | Written, no processor | Add processor + relay eligibility |
| **Access permissions** | No `Intelligence.*` codes | Seed in AccessGovernance |

---

## Missing

| Capability | Phase 10 deliverable |
|------------|---------------------|
| `WorkforceIntelligenceService` | Dedicated intelligence bounded context |
| `WorkforceSnapshot` | Point-in-time headcount/distribution read model |
| `ExecutiveMetrics` | Rolled-up executive KPIs |
| Skill/competency/training matrices | Gap analysis, coverage, compliance % |
| Certification/license dashboards | Active/expiring/expired + forecast |
| Organization analytics | Span of control, vacancies, transfers |
| Responsibility/committee analytics | Coverage, gaps, attendance, action closure |
| Availability analytics | Working/leave/vacation coverage |
| `ForecastSnapshot` | Versioned retirement/attrition/hiring forecasts |
| `WorkforceRecommendation` | Explainable rule-based + AI suggestions |
| Enterprise workforce search | Cross-domain denormalized index |
| `/intelligence/*` APIs | 10+ read endpoints via Gateway |
| Angular intelligence dashboards | Executive + matrix + forecast UI |
| Intelligence outbox events | `WorkforceSnapshotUpdated`, `ForecastGenerated`, etc. |

---

## Technical debt

| Item | Risk | Phase 10 action |
|------|------|----------------|
| Log-only outbox processors | Events never reach analytics | Hybrid: HTTP snapshots + workforce-events relay |
| Single Service Bus topic | Workforce events isolated | Add `workforce-events` topic |
| Duplicate availability models | Inconsistent coverage data | Intelligence reads Communication only |
| No cross-service reconciliation | KPI drift undetected | Snapshot reconcile job with drift logging |
| Compliance analytics silo | No workforce overlay | Intelligence layer separate; future linkage in Phase 11 |

---

## Per-domain API inventory

### WorkforceService (`api/v1/employment`)
- List, search, org-chart, positions, transfer, promote, history, timeline
- Internal: `internal/employment/{id}`, `by-person/{personId}`
- Outbox: `EmploymentCreated`, `EmploymentTransferred`, `EmploymentPromoted`, etc.

### ProfessionalService (`api/v1/professional-profiles`)
- Profile CRUD, qualifications, licenses, certifications, competencies, training
- `GET /renewals` — expiring credentials
- Internal: `internal/professional/{id}`, `by-employment`, `by-person`, `expiring`
- Outbox: `ProfessionalProfileCreated`, `QualificationAdded`, `TrainingCompleted`, etc.

### OrganizationService
- Membership, invitations, org tree, department tree, org graph
- Outbox: `MembershipCreated`, `DepartmentCreated`, `OrganizationHierarchyChanged`

### GovernanceService
- Responsibilities, committees, meetings, decisions, `GET /governance/analytics/summary`
- Outbox: `ResponsibilityAssigned`, `CommitteeDecisionRecorded`, etc.

### CommunicationService
- `GET /availability` — person availability profile
- Outbox: `AvailabilityChanged`

### PersonService
- `GET /persons`, `/search`, profile detail
- Outbox: `PersonCreated`, `PersonUpdated`

---

## Authorization boundary

| Context | Owns | Must NOT own |
|---------|------|--------------|
| **WorkforceIntelligenceService** | Snapshots, KPIs, forecasts, recommendations, search index | Employment, credentials, org membership, governance assignments |
| **WorkforceService** | Employment operational data | Analytics aggregates |
| **ProfessionalService** | Credential operational data | Training compliance dashboards |
| **GovernanceService** | Responsibility/committee data | Executive workforce dashboards |

---

## Implementation authorization

Gap analysis complete. Proceed to `WORKFORCE_INTELLIGENCE_DOMAIN_MODEL.md` then `WorkforceIntelligenceService` implementation.
