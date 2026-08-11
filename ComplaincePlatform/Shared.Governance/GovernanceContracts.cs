namespace Shared.Governance;

public sealed class GovernedResourceRef
{
    public required string ResourceType { get; init; }
    public Guid? ResourceId { get; init; }
}

public sealed class AssigneeRef
{
    public required string AssigneeType { get; init; }
    public required Guid AssigneeId { get; init; }
}

public sealed class GovernanceTimelineEntryDto
{
    public Guid Id { get; init; }
    public string EventType { get; init; } = string.Empty;
    public string Summary { get; init; } = string.Empty;
    public DateTime OccurredAt { get; init; }
    public Guid? ActorUserId { get; init; }
}

public sealed class OwnerLookupRequest
{
    public required string ResourceType { get; init; }
    public Guid ResourceId { get; init; }
    public string? ResponsibilityTypeCode { get; init; }
}

public sealed class GovernanceOwnerDto
{
    public Guid Id { get; init; }
    public Guid ResponsibilityId { get; init; }
    public string AssigneeType { get; init; } = string.Empty;
    public Guid AssigneeId { get; init; }
    public string Priority { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
}

public sealed class OwnerLookupResult
{
    public IReadOnlyList<GovernanceOwnerDto> Owners { get; init; } = Array.Empty<GovernanceOwnerDto>();
}

public sealed class ApprovalEvaluateRequest
{
    public Guid ApprovalChainId { get; init; }
    public GovernedResourceRef? Resource { get; init; }
    public Guid? InitiatorUserId { get; init; }
    public IReadOnlyDictionary<string, string> Context { get; init; } = new Dictionary<string, string>();
}

public sealed class ApprovalDecision
{
    public bool Completed { get; init; }
    public bool Approved { get; init; }
    public string Reason { get; init; } = string.Empty;
    public IReadOnlyList<string> MatchedSteps { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> PendingApprovers { get; init; } = Array.Empty<string>();
}

public sealed class EscalationResolveRequest
{
    public Guid EscalationChainId { get; init; }
    public GovernedResourceRef? Resource { get; init; }
    public Guid? CurrentAssigneeUserId { get; init; }
    public int CurrentLevel { get; init; }
    public IReadOnlyDictionary<string, string> Context { get; init; } = new Dictionary<string, string>();
}

public sealed class EscalationResolution
{
    public int ResolvedLevel { get; init; }
    public AssigneeRef? NextAssignee { get; init; }
    public string Reason { get; init; } = string.Empty;
    public bool EscalationComplete { get; init; }
}

public interface IGovernanceClient
{
    Task<IReadOnlyList<GovernanceTimelineEntryDto>> GetTimelineAsync(string resourceType, Guid resourceId, CancellationToken ct = default);
    Task<OwnerLookupResult> GetOwnersAsync(OwnerLookupRequest request, CancellationToken ct = default);
}

public interface IApprovalWorkflowEvaluator
{
    Task<ApprovalDecision> EvaluateAsync(ApprovalEvaluateRequest request, CancellationToken ct = default);
}

public interface IEscalationResolver
{
    Task<EscalationResolution> ResolveAsync(EscalationResolveRequest request, CancellationToken ct = default);
}
