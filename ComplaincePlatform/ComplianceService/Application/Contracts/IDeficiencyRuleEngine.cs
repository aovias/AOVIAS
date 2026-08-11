using ComplianceService.Domain;
using ComplianceService.Domain.Enums;

namespace ComplianceService.Application.Contracts;

/// <summary>Result of a single deficiency rule evaluation.</summary>
public class DeficiencyRuleResult
{
    public DeficiencyType DeficiencyType { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public interface IDeficiencyRuleEngine
{
    /// <summary>
    /// Returns the list of deficiency types triggered by the current obligation state.
    /// An empty list means the obligation is fully compliant — no deficiencies.
    /// </summary>
    Task<IReadOnlyList<DeficiencyRuleResult>> GetApplicableRulesAsync(
        OrganizationComplianceObligation obligation,
        CancellationToken ct = default);
}
