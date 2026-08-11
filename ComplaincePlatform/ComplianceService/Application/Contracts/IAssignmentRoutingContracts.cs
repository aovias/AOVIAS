using ComplianceService.Domain;
using ComplianceService.Domain.Enums;

namespace ComplianceService.Application.Contracts;

/// <summary>Sprint 9B — primary remediation task is ownership authority; deficiency is projection.</summary>
public interface IDeficiencyTaskOwnershipSyncService
{
    Task<ComplianceTask?> GetPrimaryRemediationTaskAsync(Guid deficiencyId, CancellationToken ct = default);

    /// <summary>Task owner changed → sync deficiency projection.</summary>
    Task SyncDeficiencyOwnerFromTaskAsync(ComplianceTask task, string? updatedBy = null, CancellationToken ct = default);

    /// <summary>Deficiency owner changed → update primary task owner.</summary>
    Task SyncTaskOwnerFromDeficiencyAsync(
        ComplianceDeficiency deficiency,
        string? updatedBy = null,
        CancellationToken ct = default);

    /// <summary>Set owner on primary task and sync deficiency (canonical write path).</summary>
    Task SetPrimaryOwnerAsync(
        Guid deficiencyId,
        string ownerUserId,
        DateTime? dueDate = null,
        string? departmentName = null,
        Guid? departmentId = null,
        string? updatedBy = null,
        CancellationToken ct = default);
}

public interface IAssignmentRoutingService
{
    Task<AssignmentRoutingResultDto> ResolveForDeficiencyAsync(Guid deficiencyId, CancellationToken ct = default);
    Task<AssignmentRoutingResultDto> ResolveForObjectiveElementAsync(
        Guid organizationId,
        Guid hospitalId,
        Guid objectiveElementId,
        CancellationToken ct = default);
    Task<AssignmentRoutingResultDto> ResolveForFindingAsync(
        Guid findingId,
        Guid organizationId,
        Guid hospitalId,
        CancellationToken ct = default);

    /// <summary>Apply routing when confidence is high; otherwise returns suggestion only.</summary>
    Task<AssignmentRoutingApplyResultDto> TryAutoAssignDeficiencyAsync(
        Guid deficiencyId,
        bool forceAutoAssign = false,
        string? updatedBy = null,
        CancellationToken ct = default);

    Task<AssignmentRoutingMetricsDto> GetMetricsAsync(Guid organizationId, CancellationToken ct = default);
}

public class AssignmentRoutingResultDto
{
    public string? RecommendedAssigneeUserId { get; set; }
    public Guid? RecommendedDepartmentId { get; set; }
    public string? RecommendedDepartmentName { get; set; }
    public string? NormalizedDepartmentCode { get; set; }
    public AssignmentRoutingRuleType RuleApplied { get; set; }
    public string RuleAppliedLabel { get; set; } = string.Empty;
    public string RoutingReason { get; set; } = string.Empty;
    public AssignmentConfidence Confidence { get; set; }
    public bool CanAutoAssign { get; set; }
}

public class AssignmentRoutingApplyResultDto
{
    public AssignmentRoutingResultDto Suggestion { get; set; } = new();
    public bool WasApplied { get; set; }
    public bool WasAutoAssigned { get; set; }
    public Guid? PrimaryRemediationTaskId { get; set; }
}

public class AssignmentRoutingMetricsDto
{
    public int TotalRoutingDecisions { get; set; }
    public int AutoAssignedCount { get; set; }
    public int ManualOverrideCount { get; set; }
    public int HighConfidenceCount { get; set; }
    public double AutoAssignmentRate { get; set; }
    public double RoutingSuccessRate { get; set; }
    public IReadOnlyList<AssignmentCountByLabelDto> ByDepartment { get; set; } = Array.Empty<AssignmentCountByLabelDto>();
    public IReadOnlyList<AssignmentCountByLabelDto> ByRule { get; set; } = Array.Empty<AssignmentCountByLabelDto>();
}

public class AssignmentCountByLabelDto
{
    public string Label { get; set; } = string.Empty;
    public int Count { get; set; }
}
