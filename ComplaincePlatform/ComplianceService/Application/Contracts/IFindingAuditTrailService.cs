namespace ComplianceService.Application.Contracts;

public interface IFindingAuditTrailService
{
    Task LogAsync(
        Guid findingId,
        string action,
        Guid? actorUserId,
        string actorType,
        string? previousStateJson,
        string? newStateJson,
        CancellationToken cancellationToken = default);
}
