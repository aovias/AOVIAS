# PRAMAN Workforce Intelligence Domain Model — EIWAM Phase 10

| Field | Value |
|-------|-------|
| **Phase** | EIWAM Phase 10 — Workforce Intelligence |
| **Prior milestone** | Phases 0–9 EIWAM |
| **Status** | Approved — greenfield WorkforceIntelligenceService |
| **Bounded context** | WorkforceIntelligenceService (port 7216) |

## Purpose

Workforce Intelligence is PRAMAN's **enterprise analytical layer** over the workforce platform. It answers management questions — headcount trends, skill gaps, training compliance, credential risk, succession planning — without owning operational data.

```
Source Events / HTTP APIs → ProjectionEngine / SnapshotScheduler
                         → Read Models (snapshots, matrices, forecasts)
                         → IntelligenceQueryService → Gateway → Angular dashboards
                         → RecommendationOrchestrator → AI (explainable)
```

**Read, never own.**

---

## Spec name mapping

| EIWAM Phase 10 spec | Implementation |
|---------------------|----------------|
| WorkforceDashboard | `ExecutiveMetrics` + `AnalyticsCache` |
| WorkforceSnapshot | `WorkforceSnapshot` |
| SkillMatrix | `SkillMatrixEntry` |
| CompetencyMatrix | `CompetencyMatrixEntry` |
| TrainingMatrix | `TrainingMatrixEntry` |
| CertificationMatrix | `CertificationMatrixEntry` |
| LicenseMatrix | `LicenseMatrixEntry` |
| OrganizationAnalytics | `OrganizationAnalyticsSnapshot` |
| ResponsibilityMatrix | `ResponsibilityMatrixEntry` |
| CommitteeAnalytics | `CommitteeAnalyticsSnapshot` |
| AvailabilityAnalytics | `AvailabilityAnalyticsSnapshot` |
| WorkforceRecommendation | `WorkforceRecommendation` |
| WorkforceForecast | `ForecastSnapshot` |
| ExecutiveMetrics | `ExecutiveMetrics` |

---

## Bounded context boundaries

| Context | Owns | Must NOT own |
|---------|------|--------------|
| **WorkforceIntelligenceService** | Read models, KPIs, forecasts, recommendations, search index | Person, Employment, License, Committee, Availability |
| **WorkforceService** | Employment lifecycle | Headcount aggregates |
| **ProfessionalService** | Credentials | Training compliance % |
| **GovernanceService** | Assignments | Responsibility coverage analytics |
| **CommunicationService** | Availability profiles | Workforce coverage dashboards |
| **AIOrchestratorService** | LLM execution | Recommendation persistence |

---

## Core read models

### WorkforceSnapshot

Point-in-time workforce composition.

| Field | Type | Notes |
|-------|------|-------|
| Id | Guid | PK |
| OrganizationId | Guid | Scope |
| HospitalId | Guid? | Optional filter |
| AsOfUtc | DateTime | Snapshot timestamp |
| Version | int | Incremental version |
| CorrelationId | string? | Trace |
| TotalHeadcount | int | Active employments |
| TotalFte | decimal | Sum FTE |
| ByDepartmentJson | string | `{ deptId: count }` |
| ByProfessionJson | string | Category distribution |
| ByEmploymentTypeJson | string | Full-time/part-time/contract |
| AgeDistributionJson | string | Buckets from Person |
| ExperienceDistributionJson | string | Years of experience buckets |
| QualificationDistributionJson | string | Education levels |
| GrowthRate | decimal? | Period-over-period |
| AttritionRate | decimal? | Terminations / avg headcount |
| RetentionRate | decimal? | 1 - attrition |

### ExecutiveMetrics

Rolled-up dashboard KPIs.

| Field | Type |
|-------|------|
| Id | Guid |
| OrganizationId | Guid |
| AsOfUtc | DateTime |
| Headcount | int |
| OpenVacancies | int |
| TrainingCompliancePercent | decimal |
| CredentialRiskCount | int |
| CriticalSkillGaps | int |
| CommitteeActionBacklog | int |
| AvailabilityCoveragePercent | decimal |
| SnapshotId | Guid (FK) |

### SkillMatrixEntry / CompetencyMatrixEntry

