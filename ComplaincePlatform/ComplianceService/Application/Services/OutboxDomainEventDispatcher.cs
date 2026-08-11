using ComplianceService.Application.Contracts;
using ComplianceService.Domain;
using ComplianceService.Domain.Events;
using ComplianceService.Infrastructure.Persistence;
using System.Text.Json;

namespace ComplianceService.Application.Services;

public class OutboxDomainEventDispatcher : IDomainEventDispatcher
{
    private readonly ComplianceDbContext _dbContext;

    public OutboxDomainEventDispatcher(ComplianceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task DispatchAsync(IEnumerable<IDomainEvent> events, CancellationToken ct)
    {
        if (events == null)
        {
            return;
        }

            foreach (var domainEvent in events)
            {
                // Handle ComplianceScoreUpdatedDomainEvent
                if (domainEvent is ComplianceScoreUpdatedDomainEvent scoreUpdatedEvent)
                {
                    await AddOutboxMessageAsync(scoreUpdatedEvent, scoreUpdatedEvent.HospitalId, scoreUpdatedEvent.FrameworkId, ct);
                }
                // Handle FrameworkEnrolledDomainEvent
                else if (domainEvent is FrameworkEnrolledDomainEvent enrolledEvent)
                {
                    await AddOutboxMessageAsync(enrolledEvent, enrolledEvent.HospitalId, enrolledEvent.FrameworkId, ct);
                }
                // Handle Compliance Journey domain events
                else if (domainEvent is ComplianceEnrolledDomainEvent enrolledJourneyEvent)
                {
                    await AddOutboxMessageAsync(enrolledJourneyEvent, enrolledJourneyEvent.HospitalId, enrolledJourneyEvent.FrameworkId, ct);
                }
                else if (domainEvent is ComplianceSubmittedDomainEvent submittedEvent)
                {
                    await AddOutboxMessageAsync(submittedEvent, submittedEvent.HospitalId, submittedEvent.FrameworkId, ct);
                }
                else if (domainEvent is ComplianceAuditScheduledDomainEvent auditScheduledEvent)
                {
                    await AddOutboxMessageAsync(auditScheduledEvent, auditScheduledEvent.HospitalId, auditScheduledEvent.FrameworkId, ct);
                }
                else if (domainEvent is ComplianceAuditCompletedDomainEvent auditCompletedEvent)
                {
                    await AddOutboxMessageAsync(auditCompletedEvent, auditCompletedEvent.HospitalId, auditCompletedEvent.FrameworkId, ct);
                }
                else if (domainEvent is ComplianceApprovedDomainEvent approvedEvent)
                {
                    await AddOutboxMessageAsync(approvedEvent, approvedEvent.HospitalId, approvedEvent.FrameworkId, ct);
                }
                else if (domainEvent is ComplianceRejectedDomainEvent rejectedEvent)
                {
                    await AddOutboxMessageAsync(rejectedEvent, rejectedEvent.HospitalId, rejectedEvent.FrameworkId, ct);
                }
                else if (domainEvent is ComplianceSuspendedDomainEvent suspendedEvent)
                {
                    await AddOutboxMessageAsync(suspendedEvent, suspendedEvent.HospitalId, suspendedEvent.FrameworkId, ct);
                }
                else if (domainEvent is ComplianceExpiredDomainEvent expiredEvent)
                {
                    await AddOutboxMessageAsync(expiredEvent, expiredEvent.HospitalId, expiredEvent.FrameworkId, ct);
                }
                else if (domainEvent is ComplianceRenewalInProgressDomainEvent renewalInProgressEvent)
                {
                    await AddOutboxMessageAsync(renewalInProgressEvent, renewalInProgressEvent.HospitalId, renewalInProgressEvent.FrameworkId, ct);
                }
                else if (domainEvent is ComplianceRenewedDomainEvent renewedEvent)
                {
                    await AddOutboxMessageAsync(renewedEvent, renewedEvent.HospitalId, renewedEvent.FrameworkId, ct);
                }
                else if (domainEvent is ObligationStatusUpdatedDomainEvent obligationEvent)
                {
                    await AddOutboxMessageAsync(obligationEvent, obligationEvent.HospitalId, obligationEvent.FrameworkId, ct);
                }
                else if (domainEvent is DeficiencyIdentifiedDomainEvent deficiencyIdentified)
                {
                    await AddOutboxMessageAsync(deficiencyIdentified, deficiencyIdentified.HospitalId, deficiencyIdentified.FrameworkId, ct);
                }
                else if (domainEvent is DeficiencyClosedDomainEvent deficiencyClosed)
                {
                    await AddOutboxMessageAsync(deficiencyClosed, deficiencyClosed.HospitalId, deficiencyClosed.FrameworkId, ct);
                }
                else if (domainEvent is DeficiencyStatusChangedDomainEvent deficiencyStatusChanged)
                {
                    await AddOutboxMessageAsync(
                        deficiencyStatusChanged,
                        deficiencyStatusChanged.HospitalId,
                        deficiencyStatusChanged.FrameworkId,
                        ct);
                }
                else if (domainEvent is ComplianceStateChangedDomainEvent stateChangedEvent)
                {
                    await AddOutboxMessageAsync(
                        stateChangedEvent,
                        stateChangedEvent.HospitalId,
                        stateChangedEvent.FrameworkId,
                        ct);
                }
                else
                {
                    // Fallback for unknown domain events - use ComplianceJourneyId if available
                    var journeyIdProperty = domainEvent.GetType().GetProperty("ComplianceJourneyId");
                    if (journeyIdProperty != null && journeyIdProperty.GetValue(domainEvent) is Guid journeyId)
                    {
                        // For ComplianceJourney events, we need to get HospitalId and FrameworkId from the journey
                        // This is a fallback - ideally all events should have HospitalId and FrameworkId
                        await AddOutboxMessageAsync(domainEvent, Guid.Empty, Guid.Empty, ct);
                    }
                }
            }
    }

    private async Task AddOutboxMessageAsync(
        IDomainEvent domainEvent,
        Guid hospitalId,
        Guid frameworkId,
        CancellationToken ct)
    {
        // Serialize to JSON
        var payload = JsonSerializer.Serialize(domainEvent, new JsonSerializerOptions
        {
            WriteIndented = false
        });

        // Get the event type name
        var eventType = domainEvent.GetType().FullName ?? domainEvent.GetType().Name;

        // Generate AggregateId from HospitalId and FrameworkId (deterministic composite key)
        // Using deterministic GUID generation for consistency
        var aggregateIdBytes = new byte[16];
        BitConverter.GetBytes(hospitalId.GetHashCode()).CopyTo(aggregateIdBytes, 0);
        BitConverter.GetBytes(frameworkId.GetHashCode()).CopyTo(aggregateIdBytes, 8);
        var aggregateId = new Guid(aggregateIdBytes);

        // Create OutboxMessage
        var outboxMessage = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            AggregateId = aggregateId,
            OccurredOn = domainEvent.OccurredOn,
            Type = eventType,
            Payload = payload,
            ProjectionProcessedOn = null,
            RelayProcessedOn = null,
            RetryCount = 0,
            Error = null
        };

        // Add to DbContext (caller owns transaction, no SaveChanges here)
        await _dbContext.OutboxMessages.AddAsync(outboxMessage, ct);
    }
}
