using ComplianceService.Application.ComplianceScoring;

namespace ComplianceService.Application.Contracts;

public interface IScoringParityService
{
    Task<ScoringParityResult> CompareAsync(
        Guid hospitalId,
        Guid frameworkId,
        Guid? journeyId,
        CancellationToken cancellationToken = default);
}
