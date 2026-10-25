using AuditService.Domain.Enums;
using ComplianceService.Domain;
using ComplianceService.Domain.Enums;

namespace AuditService.Application.Utilities;

/// <summary>
/// Utility class for mapping objective elements and risks to audit non-conformance types.
/// </summary>
public static class NonConformanceMapper
{
    /// <summary>
    /// Maps an incomplete objective element to an audit non-conformance type using classification.
    /// </summary>
    public static AuditNonConformanceType MapFromObjectiveElement(ObjectiveElement oe, bool isIncomplete)
    {
        if (oe == null)
            throw new ArgumentNullException(nameof(oe));

        if (!isIncomplete)
        {
            return AuditNonConformanceType.OFI;
        }

        return oe.Classification switch
        {
            ObjectiveElementClassification.Core => AuditNonConformanceType.MajorNC,
            ObjectiveElementClassification.Commitment => AuditNonConformanceType.MinorNC,
            ObjectiveElementClassification.Achievement => AuditNonConformanceType.Observation,
            ObjectiveElementClassification.Excellence => AuditNonConformanceType.OFI,
            _ => AuditNonConformanceType.Observation
        };
    }

    /// <summary>
    /// Maps a compliance severity to an audit non-conformance type for incomplete items.
    /// </summary>
    public static AuditNonConformanceType MapFromComplianceSeverity(ComplianceSeverity severity, bool isIncomplete)
    {
        if (!isIncomplete)
        {
            return AuditNonConformanceType.OFI;
        }

        return severity switch
        {
            ComplianceSeverity.Critical => AuditNonConformanceType.MajorNC,
            ComplianceSeverity.Major => AuditNonConformanceType.MinorNC,
            ComplianceSeverity.Minor => AuditNonConformanceType.Observation,
            ComplianceSeverity.Informational => AuditNonConformanceType.OFI,
            _ => AuditNonConformanceType.Observation
        };
    }

    /// <summary>
    /// Maps a risk to an audit non-conformance type based on its severity and status.
    /// </summary>
    public static AuditNonConformanceType? MapFromRisk(Risk risk)
    {
        if (risk == null)
            throw new ArgumentNullException(nameof(risk));

        if (!risk.Status.IsActive())
        {
            return null;
        }

        return risk.Severity switch
        {
            ComplianceSeverity.Critical => AuditNonConformanceType.MajorNC,
            ComplianceSeverity.Major => AuditNonConformanceType.MinorNC,
            ComplianceSeverity.Minor => null,
            ComplianceSeverity.Informational => null,
            _ => null
        };
    }

    /// <summary>
    /// Maps a risk severity and status to an audit non-conformance type.
    /// </summary>
    public static AuditNonConformanceType? MapFromRiskSeverityAndStatus(ComplianceSeverity severity, RiskStatus status)
    {
        if (!status.IsActive())
        {
            return null;
        }

        return severity switch
        {
            ComplianceSeverity.Critical => AuditNonConformanceType.MajorNC,
            ComplianceSeverity.Major => AuditNonConformanceType.MinorNC,
            ComplianceSeverity.Minor => null,
            ComplianceSeverity.Informational => null,
            _ => null
        };
    }
}
