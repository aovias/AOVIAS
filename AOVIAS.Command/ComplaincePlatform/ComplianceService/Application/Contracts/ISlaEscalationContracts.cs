using ComplianceService.Application.DTOs;
using ComplianceService.Domain;
using ComplianceService.Domain.Enums;

namespace ComplianceService.Application.Contracts;

public interface ISlaPolicyService
{
    Task EnsureDefaultPoliciesAsync(Guid organizationId, Guid? hospitalId = null, CancellationToken ct = default);
    Task<IReadOnlyList<SlaPolicyDto>> ListPoliciesAsync(Guid organizationId, Guid? hospitalId = null, CancellationToken ct = default);
    Task<SlaPolicyDto> CreatePolicyAsync(CreateSlaPolicyRequestDto request, CancellationToken ct = default);
    Task<SlaPolicy?> ResolvePolicyForDeficiencyAsync(ComplianceDeficiency deficiency, CancellationToken ct = default);
}

public interface ISlaEvaluationService
{
    Task AssignSlaToDeficiencyAsync(ComplianceDeficiency deficiency, CancellationToken ct = default);
    Task<SlaEvaluationResultDto> EvaluateAsync(Guid deficiencyId, bool persist = true, CancellationToken ct = default);
    Task<IReadOnlyList<SlaEvaluationResultDto>> EvaluateBatchAsync(IReadOnlyList<Guid> deficiencyIds, bool persist = true, CancellationToken ct = default);
}

public interface IEscalationService
{
    Task<EscalationEvaluationResultDto> EvaluateAsync(Guid deficiencyId, SlaEvaluationResultDto sla, bool persist = true, CancellationToken ct = default);
    Task<IReadOnlyList<DeficiencyEscalationHistoryDto>> GetHistoryAsync(Guid deficiencyId, CancellationToken ct = default);
    Task<DeficiencyEscalationHistoryDto?> AcknowledgeAsync(Guid deficiencyId, Guid escalationId, string? acknowledgedBy, CancellationToken ct = default);
    Task<SlaEscalationMetricsDto> GetMetricsAsync(Guid organizationId, CancellationToken ct = default);
    Task<SlaEscalationMetricsDto> GetMetricsForScopeAsync(
        Guid organizationId,
        Guid hospitalId,
        Guid frameworkId,
        CancellationToken ct = default);
}

public class SlaPolicyDto
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid? HospitalId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string? LifecycleStage { get; set; }
    public int TargetResolutionHours { get; set; }
    public int WarningThresholdPercent { get; set; }
    public Guid? EscalationPolicyId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedOn { get; set; }
}

public class CreateSlaPolicyRequestDto
{
    public Guid OrganizationId { get; set; }
    public Guid? HospitalId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DeficiencySeverity Severity { get; set; }
    public ObjectiveElementClassification? LifecycleStage { get; set; }
    public int TargetResolutionHours { get; set; }
    public int WarningThresholdPercent { get; set; } = 80;
    public Guid? EscalationPolicyId { get; set; }
}

public class SlaEvaluationResultDto
{
    public Guid DeficiencyId { get; set; }
    public Guid? SlaPolicyId { get; set; }
    public string? PolicyName { get; set; }
    public DateTime? SlaStartDate { get; set; }
    public DateTime? SlaDueDate { get; set; }
    public string CurrentSlaStatus { get; set; } = nameof(SlaStatus.OnTrack);
    public double ElapsedHours { get; set; }
    public double RemainingHours { get; set; }
    public double OverdueHours { get; set; }
    public double PercentConsumed { get; set; }
    public int? DaysRemaining { get; set; }
    public bool IsOverdue { get; set; }
}

public class EscalationEvaluationResultDto
{
    public Guid DeficiencyId { get; set; }
    public int? CurrentLevel { get; set; }
    public string? CurrentState { get; set; }
    public string? TargetUserId { get; set; }
    public string? TargetLabel { get; set; }
    public bool WasEscalated { get; set; }
}

public class DeficiencyEscalationHistoryDto
{
    public Guid Id { get; set; }
    public int Level { get; set; }
    public string? TargetUserId { get; set; }
    public string TargetLabel { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public DateTime TriggeredAt { get; set; }
    public DateTime? AcknowledgedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
}

public class SlaEscalationMetricsDto
{
    public int TotalOpenWithSla { get; set; }
    public int OnTrackCount { get; set; }
    public int WarningCount { get; set; }
    public int AtRiskCount { get; set; }
    public int BreachedCount { get; set; }
    public int EscalatedCount { get; set; }
    public int CriticalEscalations { get; set; }
    public double BreachRate { get; set; }
    public double EscalationRate { get; set; }
    public double AverageResolutionHours { get; set; }
    public IReadOnlyList<DeficiencyCountByLabelDto> DepartmentSlaCompliance { get; set; } = Array.Empty<DeficiencyCountByLabelDto>();
}
