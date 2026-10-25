using ComplianceService.Domain;
using Shared.Infrastructure.Domain.Enums;

namespace ComplianceService.Domain.Policies;

/// <summary>
/// Defines which objective-element classifications apply for a given <see cref="OperationalMode"/>
/// for workspace UX vs accreditation decision rules (may differ by design).
/// </summary>
public interface IOperationalModeClassificationPolicy
{
    /// <summary>
    /// Classifications included in journey workspace aggregates and editable OE scope.
    /// </summary>
    IReadOnlyList<ObjectiveElementClassification> GetClassificationsForWorkspace(OperationalMode mode);

    /// <summary>
    /// Classifications included when evaluating accreditation decision rules (RuleEngine).
    /// </summary>
    IReadOnlyList<ObjectiveElementClassification> GetClassificationsForDecisionRules(OperationalMode mode);

    /// <summary>
    /// Whether an OE classification may be edited in the workspace for this mode.
    /// </summary>
    bool IsClassificationEditable(OperationalMode mode, ObjectiveElementClassification classification);
}
