namespace ComplianceService.Domain;

/// <summary>
/// Work completion state for a single Objective Element within scoring (replaces requirement/task indirection).
/// </summary>
public record ObjectiveElementWorkSnapshot(
    Guid ObjectiveElementId,
    Guid ChapterId,
    string Status,
    decimal Weight);
