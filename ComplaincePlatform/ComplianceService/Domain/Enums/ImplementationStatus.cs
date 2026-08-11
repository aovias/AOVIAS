namespace ComplianceService.Domain.Enums;

/// <summary>
/// Tracks how far an organization has progressed in implementing an Objective Element requirement.
/// </summary>
public enum ImplementationStatus
{
    Unknown = 0,
    NotStarted = 1,
    Implemented = 2,
    PartiallyImplemented = 3,
    NotImplemented = 4,
    Expired = 5
}
