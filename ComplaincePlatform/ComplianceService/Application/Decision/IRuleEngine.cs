using ComplianceService.Application.Decision.Models;
using Shared.Infrastructure.Domain.Enums;

namespace ComplianceService.Application.Decision
{
    public interface IRuleEngine
    {
        Task<DecisionResult> EvaluateAsync(
            Guid frameworkId,
            Guid journeyId,
            OperationalMode operationalMode,
            CancellationToken cancellationToken = default);
    }
}
