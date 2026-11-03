namespace AccredaOne.Api.DevelopmentSynthetic;

internal static class DevelopmentSyntheticQueryResolver
{
    public const string OutcomeBoundedResults = "bounded_results";
    public const string OutcomeBoundedCrossFrameworkResults = "bounded_cross_framework_results";
    public const string OutcomeNoResults = "no_results";

    public static IReadOnlyList<DevelopmentSyntheticFrameworkRecord> GetEligibleFrameworks(DevelopmentSyntheticDataset dataset) =>
        dataset.Frameworks
            .Where(f => string.Equals(f.EligibilityStatus, "eligible", StringComparison.OrdinalIgnoreCase))
            .OrderBy(f => f.Code, StringComparer.Ordinal)
            .ToList();

    public static DevelopmentSyntheticFrameworkRecord? FindFrameworkByCode(DevelopmentSyntheticDataset dataset, string frameworkCode) =>
        dataset.Frameworks.FirstOrDefault(f => string.Equals(f.Code, frameworkCode, StringComparison.OrdinalIgnoreCase));

    public static IReadOnlyList<DevelopmentSyntheticRequirementRecord> GetCurrentRequirementsForFramework(
        DevelopmentSyntheticDataset dataset,
        string frameworkId) =>
        dataset.Requirements
            .Where(r => string.Equals(r.FrameworkId, frameworkId, StringComparison.Ordinal))
            .OrderBy(r => r.Id, StringComparer.Ordinal)
            .ToList();

    public static DevelopmentSyntheticResolution Resolve(string? frameworkCode, DevelopmentSyntheticDataset dataset)
    {
        if (!string.IsNullOrWhiteSpace(frameworkCode))
        {
            var framework = FindFrameworkByCode(dataset, frameworkCode);
            if (framework is null ||
                !string.Equals(framework.EligibilityStatus, "eligible", StringComparison.OrdinalIgnoreCase))
            {
                return new DevelopmentSyntheticResolution { Outcome = OutcomeNoResults };
            }

            return new DevelopmentSyntheticResolution
            {
                Outcome = OutcomeBoundedResults,
                FrameworkIds = [framework.Id],
            };
        }

        var eligible = GetEligibleFrameworks(dataset);
        return new DevelopmentSyntheticResolution
        {
            Outcome = OutcomeBoundedCrossFrameworkResults,
            FrameworkIds = eligible.Select(f => f.Id).ToList(),
        };
    }
}
