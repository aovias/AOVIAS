namespace IdentityService.Application.Contracts;

public interface IIdentityAuditService
{
    Task RecordSuccessAsync(
        string eventCode,
        string action,
        Guid? actorUserId = null,
        Guid? targetUserId = null,
        string? oldValue = null,
        string? newValue = null,
        Guid? sessionId = null,
        string? reason = null,
        CancellationToken cancellationToken = default);

    Task RecordFailureAsync(
        string eventCode,
        string action,
        string reason,
        Guid? actorUserId = null,
        Guid? targetUserId = null,
        Guid? sessionId = null,
        CancellationToken cancellationToken = default);
}
