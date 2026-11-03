namespace AccredaOne.Api.Tests.Fixtures.Synthetic;

/// <summary>
/// Test-only boundary checks for framework isolation and query scenario outcomes (DEV-06A).
/// </summary>
public static class SyntheticFixtureBoundaryVerifier
{
    public const string OutcomeBoundedResults = "bounded_results";

    public const string OutcomeBoundedCrossFrameworkResults = "bounded_cross_framework_results";

    public const string OutcomeNoResults = "no_results";

    public static IReadOnlyList<SyntheticFrameworkRecord> GetEligibleFrameworks(SyntheticFixturePack pack) =>
        pack.Frameworks
            .Where(f => string.Equals(f.EligibilityStatus, "eligible", StringComparison.OrdinalIgnoreCase))
            .OrderBy(f => f.Code, StringComparer.Ordinal)
            .ToList();

    public static SyntheticFrameworkRecord? FindFrameworkByCode(SyntheticFixturePack pack, string frameworkCode) =>
        pack.Frameworks.FirstOrDefault(f => string.Equals(f.Code, frameworkCode, StringComparison.OrdinalIgnoreCase));

    public static IReadOnlyList<SyntheticRequirementRecord> GetCurrentRequirementsForFramework(
        SyntheticFixturePack pack,
        string frameworkId) =>
        pack.Requirements
            .Where(r => string.Equals(r.FrameworkId, frameworkId, StringComparison.Ordinal))
            .OrderBy(r => r.Id, StringComparer.Ordinal)
            .ToList();

    public static IReadOnlyDictionary<string, IReadOnlyList<SyntheticRequirementRecord>> GroupEligibleRequirementsByFramework(
        SyntheticFixturePack pack)
    {
        var result = new Dictionary<string, IReadOnlyList<SyntheticRequirementRecord>>(StringComparer.Ordinal);
        foreach (var framework in GetEligibleFrameworks(pack))
        {
            result[framework.Code] = GetCurrentRequirementsForFramework(pack, framework.Id);
        }

        return result;
    }

    public static SyntheticFrameworkVersionRecord? GetVerifiedCurrentVersionForFramework(
        SyntheticFixturePack pack,
        string frameworkId) =>
        pack.FrameworkVersions.SingleOrDefault(v =>
            string.Equals(v.FrameworkId, frameworkId, StringComparison.Ordinal) &&
            IsVerifiedCurrentVersionStatus(v.VersionStatus));

    public static bool IsVerifiedCurrentVersionStatus(string versionStatus) =>
        string.Equals(versionStatus, "current", StringComparison.OrdinalIgnoreCase);

    public static bool IsApplicableCurrentVersion(SyntheticFrameworkVersionRecord version) =>
        IsVerifiedCurrentVersionStatus(version.VersionStatus);

    public static SyntheticQueryResolution ResolveQueryScenario(
        SyntheticFixturePack pack,
        SyntheticQueryScenarioRecord scenario)
    {
        var scenarioType = scenario.ScenarioType ?? string.Empty;

        if (string.Equals(scenarioType, "explicit_framework", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(scenario.FrameworkCode))
            {
                return NoResults();
            }

            var framework = FindFrameworkByCode(pack, scenario.FrameworkCode);
            if (framework is null)
            {
                return NoResults();
            }

            if (!string.Equals(framework.EligibilityStatus, "eligible", StringComparison.OrdinalIgnoreCase))
            {
                return NoResults();
            }

            return new SyntheticQueryResolution
            {
                Outcome = OutcomeBoundedResults,
                FrameworkId = framework.Id,
                RequirementIds = GetCurrentRequirementsForFramework(pack, framework.Id).Select(r => r.Id).ToList(),
            };
        }

        if (string.Equals(scenarioType, "no_framework_selection", StringComparison.OrdinalIgnoreCase))
        {
            var grouped = GroupEligibleRequirementsByFramework(pack);
            var requirementIdsByFrameworkCode = grouped.ToDictionary(
                kvp => kvp.Key,
                kvp => (IReadOnlyList<string>)kvp.Value.Select(r => r.Id).ToList(),
                StringComparer.Ordinal);

            var requirementIds = grouped.Keys
                .OrderBy(code => code, StringComparer.Ordinal)
                .SelectMany(code => requirementIdsByFrameworkCode[code])
                .ToList();

            return new SyntheticQueryResolution
            {
                Outcome = OutcomeBoundedCrossFrameworkResults,
                RequirementIds = requirementIds,
                RequirementIdsByFrameworkCode = requirementIdsByFrameworkCode,
            };
        }

        if (string.Equals(scenarioType, "ambiguous_or_unsupported", StringComparison.OrdinalIgnoreCase))
        {
            return NoResults();
        }

        return NoResults();
    }

    private static SyntheticQueryResolution NoResults() =>
        new()
        {
            Outcome = OutcomeNoResults,
            RequirementIds = [],
        };
}
