using ComplianceService.Application.Decision.Models;
using Shared.Infrastructure.Domain.Enums;

namespace ComplianceService.Application.Decision
{
    public class DecisionService
    {
        private readonly IRuleEngine _ruleEngine;

        public DecisionService(IRuleEngine ruleEngine)
        {
            _ruleEngine = ruleEngine;
        }

        public Task<DecisionResult> EvaluateAsync(Guid frameworkId, Guid journeyId, OperationalMode operationalMode, CancellationToken cancellationToken = default)
        {
            return _ruleEngine.EvaluateAsync(frameworkId, journeyId, operationalMode, cancellationToken);
        }
    }
}
