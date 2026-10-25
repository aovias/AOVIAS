namespace ComplianceService.Domain;

/// <summary>
/// Simple severity tags for gap detection results.
/// No scoring - just categorical severity levels.
/// </summary>
public enum GapSeverity
{
    /// <summary>
    /// Critical gap that must be addressed immediately.
    /// </summary>
    Critical,

    /// <summary>
    /// High priority gap that should be addressed soon.
    /// </summary>
    High,

    /// <summary>
    /// Medium priority gap that should be addressed.
    /// </summary>
    Medium,

    /// <summary>
    /// Low priority gap that can be addressed later.
    /// </summary>
    Low
}
