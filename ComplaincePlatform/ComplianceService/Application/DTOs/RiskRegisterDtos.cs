using ComplianceService.Domain.Enums;

namespace ComplianceService.Application.DTOs;

public sealed class RiskListDto
{
    public IReadOnlyList<RiskSummaryDto> Items { get; set; } = Array.Empty<RiskSummaryDto>();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

public class RiskSummaryDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Category { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public int Likelihood { get; set; }
    public int Impact { get; set; }
    public int RiskScore { get; set; }
    public int? ResidualLikelihood { get; set; }
    public int? ResidualImpact { get; set; }
    public int? ResidualRiskScore { get; set; }
    public Guid? OwnerUserId { get; set; }
    public string? OwnerName { get; set; }
    public Guid? DepartmentId { get; set; }
    public DateTime? ReviewDueDate { get; set; }
    public DateTime IdentifiedAt { get; set; }
    public DateTime LastUpdatedAt { get; set; }
}

public sealed class RiskDetailDto : RiskSummaryDto
{
    public Guid TenantId { get; set; }
    public Guid HospitalId { get; set; }
    public string? Description { get; set; }
    public string? MitigationPlan { get; set; }
    public Guid? ObjectiveElementId { get; set; }
    public Guid? TaskId { get; set; }
    public Guid? FindingId { get; set; }
    public Guid? IncidentId { get; set; }
    public Guid? DeficiencyId { get; set; }
    public DateTime? ClosedAt { get; set; }
}

public sealed class CreateRiskRequestDto
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Category { get; set; }
    public ComplianceSeverity Severity { get; set; } = ComplianceSeverity.Minor;
    public int Likelihood { get; set; } = 1;
    public int Impact { get; set; } = 1;
    public int? ResidualLikelihood { get; set; }
    public int? ResidualImpact { get; set; }
    public Guid? OwnerUserId { get; set; }
    public string? OwnerName { get; set; }
    public string? MitigationPlan { get; set; }
    public DateTime? ReviewDueDate { get; set; }
    public Guid? DepartmentId { get; set; }
    public Guid? ObjectiveElementId { get; set; }
    public Guid? TaskId { get; set; }
    public Guid? FindingId { get; set; }
    public Guid? IncidentId { get; set; }
    public Guid? DeficiencyId { get; set; }
    public RiskStatus? Status { get; set; }
}

public sealed class UpdateRiskRequestDto
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? Category { get; set; }
    public ComplianceSeverity? Severity { get; set; }
    public int? Likelihood { get; set; }
    public int? Impact { get; set; }
    public int? ResidualLikelihood { get; set; }
    public int? ResidualImpact { get; set; }
    public Guid? OwnerUserId { get; set; }
    public string? OwnerName { get; set; }
    public string? MitigationPlan { get; set; }
    public DateTime? ReviewDueDate { get; set; }
    public Guid? DepartmentId { get; set; }
    public Guid? ObjectiveElementId { get; set; }
    public Guid? TaskId { get; set; }
    public Guid? FindingId { get; set; }
    public Guid? IncidentId { get; set; }
    public Guid? DeficiencyId { get; set; }
    public RiskStatus? Status { get; set; }
}

public sealed class RiskMatrixDto
{
    public IReadOnlyList<RiskMatrixCellDto> Cells { get; set; } = Array.Empty<RiskMatrixCellDto>();
    public int TotalActiveRisks { get; set; }
    public int HighScoreCount { get; set; }
    public int ReviewDueCount { get; set; }
}

public sealed class RiskMatrixCellDto
{
    public int Likelihood { get; set; }
    public int Impact { get; set; }
    public int Score { get; set; }
    public int Count { get; set; }
    public string Band { get; set; } = "Low";
    public IReadOnlyList<RiskMatrixRiskItemDto> Risks { get; set; } = Array.Empty<RiskMatrixRiskItemDto>();
}

public sealed class RiskMatrixRiskItemDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int RiskScore { get; set; }
}
