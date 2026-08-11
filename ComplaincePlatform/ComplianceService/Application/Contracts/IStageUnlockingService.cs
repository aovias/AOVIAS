using ComplianceService.Application.DTOs;
using ComplianceService.Application.StageUnlock;
using ComplianceService.Domain;
using ComplianceService.Domain.Enums;

namespace ComplianceService.Application.Contracts;

/// <summary>
/// Automated PRAMAN maturity stage unlocking — consumes obligations, deficiencies, tasks, and verified evidence.
/// </summary>
public interface IStageUnlockingService
{
    Task<IReadOnlyList<StageUnlockEvaluationResult>> EvaluateAndPersistAsync(
        Guid journeyId,
        string triggerSource,
        Guid? triggeredByUserId = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StageUnlockEvaluationResult>> GetStageEvaluationsAsync(
        Guid hospitalId,
        Guid organizationId,
        Guid frameworkId,
        CancellationToken cancellationToken = default);

    Task<bool> IsStageAccessibleAsync(
        Guid journeyId,
        ObjectiveElementClassification stage,
        CancellationToken cancellationToken = default);

    Task<StageUnlockEvaluationResult?> GetStageEvaluationAsync(
        Guid journeyId,
        ObjectiveElementClassification stage,
        CancellationToken cancellationToken = default);
}
