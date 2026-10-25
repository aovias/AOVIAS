namespace ComplianceService.Domain.Enums;

/// <summary>
/// Represents the criticality level of a compliance task.
/// Used to prioritize tasks and determine urgency for completion.
/// </summary>
public enum TaskCriticalityLevel : int
{
    /// <summary>
    /// Critical level - highest priority task.
    /// Must be completed immediately to avoid compliance failure.
    /// </summary>
    Critical = 1,

    /// <summary>
    /// High level - high priority task.
    /// Should be completed soon to maintain compliance readiness.
    /// </summary>
    High = 2,

    /// <summary>
    /// Medium level - moderate priority task.
    /// Should be completed within normal timeframe.
    /// </summary>
    Medium = 3,

    /// <summary>
    /// Low level - lowest priority task.
    /// Can be completed when resources are available.
    /// </summary>
    Low = 4
}
