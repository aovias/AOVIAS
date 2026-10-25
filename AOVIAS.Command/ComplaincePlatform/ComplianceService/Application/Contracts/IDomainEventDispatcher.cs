using ComplianceService.Domain;

namespace ComplianceService.Application.Contracts;

public interface IDomainEventDispatcher
{
    Task DispatchAsync(IEnumerable<IDomainEvent> events, CancellationToken ct);
}
