namespace ComplianceService.Domain.Enums;

/// <summary>
/// Execution priority for a compliance task (distinct from <see cref="TaskCriticalityLevel"/> used for scoring).
/// Numeric values align with <see cref="TaskCriticalityLevel"/> (1 = most urgent tier).
/// </summary>
public enum ComplianceTaskPriority
{
    /// <summary>Highest execution priority.</summary>
    Urgent = 1,

    High = 2,

    Medium = 3,

    Low = 4
}
