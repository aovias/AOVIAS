# Workforce Intelligence API Flows — EIWAM Phase 10

## Snapshot refresh (scheduled + manual)

```mermaid
sequenceDiagram
  participant Scheduler as SnapshotSchedulerHostedService
  participant Snap as WorkforceSnapshotService
  participant WS as WorkforceService
  participant PS as ProfessionalService
  participant GS as GovernanceService
  participant DB as WorkforceIntelligence DB
  participant Outbox as Intelligence Outbox

  Scheduler->>Snap: RefreshOrganizationAsync(orgId)
  Snap->>WS: GET /api/v1/employment
  Snap->>PS: GET /api/v1/professional-profiles
  Snap->>GS: GET /api/v1/governance/analytics/summary
  Snap->>DB: Write WorkforceSnapshot + matrices
  Snap->>Outbox: WorkforceSnapshotUpdated
```

## Event projection (hybrid)

```mermaid
sequenceDiagram
  participant Source as Workforce/Professional/Governance
  participant Relay as OutboxRelayWorker
  participant SB as workforce-events
  participant Consumer as IntelligenceEventConsumer
  participant Projector as ProjectionEngine
  participant Snap as WorkforceSnapshotService

  Source->>Relay: OutboxMessage ProcessedOn set
  Relay->>SB: Publish Subject=EventType
  SB->>Consumer: Message received
  Consumer->>Projector: ProcessAsync(envelope)
  Projector->>Snap: RefreshOrganizationAsync (incremental)
```

## Recommendation generation

```mermaid
sequenceDiagram
  participant API as GET /intelligence/recommendations
  participant Orch as RecommendationOrchestrator
  participant DB as Read Models
  participant Rules as Rule-based layer

  API->>DB: Query WorkforceRecommendation
  Note over Orch: On snapshot or CertificationExpired event
  Orch->>Rules: Expiring creds, training gaps, resp gaps
  Rules->>DB: Persist with SourceReferences[]
```

## Gateway read path

```mermaid
sequenceDiagram
  participant Web as Angular WebApp
  participant GW as Gateway
  participant WI as WorkforceIntelligenceService

  Web->>GW: GET /api/v1/intelligence/dashboard
  GW->>WI: Proxy with JWT + permissions
  WI->>WI: IntelligenceQueryService + AnalyticsCache
  WI-->>Web: ApiResponse ExecutiveDashboardDto
```

## Boundary rule

WorkforceIntelligenceService **never** POST/PUT to source services. All writes are to read models, caches, forecasts, and recommendations only.
