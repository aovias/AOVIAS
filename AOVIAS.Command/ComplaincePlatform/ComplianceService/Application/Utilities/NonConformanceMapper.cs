using ComplianceService.Domain;
using ComplianceService.Domain.Enums;
using Shared.Infrastructure.Domain.Enums;

namespace ComplianceService.Application.Utilities;

/// <summary>
/// Utility class for mapping risks to audit non-conformance types.
/// </summary>
public static class NonConformanceMapper
{
    /// <summary>
    /// Maps a risk to an audit non-conformance type based on its severity and status.
    /// </summary>
    /// <param name="risk">The risk to evaluate. Must not be null.</param>
    /// <returns>The corresponding audit non-conformance type, or null if the risk does not map to a non-conformance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when risk is null.</exception>
    /// <remarks>
    /// Mapping rules:
    /// - Risk with Severity Critical and Status Open → MajorNC
    /// - Risk with Severity Major and Status Open → MinorNC
    /// - Other combinations (Mitigated, Closed, or other severities) → null (no non-conformance)
    /// </remarks>
    public static NonConformanceType? MapFromRisk(Risk risk)
    {
        if (risk == null)
            throw new ArgumentNullException(nameof(risk));

        // Only map active (non-closed) register risks
        if (!risk.Status.IsActive())
        {
            return null;
        }

        return risk.Severity switch
        {
            ComplianceSeverity.Critical => NonConformanceType.MajorNC,
            ComplianceSeverity.Major => NonConformanceType.MinorNC,
            ComplianceSeverity.Minor => null, // Minor risks don't map to non-conformances
            ComplianceSeverity.Informational => null, // Informational risks don't map to non-conformances
            _ => null // Default: no non-conformance
        };
    }

    /// <summary>
    /// Maps a risk severity and status to an audit non-conformance type.
    /// </summary>
    /// <param name="severity">The severity of the risk.</param>
    /// <param name="status">The status of the risk.</param>
    /// <returns>The corresponding audit non-conformance type, or null if the risk does not map to a non-conformance.</returns>
    /// <remarks>
    /// Mapping rules:
    /// - Risk with Severity Critical and Status Open → MajorNC
    /// - Risk with Severity Major and Status Open → MinorNC
    /// - Other combinations → null (no non-conformance)
    /// </remarks>
    public static NonConformanceType? MapFromRiskSeverityAndStatus(ComplianceSeverity severity, RiskStatus status)
    {
        if (!status.IsActive())
        {
            return null;
        }

        return severity switch
        {
            ComplianceSeverity.Critical => NonConformanceType.MajorNC,
            ComplianceSeverity.Major => NonConformanceType.MinorNC,
            ComplianceSeverity.Minor => null, // Minor risks don't map to non-conformances
            ComplianceSeverity.Informational => null, // Informational risks don't map to non-conformances
            _ => null // Default: no non-conformance
        };
    }
}
