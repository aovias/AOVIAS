# Message Consumers Implementation Summary

## ✅ Implementation Complete

All three RabbitMQ message consumers for snapshot queues have been successfully implemented and registered.

## Components Created

### 1. Base Consumer Infrastructure

**File:** `ComplianceService/Infrastructure/Messaging/RabbitMQConsumerBase.cs`

- Abstract base class for all RabbitMQ consumers
- Inherits from `BackgroundService` for continuous message processing
- Handles:
  - Connection management and reconnection logic
  - Message deserialization
  - Error handling and dead-letter queue routing
  - Manual message acknowledgment
  - QoS configuration (prefetch count)
  - Automatic retry on connection failures

**Key Features:**
- Automatic reconnection with 5-second delay on failures
- Manual acknowledgment (ack on success, reject to DLQ on failure)
- Prefetch count from configuration (default: 10)
- Proper disposal of connections and channels

### 2. Framework Snapshot Consumer

**File:** `ComplianceService/Application/Consumers/FrameworkSnapshotConsumer.cs`

**Queue:** `compliance.snapshot.framework`

**Purpose:** Updates `ComplianceSnapshot` (FrameworkComplianceSnapshot) when compliance events occur.

**Functionality:**
- Listens to `ComplianceUpdatedIntegrationEvent` messages
- Calculates framework-specific compliance score using `CalculateFrameworkScoreAsync`
- Updates or creates `ComplianceSnapshot` record
- Stores:
  - Overall score
  - Audit confidence score
  - Critical/Major/Minor counts
  - Chapter breakdown (JSON)
  - Critical risk count
  - Journey state (if available)

**Transaction Handling:**
- Uses execution strategy for retry compatibility
- Atomic upsert operation

### 3. Chapter Snapshot Consumer

**File:** `ComplianceService/Application/Consumers/ChapterSnapshotConsumer.cs`

**Queue:** `compliance.snapshot.chapter`

**Purpose:** Updates `FrameworkChapterSnapshot` for each chapter when compliance events occur.

**Functionality:**
- Listens to `ComplianceUpdatedIntegrationEvent` messages
- Calculates framework score to get chapter breakdown
- For each chapter in the framework:
  - Counts completed vs total tasks
  - Calculates risk penalty from open risks
  - Updates or creates `FrameworkChapterSnapshot` record
- Stores:
  - Chapter score
  - Completed tasks count
  - Total tasks count
  - Risk penalty

**Transaction Handling:**
- Uses execution strategy for retry compatibility
- Batch updates all chapters in a single transaction

### 4. Organization Snapshot Consumer

**File:** `ComplianceService/Application/Consumers/OrganizationSnapshotConsumer.cs`

**Queue:** `compliance.snapshot.organization`

**Purpose:** Updates `OrganizationComplianceOverviewSnapshot` (aggregates across all frameworks).

**Functionality:**
- Listens to `ComplianceUpdatedIntegrationEvent` messages
- Aggregates data from all framework snapshots for the hospital
- Calculates:
  - Weighted average overall score across all frameworks
  - Worst performing framework ID
  - Total critical risks across all frameworks
  - Upcoming renewals (next 10, sorted by date)
- Updates or creates `OrganizationComplianceOverviewSnapshot` record

**Transaction Handling:**
- Uses execution strategy for retry compatibility
- Atomic upsert operation

## Service Registration

**File:** `ComplianceService/Program.cs`

All three consumers are registered as hosted services:

```csharp
builder.Services.AddHostedService<FrameworkSnapshotConsumer>();
builder.Services.AddHostedService<ChapterSnapshotConsumer>();
builder.Services.AddHostedService<OrganizationSnapshotConsumer>();
```

## Enhanced Scoring Service

**File:** `ComplianceService/Application/Services/ComplianceScoringService.cs`

**New Method:** `CalculateFrameworkScoreAsync(Guid hospitalId, Guid frameworkId)`

- Calculates compliance score for a specific framework
- Filters tasks and chapters by framework
- Returns framework-specific score with chapter breakdown
- Used by FrameworkSnapshotConsumer and ChapterSnapshotConsumer

