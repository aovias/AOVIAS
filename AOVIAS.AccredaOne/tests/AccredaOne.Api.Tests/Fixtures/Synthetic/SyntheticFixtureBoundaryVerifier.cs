namespace AccredaOne.Api.Tests.Fixtures.Synthetic;

/// <summary>
/// Test-only boundary checks for framework isolation and query scenario outcomes (DEV-06A).
/// </summary>
public static class SyntheticFixtureBoundaryVerifier
{
    public static IReadOnlyList<SyntheticFrameworkRecord> GetEligibleFrameworks(SyntheticFixturePack pack) =>
        pack.Frameworks
            .Where(f => string.Equals(f.EligibilityStatus, "eligible", StringComparison.OrdinalIgnoreCase))
            .OrderBy(f => f.Code, StringComparer.Ordinal)
            .ToList();

    public static IReadOnlyList<SyntheticRequirementRecord> GetCurrentRequirementsForFramework(
        SyntheticFixturePack pack,
        string frameworkId) =>
        pack.Requirements
            .Where(r => string.Equals(r.FrameworkId, frameworkId, StringComparison.Ordinal))
            .OrderBy(r => r.Id, StringComparer.Ordinal)
            .ToList();

    public static SyntheticQueryResolution ResolveQueryScenario(
        SyntheticFixturePack pack,
        SyntheticQueryScenarioRecord scenario)
    {
        var eligibleByCode = GetEligibleFrameworks(pack).ToDictionary(f => f.Code, StringComparer.OrdinalIgnoreCase);

        return scenario.ExpectedOutcome switch
        {
            "bounded_results" when !string.IsNullOrWhiteSpace(scenario.FrameworkCode) &&
                                   eligibleByCode.TryGetValue(scenario.FrameworkCode, out var framework) =>
                new SyntheticQueryResolution
                {
                    Outcome = "bounded_results",
                    FrameworkId = framework.Id,
                    RequirementIds = GetCurrentRequirementsForFramework(pack, framework.Id).Select(r => r.Id).ToList(),
                },
            "clarification_required" =>
                new SyntheticQueryResolution
                {
                    Outcome = "clarification_required",
                    ClarificationReason = "No eligible framework was selected.",
                    RequirementIds = [],
                },
            "no_results" =>
                new SyntheticQueryResolution
                {
                    Outcome = "no_results",
                    RequirementIds = [],
                },
            _ => throw new InvalidOperationException($"Unsupported scenario outcome '{scenario.ExpectedOutcome}'."),
        };
    }
}
