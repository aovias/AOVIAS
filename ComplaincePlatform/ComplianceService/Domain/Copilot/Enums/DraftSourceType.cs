namespace ComplianceService.Domain.Copilot.Enums;

/// <summary>
/// Identifies the primary source that triggered or informed draft generation.
/// Supports traceability and Source Intelligence metadata construction.
/// </summary>
public enum DraftSourceType
{
    /// <summary>Draft was triggered from a Copilot recommendation.</summary>
    Recommendation = 0,

    /// <summary>Draft was triggered from a specific compliance deficiency.</summary>
    Deficiency = 1,

    /// <summary>Draft was triggered from a CAPA item.</summary>
    Capa = 2,

    /// <summary>Draft was triggered from analytics data (KPIs, trends, readiness scores).</summary>
    Analytics = 3,

    /// <summary>Draft was triggered from compliance maturity / stage unlock data.</summary>
    Maturity = 4,

    /// <summary>Draft was manually requested by a user with no specific source entity.</summary>
    Manual = 5,
}
