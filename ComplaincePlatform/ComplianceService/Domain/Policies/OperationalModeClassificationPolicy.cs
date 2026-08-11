using ComplianceService.Domain;
using Shared.Infrastructure.Domain.Enums;

namespace ComplianceService.Domain.Policies;

/// <summary>
/// Core business rules for which objective-element classifications are in scope per <see cref="OperationalMode"/>.
/// Workspace scope (what the hospital sees/edits) and decision-rule scope (what accreditation rules evaluate) are modeled separately.
/// </summary>
public sealed class OperationalModeClassificationPolicy : IOperationalModeClassificationPolicy
{
    // --- Workspace: shared immutable arrays (reference-stable) ---
    /// <summary>Core + Commitment + Achievement + Excellence — self-assessment and re-accreditation full breadth.</summary>
    private static readonly ObjectiveElementClassification[] WorkspaceFullFour =
    [
        ObjectiveElementClassification.Core,
        ObjectiveElementClassification.Commitment,
        ObjectiveElementClassification.Achievement,
        ObjectiveElementClassification.Excellence
    ];

    /// <summary>Core + Commitment + Achievement — internal audit and surveillance workspace.</summary>
    private static readonly ObjectiveElementClassification[] WorkspaceCoreCommitmentAchievement =
    [
        ObjectiveElementClassification.Core,
        ObjectiveElementClassification.Commitment,
        ObjectiveElementClassification.Achievement
    ];

    /// <summary>Core + Commitment — final audit workspace (strictest visible subset).</summary>
    private static readonly ObjectiveElementClassification[] WorkspaceFinalTwo =
    [
        ObjectiveElementClassification.Core,
        ObjectiveElementClassification.Commitment
    ];

    // --- Decision rules (RuleEngine): shared immutable arrays ---
    /// <summary>Core + Commitment — self-assessment, internal audit, and final audit decision evaluation.</summary>
    private static readonly ObjectiveElementClassification[] DecisionCoreCommitment =
    [
        ObjectiveElementClassification.Core,
        ObjectiveElementClassification.Commitment
    ];

    /// <summary>Core + Commitment + Achievement — surveillance decision evaluation.</summary>
    private static readonly ObjectiveElementClassification[] DecisionSurveillance =
    [
        ObjectiveElementClassification.Core,
        ObjectiveElementClassification.Commitment,
        ObjectiveElementClassification.Achievement
    ];

    /// <summary>All four — re-accreditation decision evaluation.</summary>
    private static readonly ObjectiveElementClassification[] DecisionReAccreditation =
    [
        ObjectiveElementClassification.Core,
        ObjectiveElementClassification.Commitment,
        ObjectiveElementClassification.Achievement,
        ObjectiveElementClassification.Excellence
    ];

    /// <inheritdoc />
    public IReadOnlyList<ObjectiveElementClassification> GetClassificationsForWorkspace(OperationalMode mode) =>
        mode switch
        {
            // Self-assessment: full OE tree for hospital preparation.
            OperationalMode.SelfAssessment => WorkspaceFullFour,
            // Internal audit: core through achievement (no excellence in this phase).
            OperationalMode.InternalAudit => WorkspaceCoreCommitmentAchievement,
            // Final audit: regulator focus on core + commitment only.
            OperationalMode.FinalAudit => WorkspaceFinalTwo,
            // Surveillance: same breadth as internal audit for ongoing monitoring.
            OperationalMode.Surveillance => WorkspaceCoreCommitmentAchievement,
            // Re-accreditation: full maturity spectrum including excellence.
            OperationalMode.ReAccreditation => WorkspaceFullFour,
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unknown OperationalMode value.")
        };

    /// <inheritdoc />
    public IReadOnlyList<ObjectiveElementClassification> GetClassificationsForDecisionRules(OperationalMode mode) =>
        mode switch
        {
            OperationalMode.SelfAssessment => DecisionCoreCommitment,
            OperationalMode.InternalAudit => DecisionCoreCommitment,
            OperationalMode.FinalAudit => DecisionCoreCommitment,
            OperationalMode.Surveillance => DecisionSurveillance,
            OperationalMode.ReAccreditation => DecisionReAccreditation,
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unknown OperationalMode value.")
        };

    /// <inheritdoc />
    public bool IsClassificationEditable(OperationalMode mode, ObjectiveElementClassification classification)
    {
        var allowed = GetClassificationsForWorkspace(mode);
        return allowed.Contains(classification);
    }
}
