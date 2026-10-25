namespace Shared.Authorization;

public sealed class AuthorizationDecision
{
    public bool Allowed { get; init; }
    public string Reason { get; init; } = string.Empty;
    public IReadOnlyList<string> MatchedRoles { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> MatchedPermissions { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> MatchedPolicies { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> MatchedDelegations { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> MatchedResponsibilities { get; init; } = Array.Empty<string>();
    public IReadOnlyList<EvaluationTraceStep> EvaluationTrace { get; init; } = Array.Empty<EvaluationTraceStep>();
    public RowLevelFilter? RowLevelFilter { get; init; }
}

public sealed class EvaluationTraceStep
{
    public string Step { get; init; } = string.Empty;
    public string Outcome { get; init; } = string.Empty;
    public string? Detail { get; init; }
}

public sealed class RowLevelFilter
{
    public Guid? OrganizationId { get; init; }
    public Guid? HospitalId { get; init; }
    public Guid? DepartmentId { get; init; }
}

public sealed class EffectivePermission
{
    public string Code { get; init; } = string.Empty;
    public IReadOnlyList<string> Sources { get; init; } = Array.Empty<string>();
}