| Field | Type |
|-------|------|
| OrganizationId | Guid |
| DepartmentId | Guid? |
| PersonId | Guid |
| EmploymentId | Guid |
| SkillOrCompetencyName | string |
| CurrentLevel | string |
| TargetLevel | string? |
| Gap | string? |
| IsCritical | bool |
| CoverageScore | decimal? |
| AsOfUtc | DateTime |

### TrainingMatrixEntry

| Field | Type |
|-------|------|
| OrganizationId | Guid |
| DepartmentId | Guid? |
| CourseName | string |
| IsMandatory | bool |
| Status | string | Completed/Pending/Expired/Upcoming |
| CompliancePercent | decimal |
| DueDate | DateTime? |
| AsOfUtc | DateTime |

### CertificationMatrixEntry / LicenseMatrixEntry

| Field | Type |
|-------|------|
| OrganizationId | Guid |
| PersonId | Guid |
| CredentialName | string |
| Status | string | Active/Expiring/Expired/RenewalRequired |
| ExpiryDate | DateTime? |
| DaysUntilExpiry | int? |
| DepartmentId | Guid? |
| ProfessionalCategory | string? |
| AsOfUtc | DateTime |

### OrganizationAnalyticsSnapshot

| Field | Type |
|-------|------|
| OrganizationId | Guid |
| DepartmentSizeJson | string |
| ReportingDepth | int |
| LeadershipDistributionJson | string |
| SpanOfControlAvg | decimal |
| CrossFunctionalAssignments | int |
| Vacancies | int |
| TransfersInPeriod | int |
| PromotionsInPeriod | int |
| AsOfUtc | DateTime |

### ResponsibilityMatrixEntry

| Field | Type |
|-------|------|
| ResponsibilityId | Guid |
| OwnerId | Guid? |
| ReviewerId | Guid? |
| ApproverId | Guid? |
| CommitteeId | Guid? |
| CoverageStatus | string | Covered/Gap/Duplicate |
| EscalationChainId | Guid? |
| AsOfUtc | DateTime |

### CommitteeAnalyticsSnapshot

| Field | Type |
|-------|------|
| CommitteeId | Guid |
| CommitteeName | string |
| MemberCount | int |
| AttendanceRate | decimal |
| MeetingFrequencyPerMonth | decimal |
| DecisionCompletionRate | decimal |
| ActionClosureRate | decimal |
| MemberParticipationJson | string |
| AsOfUtc | DateTime |

### AvailabilityAnalyticsSnapshot

| Field | Type |
|-------|------|
| OrganizationId | Guid |
| WorkingCount | int |
| OnLeaveCount | int |
| VacationCount | int |
| UnavailableCount | int |
| InTrainingCount | int |
| InMeetingCount | int |
| CoveragePercent | decimal |
| AsOfUtc | DateTime |

### ForecastSnapshot

| Field | Type |
|-------|------|
| Id | Guid |
| OrganizationId | Guid |
| ForecastType | string | Retirement/CertificationExpiry/LicenseExpiry/Attrition/Hiring/Training/Competency |
| Version | int |
| GeneratedAtUtc | DateTime |
| InputSnapshotIdsJson | string |
| ParametersJson | string |
| ResultsJson | string |
| CorrelationId | string? |

### WorkforceRecommendation

| Field | Type |
|-------|------|
| Id | Guid |
| OrganizationId | Guid |
| RecommendationType | string |
| Title | string |
| Explanation | string |
| SourceReferencesJson | string | `[{ service, entityType, entityId }]` |
| Confidence | decimal |
| ModelVersion | string? |
| Priority | string |
| GeneratedAtUtc | DateTime |
| ExpiresAtUtc | DateTime? |

### AnalyticsCache / RecommendationCache

Denormalized JSON payloads with TTL.

| Field | Type |
|-------|------|
| CacheKey | string | `{orgId}:{scope}:{asOf}` |
| PayloadJson | string |
| ExpiresAtUtc | DateTime |
| CreatedAtUtc | DateTime |

### WorkforceSearchIndex

| Field | Type |
|-------|------|
| PersonId | Guid |
| EmploymentId | Guid |
| OrganizationId | Guid |
| SearchText | string | Denormalized |
| SkillsJson | string |
| CertificationsJson | string |
| DepartmentName | string? |
| ProfessionalCategory | string? |
| IndexedAtUtc | DateTime |

