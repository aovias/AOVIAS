using ComplianceService.Domain;

namespace ComplianceService.Application.Contracts;

/// <summary>
/// Factory for resolving framework-specific journey policies by framework code.
/// </summary>
public interface IFrameworkJourneyPolicyFactory
{
    IFrameworkJourneyPolicy GetPolicy(string frameworkCode);
}

