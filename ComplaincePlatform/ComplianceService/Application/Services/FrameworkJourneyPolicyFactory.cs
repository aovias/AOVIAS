using ComplianceService.Application.Contracts;
using ComplianceService.Domain;

namespace ComplianceService.Application.Services;

/// <summary>
/// Default implementation of IFrameworkJourneyPolicyFactory that selects a
/// concrete policy based on framework code. Falls back to DefaultJourneyPolicy.
/// </summary>
public class FrameworkJourneyPolicyFactory : IFrameworkJourneyPolicyFactory
{
    private readonly IReadOnlyDictionary<string, IFrameworkJourneyPolicy> _policiesByCode;
    private readonly IFrameworkJourneyPolicy _defaultPolicy;

    public FrameworkJourneyPolicyFactory(IEnumerable<IFrameworkJourneyPolicy> policies)
    {
        // Index policies by FrameworkCode (case-insensitive)
        _policiesByCode = policies
            .Where(p => !string.IsNullOrWhiteSpace(p.FrameworkCode))
            .ToDictionary(
                p => p.FrameworkCode,
                p => p,
                StringComparer.OrdinalIgnoreCase);

        _defaultPolicy = _policiesByCode.TryGetValue("DEFAULT", out var def)
            ? def
            : new DefaultJourneyPolicy();
    }

    public IFrameworkJourneyPolicy GetPolicy(string frameworkCode)
    {
        if (string.IsNullOrWhiteSpace(frameworkCode))
        {
            return _defaultPolicy;
        }

        return _policiesByCode.TryGetValue(frameworkCode, out var policy)
            ? policy
            : _defaultPolicy;
    }
}