---

## Event catalog

### Subscribe (via `workforce-events`)

| Event | Projection handler |
|-------|-------------------|
| `EmploymentCreated` | Headcount + org analytics |
| `EmploymentTransferred` | Headcount redistribution |
| `EmploymentTerminated` | Attrition metrics |
| `QualificationAdded` | Qualification distribution |
| `CertificationExpired` | Certification matrix |
| `LicenseRenewed` | License matrix |
| `TrainingCompleted` | Training matrix |
| `ResponsibilityAssigned` | Responsibility matrix |
| `CommitteeDecisionRecorded` | Committee analytics |
| `PersonUpdated` | Demographics refresh |

### Produce (intelligence outbox)

| Event | When |
|-------|------|
| `WorkforceSnapshotUpdated` | After snapshot job |
| `ForecastGenerated` | After forecast run |
| `RecommendationGenerated` | After recommendation batch |
| `DashboardRefreshed` | After cache invalidation |

---

## KPI definitions

| KPI | Formula | Source |
|-----|---------|--------|
| Headcount | Count active employments | WorkforceService |
| Training compliance % | Completed mandatory / total mandatory | ProfessionalService |
| Credential risk | Count expiring within 90 days | ProfessionalService renewals |
| Skill gap | Skills where current < target | ProfessionalService competencies |
| Span of control | Avg direct reports per manager | WorkforceService org-chart |
| Availability coverage | Available staff / required coverage | CommunicationService |
| Attrition rate | Terminations in period / avg headcount | WorkforceSnapshot history |

---

## Reconciliation rules

1. Full snapshot headcount must match WorkforceService active employment count (±0).
2. Training compliance % must match manual count from ProfessionalService training records.
3. Forecast versions are immutable; new run creates new version.
4. Recommendations must include at least one `sourceRef`.
5. All snapshots carry `AsOfUtc` and `CorrelationId`.

---

## Enterprise Intelligence Platform seed

Reusable abstractions in `Application/Intelligence/Platform/`:

- `IIntelligenceProjectionHandler` — event → read model mutation
- `ISnapshotJob` — scheduled HTTP hydration + KPI compute
- `IForecastEngine` — versioned forecast generation
- `IRecommendationOrchestrator` — rule-based + AI with explainability
- `IAnalyticsCache` — TTL cache for dashboard payloads

Future phases (Compliance Intelligence, Audit Intelligence, etc.) clone this pattern.

---

## API contract summary

Base: `GET /api/v1/intelligence/*`

| Route | Response |
|-------|----------|
| `/dashboard` | `ExecutiveDashboardDto` |
| `/headcount` | `HeadcountAnalyticsDto` |
| `/skills` | `SkillMatrixDto` |
| `/competencies` | `CompetencyMatrixDto` |
| `/training` | `TrainingMatrixDto` |
| `/licenses` | `LicenseDashboardDto` |
| `/certifications` | `CertificationDashboardDto` |
| `/forecast` | `ForecastDto` |
| `/recommendations` | `RecommendationListDto` |
| `/search` | `WorkforceSearchResultDto` |
| `/organization` | `OrganizationAnalyticsDto` |
| `/responsibilities` | `ResponsibilityMatrixDto` |
| `/committees` | `CommitteeAnalyticsDto` |
| `/availability` | `AvailabilityAnalyticsDto` |

Common query params: `organizationId`, `hospitalId?`, `departmentId?`, `asOf?`, `from?`, `to?`

---

## Boundary: Workforce Intelligence vs Enterprise Directory

**Workforce Intelligence** (Phase 10) owns analytics projections: executive dashboards, skill/competency matrices, forecasts, and recommendations. Its `/intelligence/search` endpoint is a lightweight analytics lookup over `WorkforceSearchIndex` — not a governed enterprise discovery surface.

**Enterprise Directory** (Phase 11) owns governed discovery: SQL FTS on `DirectoryIndex`, composable profiles, facets, saved searches, NL query translation, and Access Governance RLS + field masking on every query.

Do not extend `/intelligence/*` for directory discovery. Route all people discovery, assignee lookup, and enterprise profile reads through `/directory/*` via Gateway.
