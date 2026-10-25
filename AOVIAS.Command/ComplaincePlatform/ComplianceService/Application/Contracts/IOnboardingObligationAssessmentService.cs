using ComplianceService.Application.DTOs;

namespace ComplianceService.Application.Contracts;

/// <summary>
/// Applies onboarding wizard assessment answers to OrganizationComplianceObligation records
/// and triggers deficiency evaluation for each updated obligation.
/// </summary>
public interface IOnboardingObligationAssessmentService
{
    /// <summary>
    /// Applies wizard assessment answers to obligations for the given hospital × framework.
    /// When <paramref name="answers"/> is null or empty, loads the latest saved wizard assessment.
    /// Idempotent — safe to call multiple times.
    /// </summary>
    Task<int> ApplyAsync(
        Guid organizationId,
        Guid hospitalId,
        Guid frameworkId,
        IReadOnlyList<WizardAssessmentAnswerItemDto>? answers = null,
        CancellationToken ct = default);

    /// <summary>
    /// Applies onboarding assessment only when obligations for the framework have not yet
    /// been updated from a wizard assessment (all matching OEs still NotAssessed).
    /// </summary>
    Task ApplyIfPendingAsync(
        Guid hospitalId,
        Guid frameworkId,
        CancellationToken ct = default);
}
