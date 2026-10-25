using ComplianceService.Domain;
using Shared.Infrastructure.Domain.Enums;

namespace ComplianceService.Application.Journey;

/// <summary>
/// Resolves operational mode from query params; rejects inconsistent pairs (spec §6).
/// </summary>
public static class OperationalModeQuery
{
    /// <summary>
    /// When both query params are absent, uses <see cref="ComplianceJourney.OperationalMode"/>.
    /// </summary>
    public static OperationalMode ResolveForJourney(
        ComplianceJourney journey,
        OperationalMode? operationalMode,
        string? auditLifecycleMode)
    {
        OperationalMode? fromString = null;
        if (!string.IsNullOrWhiteSpace(auditLifecycleMode))
        {
            fromString = ParseAuditLifecycleMode(auditLifecycleMode.Trim());
            if (fromString == null)
            {
                throw new ArgumentException("Invalid auditLifecycleMode.", nameof(auditLifecycleMode));
            }
        }

        if (operationalMode.HasValue && fromString.HasValue && operationalMode.Value != fromString.Value)
        {
            throw new ArgumentException("operationalMode and auditLifecycleMode disagree.");
        }

        if (operationalMode.HasValue)
        {
            return operationalMode.Value;
        }

        if (fromString.HasValue)
        {
            return fromString.Value;
        }

        return journey.OperationalMode;
    }

    private static OperationalMode? ParseAuditLifecycleMode(string s)
    {
        return s.ToLowerInvariant() switch
        {
            "self_assessment" or "selfassessment" => OperationalMode.SelfAssessment,
            "internal_audit" or "internalaudit" => OperationalMode.InternalAudit,
            "final_audit" or "finalaudit" => OperationalMode.FinalAudit,
            "surveillance" => OperationalMode.Surveillance,
            "re_accreditation" or "reaccreditation" => OperationalMode.ReAccreditation,
            _ => null
        };
    }
}
