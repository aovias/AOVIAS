using ComplianceService.Domain;
using ComplianceService.Infrastructure.Persistence;

namespace ComplianceService.Application.Dashboard.Projection;

public interface IDashboardProjectionEngine
{
    Task ProcessOutboxMessageAsync(OutboxMessage message, CancellationToken cancellationToken = default);

    Task RebuildAsync(
        DashboardProjectionScope scope,
        string triggerEventType,
        Guid? outboxMessageId = null,
        CancellationToken cancellationToken = default);

    Task<DashboardProjection?> TryGetProjectionAsync(
        DashboardProjectionScope scope,
        CancellationToken cancellationToken = default);
}
