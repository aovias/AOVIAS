namespace ComplianceService.Application.TaskExecution;

/// <summary>Risk band derived from <see cref="TaskRiskCalculator.ComputeScore"/>.</summary>
public enum TaskRiskLevelApi
{
    Low = 0,
    Medium = 1,
    High = 2,
    Critical = 3
}
