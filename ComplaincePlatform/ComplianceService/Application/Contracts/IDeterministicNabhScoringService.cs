using ComplianceService.Application.ComplianceScoring;

namespace ComplianceService.Application.Contracts;

public interface IDeterministicNabhScoringService
{
    Task<DeterministicNabhScoreResult> EvaluateAsync(
        Guid hospitalId,
        Guid frameworkId,
        Guid? journeyId,
        CancellationToken cancellationToken = default);
}
