using System.Text.Json;

namespace AccredaOne.Api.Tests.Fixtures.Synthetic;

public static class SyntheticFixtureLoader
{
    public const string SyntheticClassification = "synthetic";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Disallow,
        AllowTrailingCommas = false,
    };

    public static SyntheticFixturePack LoadDefault()
    {
        var root = ResolveFixtureRoot();
        return LoadFromDirectory(root);
    }

    public static SyntheticFixturePack LoadFromDirectory(string directoryPath)
    {
        var manifest = DeserializeFile<SyntheticFixtureManifest>(Path.Combine(directoryPath, "manifest.json"));
        var frameworks = DeserializeFile<List<SyntheticFrameworkRecord>>(Path.Combine(directoryPath, "frameworks.json"));
        var sources = DeserializeFile<List<SyntheticSourceRecord>>(Path.Combine(directoryPath, "sources.json"));
        var requirements = DeserializeFile<List<SyntheticRequirementRecord>>(Path.Combine(directoryPath, "requirements.json"));
        var frameworkVersions = DeserializeFile<List<SyntheticFrameworkVersionRecord>>(Path.Combine(directoryPath, "framework-versions.json"));
        var explanations = DeserializeFile<List<SyntheticExplanationRecord>>(Path.Combine(directoryPath, "explanations.json"));
        var queryScenarios = DeserializeFile<List<SyntheticQueryScenarioRecord>>(Path.Combine(directoryPath, "query-scenarios.json"));

        var pack = new SyntheticFixturePack
        {
            Manifest = manifest,
            Frameworks = frameworks,
            Sources = sources,
            Requirements = requirements,
            FrameworkVersions = frameworkVersions,
            Explanations = explanations,
            QueryScenarios = queryScenarios,
        };

        Validate(pack);
        return pack;
    }

    internal static void Validate(SyntheticFixturePack pack)
    {
        var errors = new List<string>();
        var ids = new HashSet<string>(StringComparer.Ordinal);

        void TrackId(string id, string entity)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                errors.Add($"{entity} has an empty id.");
                return;
            }

            if (!ids.Add(id))
            {
                errors.Add($"Duplicate id '{id}' detected.");
            }
        }

        AssertSyntheticRecord(pack.Manifest.Classification, pack.Manifest.Provenance, "manifest", errors);

        var counts = pack.Manifest.ExpectedCounts;
        if (pack.Frameworks.Count != counts.FrameworksTotal)
        {
            errors.Add($"Expected {counts.FrameworksTotal} frameworks, found {pack.Frameworks.Count}.");
        }

        if (pack.Sources.Count != counts.SourceInstruments)
        {
            errors.Add($"Expected {counts.SourceInstruments} sources, found {pack.Sources.Count}.");
        }

        if (pack.FrameworkVersions.Count != counts.FrameworkVersionRecords)
        {
            errors.Add($"Expected {counts.FrameworkVersionRecords} framework version records, found {pack.FrameworkVersions.Count}.");
        }

        if (pack.Explanations.Count != counts.Explanations)
        {
            errors.Add($"Expected {counts.Explanations} explanations, found {pack.Explanations.Count}.");
        }

        if (pack.QueryScenarios.Count != counts.QueryScenarios)
        {
            errors.Add($"Expected {counts.QueryScenarios} query scenarios, found {pack.QueryScenarios.Count}.");
        }

        foreach (var framework in pack.Frameworks)
        {
            TrackId(framework.Id, "framework");
            AssertSyntheticRecord(framework.Classification, framework.Provenance, framework.Id, errors);
        }

        var frameworkIds = pack.Frameworks.Select(f => f.Id).ToHashSet(StringComparer.Ordinal);
        var eligibleFrameworkIds = pack.Frameworks
            .Where(f => string.Equals(f.EligibilityStatus, "eligible", StringComparison.OrdinalIgnoreCase))
            .Select(f => f.Id)
            .ToHashSet(StringComparer.Ordinal);

        if (eligibleFrameworkIds.Count != counts.FrameworksEligible)
        {
            errors.Add($"Expected {counts.FrameworksEligible} eligible frameworks, found {eligibleFrameworkIds.Count}.");
        }

        if (pack.Frameworks.Count(f => string.Equals(f.EligibilityStatus, "disabled", StringComparison.OrdinalIgnoreCase)) != counts.FrameworksIneligible)
        {
            errors.Add($"Expected {counts.FrameworksIneligible} ineligible frameworks.");
        }

        var disabledFrameworkIds = pack.Frameworks
            .Where(f => string.Equals(f.EligibilityStatus, "disabled", StringComparison.OrdinalIgnoreCase))
            .Select(f => f.Id)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var source in pack.Sources)
        {
            TrackId(source.Id, "source");
            AssertSyntheticRecord(source.Classification, source.Provenance, source.Id, errors);
            if (!frameworkIds.Contains(source.FrameworkId))
            {
                errors.Add($"Source '{source.Id}' references unknown framework '{source.FrameworkId}'.");
            }
            else if (disabledFrameworkIds.Contains(source.FrameworkId))
            {
                errors.Add($"Source '{source.Id}' must not attach to disabled framework '{source.FrameworkId}'.");
            }

            if (!string.Equals(source.ContentKind, "source_instrument", StringComparison.Ordinal))
            {
                errors.Add($"Source '{source.Id}' must have contentKind source_instrument.");
            }
        }

        var sourcesByFramework = pack.Sources
            .GroupBy(s => s.FrameworkId)
            .ToDictionary(g => g.Key, g => g.Count(), StringComparer.Ordinal);

        foreach (var frameworkId in eligibleFrameworkIds)
        {
            if (!sourcesByFramework.TryGetValue(frameworkId, out var sourceCount) || sourceCount != 1)
            {
                errors.Add($"Eligible framework '{frameworkId}' must have exactly one source instrument.");
            }
        }

        var sourceIds = pack.Sources.Select(s => s.Id).ToHashSet(StringComparer.Ordinal);
        var requirementCountByFramework = pack.Requirements
            .GroupBy(r => r.FrameworkId)
            .ToDictionary(g => g.Key, g => g.Count(), StringComparer.Ordinal);

        foreach (var frameworkId in eligibleFrameworkIds)
        {
            if (!requirementCountByFramework.TryGetValue(frameworkId, out var count) ||
                count != counts.RequirementsCurrentPerEligibleFramework)
            {
                errors.Add($"Framework '{frameworkId}' must have {counts.RequirementsCurrentPerEligibleFramework} current requirements.");
            }
        }

        foreach (var requirement in pack.Requirements)
        {
            TrackId(requirement.Id, "requirement");
            AssertSyntheticRecord(requirement.Classification, requirement.Provenance, requirement.Id, errors);
            if (!frameworkIds.Contains(requirement.FrameworkId))
            {
                errors.Add($"Requirement '{requirement.Id}' references unknown framework.");
            }
            else if (disabledFrameworkIds.Contains(requirement.FrameworkId))
            {
                errors.Add($"Requirement '{requirement.Id}' must not attach to disabled framework '{requirement.FrameworkId}'.");
            }

            if (!sourceIds.Contains(requirement.SourceInstrumentId))
            {
                errors.Add($"Requirement '{requirement.Id}' references unknown source.");
            }

            if (!string.Equals(requirement.VersionStatus, "current", StringComparison.OrdinalIgnoreCase))
            {
                errors.Add($"Requirement '{requirement.Id}' must be current in this pack.");
            }
        }

        var currentVersionCountByFramework = new Dictionary<string, int>(StringComparer.Ordinal);

        foreach (var version in pack.FrameworkVersions)
        {
            TrackId(version.Id, "framework-version");
            AssertSyntheticRecord(version.Classification, version.Provenance, version.Id, errors);
            if (!frameworkIds.Contains(version.FrameworkId))
            {
                errors.Add($"Version '{version.Id}' references unknown framework.");
            }
            else if (disabledFrameworkIds.Contains(version.FrameworkId))
            {
                errors.Add($"Version '{version.Id}' must not attach to disabled framework '{version.FrameworkId}'.");
            }

            if (SyntheticFixtureBoundaryVerifier.IsVerifiedCurrentVersionStatus(version.VersionStatus))
            {
                currentVersionCountByFramework.TryGetValue(version.FrameworkId, out var count);
                currentVersionCountByFramework[version.FrameworkId] = count + 1;
            }
        }

        foreach (var (frameworkId, currentCount) in currentVersionCountByFramework)
        {
            if (currentCount > 1)
            {
                errors.Add($"Framework '{frameworkId}' must have at most one verified current version record.");
            }
        }

        foreach (var explanation in pack.Explanations)
        {
            TrackId(explanation.Id, "explanation");
            AssertSyntheticRecord(explanation.Classification, explanation.Provenance, explanation.Id, errors);
            if (!frameworkIds.Contains(explanation.FrameworkId))
            {
                errors.Add($"Explanation '{explanation.Id}' references unknown framework.");
            }
            else if (disabledFrameworkIds.Contains(explanation.FrameworkId))
            {
                errors.Add($"Explanation '{explanation.Id}' must not attach to disabled framework '{explanation.FrameworkId}'.");
            }

            if (!string.Equals(explanation.ContentKind, "accredaone_explanation", StringComparison.Ordinal))
            {
                errors.Add($"Explanation '{explanation.Id}' must have contentKind accredaone_explanation.");
            }
        }

        var explanationsByFramework = pack.Explanations
            .GroupBy(e => e.FrameworkId)
            .ToDictionary(g => g.Key, g => g.Count(), StringComparer.Ordinal);

        foreach (var frameworkId in eligibleFrameworkIds)
        {
            if (!explanationsByFramework.TryGetValue(frameworkId, out var explanationCount) || explanationCount != 1)
            {
                errors.Add($"Eligible framework '{frameworkId}' must have exactly one explanation.");
            }
        }

        foreach (var scenario in pack.QueryScenarios)
        {
            TrackId(scenario.Id, "query-scenario");
            AssertSyntheticRecord(scenario.Classification, scenario.Provenance, scenario.Id, errors);
            ValidateQueryScenarioConsistency(scenario, pack, errors);
        }

        if (errors.Count > 0)
        {
            throw new InvalidOperationException("Synthetic fixture validation failed: " + string.Join(' ', errors));
        }
    }

    private static void ValidateQueryScenarioConsistency(
        SyntheticQueryScenarioRecord scenario,
        SyntheticFixturePack pack,
        List<string> errors)
    {
        var scenarioType = scenario.ScenarioType ?? string.Empty;
        if (string.IsNullOrWhiteSpace(scenarioType))
        {
            errors.Add($"Query scenario '{scenario.Id}' must specify scenarioType.");
            return;
        }

        if (string.Equals(scenarioType, "explicit_framework", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(scenario.FrameworkCode))
            {
                errors.Add($"Query scenario '{scenario.Id}' with explicit_framework requires frameworkCode.");
                return;
            }

            if (string.IsNullOrWhiteSpace(scenario.ExpectedOutcome))
            {
                errors.Add($"Query scenario '{scenario.Id}' must specify expectedOutcome.");
                return;
            }

            var framework = pack.Frameworks.FirstOrDefault(f =>
                string.Equals(f.Code, scenario.FrameworkCode, StringComparison.OrdinalIgnoreCase));
            var derived = framework switch
            {
                null => "no_results",
                { EligibilityStatus: var status } when string.Equals(status, "eligible", StringComparison.OrdinalIgnoreCase) =>
                    "bounded_results",
                _ => "no_results",
            };

            if (!string.Equals(scenario.ExpectedOutcome, derived, StringComparison.Ordinal))
            {
                errors.Add(
                    $"Query scenario '{scenario.Id}' expectedOutcome '{scenario.ExpectedOutcome}' does not match scenarioType/frameworkCode contract ('{derived}').");
            }

            return;
        }

        if (string.Equals(scenarioType, "no_framework_selection", StringComparison.OrdinalIgnoreCase))
        {
            if (!string.IsNullOrWhiteSpace(scenario.FrameworkCode))
            {
                errors.Add($"Query scenario '{scenario.Id}' with no_framework_selection must not include frameworkCode.");
            }

            if (!string.Equals(
                    scenario.ExpectedOutcome,
                    SyntheticFixtureBoundaryVerifier.OutcomeBoundedCrossFrameworkResults,
                    StringComparison.Ordinal))
            {
                errors.Add(
                    $"Query scenario '{scenario.Id}' with no_framework_selection must expect {SyntheticFixtureBoundaryVerifier.OutcomeBoundedCrossFrameworkResults}.");
            }

            return;
        }

        if (string.Equals(scenarioType, "ambiguous_or_unsupported", StringComparison.OrdinalIgnoreCase))
        {
            if (!string.Equals(scenario.ExpectedOutcome, "no_results", StringComparison.Ordinal))
            {
                errors.Add($"Query scenario '{scenario.Id}' with ambiguous_or_unsupported must expect no_results.");
            }

            return;
        }

        errors.Add($"Query scenario '{scenario.Id}' has unsupported scenarioType '{scenarioType}'.");
    }

    private static void AssertSyntheticRecord(string classification, string provenance, string label, List<string> errors)
    {
        if (!string.Equals(classification, SyntheticClassification, StringComparison.OrdinalIgnoreCase))
        {
            errors.Add($"Record '{label}' must have classification '{SyntheticClassification}'.");
        }

        if (string.IsNullOrWhiteSpace(provenance))
        {
            errors.Add($"Record '{label}' must include provenance.");
        }
    }

    private static T DeserializeFile<T>(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Fixture file not found: {path}");
        }

        var json = File.ReadAllText(path);
        var value = JsonSerializer.Deserialize<T>(json, JsonOptions);
        if (value is null)
        {
            throw new InvalidOperationException($"Fixture file '{path}' did not deserialize.");
        }

        return value;
    }

    private static string ResolveFixtureRoot()
    {
        var baseDirectory = AppContext.BaseDirectory;
        var candidate = Path.Combine(baseDirectory, "Fixtures", "Synthetic");
        if (Directory.Exists(candidate))
        {
            return candidate;
        }

        var projectRelative = Path.GetFullPath(Path.Combine(baseDirectory, "..", "..", "..", "Fixtures", "Synthetic"));
        if (Directory.Exists(projectRelative))
        {
            return projectRelative;
        }

        throw new DirectoryNotFoundException("Synthetic fixture directory was not found.");
    }
}
