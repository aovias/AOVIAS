namespace ComplianceService.Domain.Enums;

/// <summary>
/// Represents the severity level of a compliance requirement or standard.
/// Used to classify the criticality of compliance risks and related items.
/// </summary>
public enum ComplianceSeverity : int
{
    /// <summary>
    /// Critical severity - highest priority compliance requirement.
    /// Non-compliance may result in regulatory action or significant risk.
    /// </summary>
    Critical = 1,

    /// <summary>
    /// Major severity - high priority compliance requirement.
    /// Non-compliance may result in audit findings or moderate risk.
    /// </summary>
    Major = 2,

    /// <summary>
    /// Minor severity - medium priority compliance requirement.
    /// Non-compliance may result in observations or low risk.
    /// </summary>
    Minor = 3,

    /// <summary>
    /// Informational severity - lowest priority compliance requirement.
    /// Non-compliance is noted but does not pose significant risk.
    /// </summary>
    Informational = 4
}
