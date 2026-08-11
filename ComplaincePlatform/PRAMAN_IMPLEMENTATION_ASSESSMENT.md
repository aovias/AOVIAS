# PRAMAN Event-Driven Compliance - Implementation Assessment

## What Can Be Implemented

### ✅ Already Implemented
- **Outbox Pattern Foundation**: `OutboxMessage` entity, `OutboxDomainEventDispatcher`, `InProcessOutboxProcessor`
- **HospitalComplianceAggregate**: Core aggregate with `(HospitalId, FrameworkId)` scope
- **Domain Events**: `ComplianceUpdatedDomainEvent` with outbox serialization
- **Snapshot Infrastructure**: `ComplianceSnapshot` entity exists (needs Level 1/2 separation)
- **Command Flow**: Transactional command handlers with outbox insert

### 🚧 Ready to Implement
1. **Level 2 Framework Snapshot Separation**
   - Split `ComplianceSnapshot` into `FrameworkComplianceSnapshot` and `FrameworkChapterSnapshot`
   - Add `JourneyState` field to framework snapshot
   - Implement chapter-level breakdown

2. **Level 1 Organization Snapshot**
   - Create `OrganizationComplianceOverviewSnapshot` entity
   - Weighted average calculation across frameworks
   - Worst framework identification
   - Renewal alerts aggregation

3. **Journey State Machine**
   - Add `JourneyState` enum/property to `HospitalComplianceAggregate`
   - Implement `JourneyPolicy` interface for framework-specific state transitions
   - Add `TransitionJourneyState` domain method

4. **RabbitMQ Integration**
   - Outbox relay worker (background service)
   - Integration event publishing
   - Retry mechanism with `RetryCount` field
   - Dead-letter queue handling

5. **Evidence Management**
   - `Evidence` entity enhancement (add `Hash`, `FrameworkId`)
   - Azure Blob Storage integration
   - Multi-tenant path structure: `/tenant/{hospitalId}/framework/{frameworkId}/requirement/{requirementId}/`
   - File upload API endpoints

6. **Report Generation**
   - Self Assessment Report
   - Audit Ready Summary
   - NC Register
   - Renewal Compliance Status
   - Generate from snapshot tables only (no live aggregate queries)

## Assumptions

1. **Framework Master Data**: Framework entities (`ComplianceFramework`) exist and are pre-seeded
2. **User Context**: `IUserContextService` provides `HospitalId` for multi-tenancy
3. **Scoring Logic**: Existing `ComplianceScoringService` can be extended for framework-specific calculations
4. **Database**: SQL Server with EF Core migrations support
5. **Background Processing**: Hangfire is available for outbox relay worker
6. **Azure Integration**: Azure Blob Storage account and credentials are configured
7. **RabbitMQ**: RabbitMQ server is available and connection strings are configured

## Considerations

### Technical
- **Snapshot Rebuild**: Need capability to rebuild snapshots from event history (for disaster recovery)
- **Eventual Consistency**: Dashboard APIs must read from snapshots, not aggregates (few seconds delay acceptable)
- **Idempotency**: All projection handlers must be idempotent (handle duplicate events)
- **Concurrency**: Aggregate-level optimistic locking via `RowVersion`
- **Performance**: Snapshot tables require proper indexing (`(HospitalId, FrameworkId)` unique constraint)

### Business Logic Dependencies

1. **Framework Enrollment**
   - Business rule: Can hospital enroll in multiple frameworks simultaneously?
   - Validation: Framework version compatibility checks
   - State: Initial journey state after enrollment

2. **Journey State Transitions**
   - Framework-specific state machines (NABH vs CEA vs PCPNDT may have different states)
   - Validation rules for state transitions
   - Who can trigger transitions? (Role-based permissions)

3. **Scoring Calculation**
   - Framework-specific scoring weights
   - Risk penalty calculation methodology
   - Audit confidence simulation algorithm
   - Chapter-level vs requirement-level scoring granularity

4. **Evidence Validation**
   - File type restrictions per requirement
   - File size limits
   - Evidence expiration rules
   - Evidence approval workflow

5. **Renewal Logic**
   - Framework renewal period calculation
   - Renewal deadline alerts (how many days before?)
   - Renewal scoring impact (does score reset or accumulate?)

6. **Report Generation Rules**
   - Report template selection (framework-specific?)
   - Data retention policy for generated reports
   - Report versioning strategy
   - Access control (who can generate which reports?)

## Dependencies

### Infrastructure
- **RabbitMQ**: Message broker for event publishing
- **Azure Blob Storage**: File storage for evidence
- **Hangfire**: Background job processing for outbox relay
- **PDF Generation Library**: For report generation (e.g., QuestPDF, iTextSharp)

### Domain Knowledge Required
- **Regulatory Framework Details**: NABH, CEA, PCPNDT, Fire Safety, ISO specific requirements
- **State Machine Definitions**: Framework-specific journey states and transitions
- **Scoring Methodology**: How scores are calculated per framework type
- **Evidence Requirements**: What evidence is required per requirement type

### External Services
- **Azure Blob Storage SDK**: For file upload/download
- **RabbitMQ Client Library**: For message publishing/consuming
- **PDF Generation Service**: For report creation

## Implementation Priority

1. **Phase 1** (Foundation)
   - Level 2 Framework Snapshot separation
   - Journey state machine basic implementation
   - Evidence entity enhancement

2. **Phase 2** (Integration)
   - RabbitMQ outbox relay worker
   - Azure Blob Storage integration
   - Level 1 Organization Snapshot

3. **Phase 3** (Advanced Features)
   - Report generation
   - Renewal engine
   - AI-assisted gap detection

## Notes

- Current `ComplianceSnapshot` may need migration to split into Level 1/2 snapshots
- `OutboxMessage` entity matches blueprint specification (needs `AggregateId` and `RetryCount` fields)
- Projection handlers should be registered as RabbitMQ consumers (not in-process)
- All dashboard APIs must be read-only and query snapshots only