**Refactoring:**
- Extracted common scoring logic into `CalculateScoreFromTasksAsync` private method
- Both `CalculateHospitalScoreAsync` and `CalculateFrameworkScoreAsync` use shared logic

## Message Flow

1. **Domain Event Raised:**
   - `ComplianceCommandService.CompleteTaskAsync()` raises `ComplianceUpdatedDomainEvent`

2. **Outbox Pattern:**
   - Event serialized and inserted into `OutboxMessages` (atomic with state change)

3. **Outbox Relay Worker:**
   - Polls `OutboxMessages` every 5 seconds
   - Publishes `ComplianceUpdatedIntegrationEvent` to RabbitMQ exchange `praman.events`
   - Routing key: `compliance.updated`

4. **Queue Routing:**
   - Message routed to all three snapshot queues:
     - `compliance.snapshot.framework`
     - `compliance.snapshot.chapter`
     - `compliance.snapshot.organization`

5. **Consumer Processing:**
   - Each consumer processes the message independently
   - Updates respective snapshot table
   - Acknowledges message on success
   - Rejects to DLQ on failure

## Error Handling

### Automatic Retry
- Connection failures: Automatic reconnection with 5-second delay
- Transient errors: Retried by execution strategy (database)

### Dead Letter Queue
- Permanent failures: Messages rejected without requeue
- Failed messages routed to `compliance.dlq` for manual inspection

### Logging
- All consumers log at appropriate levels:
  - Information: Successful processing
  - Warning: Missing data or skipped operations
  - Error: Processing failures with full exception details

## Configuration

All consumers use configuration from `appsettings.json`:

```json
"RabbitMQ": {
  "FrameworkSnapshotQueue": "compliance.snapshot.framework",
  "ChapterSnapshotQueue": "compliance.snapshot.chapter",
  "OrganizationSnapshotQueue": "compliance.snapshot.organization",
  "PrefetchCount": 10
}
```

## Testing

### Manual Testing Steps

1. **Trigger Event:**
   ```bash
   # Complete a compliance task via API
   POST /api/v1/compliance/tasks/{taskId}/complete
   ```

2. **Verify Outbox:**
   - Check `OutboxMessages` table for new record
   - Verify `ProcessedOn` is set after OutboxRelayWorker runs

3. **Verify RabbitMQ:**
   - Check RabbitMQ Management UI
   - Verify messages appear in snapshot queues
   - Verify messages are consumed (queue count decreases)

4. **Verify Snapshots:**
   - Check `ComplianceSnapshots` table for updated record
   - Check `FrameworkChapterSnapshots` table for chapter records
   - Check `OrganizationComplianceOverviewSnapshots` table for organization record

5. **Check Logs:**
   - Look for consumer log messages:
     - "Processing framework snapshot update..."
     - "Processing chapter snapshot update..."
     - "Processing organization snapshot update..."
     - "Updated framework snapshot..."
     - "Updated chapter snapshots..."
     - "Updated organization snapshot..."

## Performance Considerations

- **Prefetch Count:** Set to 10 to limit unacknowledged messages per consumer
- **Batch Processing:** Chapter consumer processes all chapters in one transaction
- **Connection Pooling:** Shared connection factory for all consumers
- **Execution Strategy:** Database retries handled automatically

## Next Steps

1. **Monitor Consumer Health:**
   - Add health checks for consumers
   - Monitor message processing rates
   - Alert on DLQ message accumulation

2. **Performance Tuning:**
   - Adjust prefetch count based on message volume
   - Consider parallel processing for large frameworks
   - Optimize database queries if needed

3. **Enhanced Features:**
   - Add metrics/telemetry for consumer performance
   - Implement circuit breaker pattern for resilience
   - Add consumer lag monitoring

## Summary

✅ **All three message consumers implemented and registered**
✅ **Base consumer infrastructure with error handling**
✅ **Framework-specific scoring method added**
✅ **Transaction handling with execution strategy**
✅ **Dead-letter queue routing for failures**
✅ **Comprehensive logging and error handling**

The system is now ready to process compliance events and maintain up-to-date snapshot read models!
