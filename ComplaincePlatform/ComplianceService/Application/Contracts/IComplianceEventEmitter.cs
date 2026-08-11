using ComplianceService.Application.Events;

namespace ComplianceService.Application.Contracts;

public interface IComplianceEventEmitter
{
    Task EmitAsync(
        ComplianceEventType eventType,
        Guid entityId,
        string entityType,
        Guid tenantId,
        Guid? userId,
        object payload,
        CancellationToken cancellationToken = default);
}
