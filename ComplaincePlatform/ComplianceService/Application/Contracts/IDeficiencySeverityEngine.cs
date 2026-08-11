using ComplianceService.Domain;
using ComplianceService.Domain.Enums;

namespace ComplianceService.Application.Contracts;

public interface IDeficiencySeverityEngine
{
    /// <summary>
    /// Deterministically calculates deficiency severity from type, maturity stage, and risk category.
    /// No AI — pure rule table.
    /// </summary>
    DeficiencySeverity CalculateSeverity(
        DeficiencyType deficiencyType,
        ObjectiveElementClassification maturityLevel,
        string? riskCategory);
}
