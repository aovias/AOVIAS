using ComplianceService.Application.Contracts;
using ComplianceService.Domain;

namespace ComplianceService.Application.Services;

public class InProcessDomainEventDispatcher : IDomainEventDispatcher
{
    private readonly IComplianceProjectionHandler _complianceProjectionHandler;
    private readonly ILogger<InProcessDomainEventDispatcher> _logger;

    public InProcessDomainEventDispatcher(
        IComplianceProjectionHandler complianceProjectionHandler,
        ILogger<InProcessDomainEventDispatcher> logger)
    {
        _complianceProjectionHandler = complianceProjectionHandler;
        _logger = logger;
    }

    public async Task DispatchAsync(IEnumerable<IDomainEvent> events, CancellationToken ct)
    {
        if (events == null)
        {
            return;
        }

        foreach (var domainEvent in events)
        {
            try
            {
                switch (domainEvent)
                {
                    case ComplianceScoreUpdatedDomainEvent scoreUpdatedEvent:
                        await _complianceProjectionHandler.HandleAsync(scoreUpdatedEvent, ct);
                        _logger.LogDebug(
                            "Dispatched ComplianceScoreUpdatedDomainEvent for HospitalId: {HospitalId}, FrameworkId: {FrameworkId}",
                            scoreUpdatedEvent.HospitalId, scoreUpdatedEvent.FrameworkId);
                        break;

                    default:
                        _logger.LogDebug(
                            "Ignoring unknown domain event type: {EventType}",
                            domainEvent.GetType().Name);
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error dispatching domain event of type {EventType}",
                    domainEvent.GetType().Name);
                // Continue processing other events even if one fails
                // In future, this could be configurable behavior
            }
        }
    }
}
