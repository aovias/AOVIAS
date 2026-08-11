namespace AuditService.Domain.Enums;

/// <summary>
/// Represents the type of non-conformance identified during an audit.
/// Used to classify audit findings according to standard audit terminology.
/// </summary>
public enum AuditNonConformanceType : int
{
    /// <summary>
    /// Major Non-Conformance (Major NC).
    /// Significant deviation from compliance requirements that poses substantial risk.
    /// Requires immediate corrective action and may impact certification or accreditation.
    /// </summary>
    MajorNC = 1,

    /// <summary>
    /// Minor Non-Conformance (Minor NC).
    /// Minor deviation from compliance requirements that poses moderate risk.
    /// Requires corrective action within specified timeframe.
    /// </summary>
    MinorNC = 2,

    /// <summary>
    /// Observation.
    /// Noted deviation that does not constitute a non-conformance but should be addressed.
    /// Represents an area for improvement without immediate risk.
    /// </summary>
    Observation = 3,

    /// <summary>
    /// Opportunity For Improvement (OFI).
    /// Suggestion for enhancement that is not a compliance requirement.
    /// Represents best practice recommendations.
    /// </summary>
    OFI = 4
}
