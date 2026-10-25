namespace ComplianceService.Application.DTOs;

public sealed class DataMigrationValidationReportDto
{
    public int TasksWithoutObjectiveElementId { get; init; }
    public int EvidenceWithoutTaskId { get; init; }
    public int FindingsWithoutScopeLinkage { get; init; }
    public int FindingsWithInvalidComplianceState { get; init; }
    public bool HasCriticalIssues { get; init; }
    public IReadOnlyList<string> SuggestedFixes { get; init; } = [];
}
