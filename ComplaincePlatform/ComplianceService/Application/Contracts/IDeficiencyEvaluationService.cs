using ComplianceService.Domain;

namespace ComplianceService.Application.Contracts;

public interface IDeficiencyEvaluationService
{
    /// <summary>
    /// Evaluates the given obligation and upserts ComplianceDeficiency records:
    /// - Creates new deficiencies for newly triggered rules.
    /// - Updates existing open deficiencies (severity recalc).
    /// - Closes deficiencies whose rules are no longer triggered.
    /// Domain events are dispatched inside this method.
    /// </summary>
    Task<IReadOnlyList<ComplianceDeficiency>> EvaluateAsync(
        OrganizationComplianceObligation obligation,
        CancellationToken ct = default);
}
