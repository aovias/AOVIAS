using ComplianceService.Application.Contracts;
using ComplianceService.Domain;
using ComplianceService.Domain.Enums;

namespace ComplianceService.Application.Services;

/// <summary>
/// Deterministic severity matrix — no AI, no ML.
/// Priority: RiskCategory keyword match > DeficiencyType + MaturityLevel rules.
/// </summary>
public class DeficiencySeverityEngine : IDeficiencySeverityEngine
{
    private static readonly HashSet<string> CriticalRiskKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "patient", "medication", "surgical", "surgery", "fire", "emergency", "infection",
        "blood", "anaesthesia", "anesthesia", "resuscitation", "critical care",
    };

    public DeficiencySeverity CalculateSeverity(
        DeficiencyType deficiencyType,
        ObjectiveElementClassification maturityLevel,
        string? riskCategory)
    {
        // Priority 1: Critical risk category keywords override the matrix.
        if (!string.IsNullOrWhiteSpace(riskCategory)
         && CriticalRiskKeywords.Any(kw =>
                riskCategory.Contains(kw, StringComparison.OrdinalIgnoreCase)))
        {
            return DeficiencySeverity.Critical;
        }

        // Priority 2: Maturity deficiency — always one level above the default for the stage.
        if (deficiencyType == DeficiencyType.Maturity)
        {
            return maturityLevel switch
            {
                ObjectiveElementClassification.Core       => DeficiencySeverity.Critical,
                ObjectiveElementClassification.Commitment => DeficiencySeverity.Major,
                ObjectiveElementClassification.Achievement => DeficiencySeverity.Moderate,
                ObjectiveElementClassification.Excellence => DeficiencySeverity.Minor,
                _ => DeficiencySeverity.Major
            };
        }

        // Priority 3: Core-stage deficiencies
        if (maturityLevel == ObjectiveElementClassification.Core)
        {
            return deficiencyType switch
            {
                DeficiencyType.Assessment    => DeficiencySeverity.Critical,
                DeficiencyType.Verification  => DeficiencySeverity.Critical,
                DeficiencyType.Evidence      => DeficiencySeverity.Major,
                DeficiencyType.Implementation => DeficiencySeverity.Major,
                _ => DeficiencySeverity.Major
            };
        }

        // Priority 4: Stage-based defaults
        return maturityLevel switch
        {
            ObjectiveElementClassification.Commitment => DeficiencySeverity.Major,
            ObjectiveElementClassification.Achievement => DeficiencySeverity.Moderate,
            ObjectiveElementClassification.Excellence => DeficiencySeverity.Minor,
            _ => DeficiencySeverity.Moderate
        };
    }
}
