namespace AccredaOne.Api.Tests.Fixtures.Synthetic;

public class SyntheticFixtureBoundaryTests
{
    private readonly SyntheticFixturePack _pack = SyntheticFixtureLoader.LoadDefault();

    [Fact]
    public void Loader_validates_manifest_counts_and_synthetic_classification()
    {
        Assert.Equal("dev-06a-synthetic-fixture-pack", _pack.Manifest.PackId);
        Assert.Equal(SyntheticFixtureLoader.SyntheticClassification, _pack.Manifest.Classification);
        Assert.Equal(2, _pack.Manifest.ExpectedCounts.FrameworksEligible);
        Assert.Equal(4, _pack.Manifest.ExpectedCounts.RequirementsCurrentPerEligibleFramework);
    }

    [Fact]
    public void Framework_isolation_keeps_requirements_within_framework_boundaries()
    {
        foreach (var framework in SyntheticFixtureBoundaryVerifier.GetEligibleFrameworks(_pack))
        {
            var requirements = SyntheticFixtureBoundaryVerifier.GetCurrentRequirementsForFramework(_pack, framework.Id);
            Assert.All(requirements, r => Assert.Equal(framework.Id, r.FrameworkId));
            Assert.All(requirements, r =>
            {
                var source = _pack.Sources.Single(s => s.Id == r.SourceInstrumentId);
                Assert.Equal(framework.Id, source.FrameworkId);
            });
        }
    }

    [Fact]
    public void Cross_framework_grouping_is_deterministic_and_separate()
    {
        var grouped = SyntheticFixtureBoundaryVerifier.GroupEligibleRequirementsByFramework(_pack);

        Assert.Equal(2, grouped.Count);
        Assert.Equal(4, grouped["NOAF"].Count);
        Assert.Equal(4, grouped["MCQF"].Count);
        Assert.Empty(grouped["NOAF"].Select(r => r.Id).Intersect(grouped["MCQF"].Select(r => r.Id)));
        Assert.Equal("SYN-REQ-NOAF-001", grouped["NOAF"][0].Id);
        Assert.Equal("SYN-REQ-MCQF-001", grouped["MCQF"][0].Id);
    }

    [Fact]
    public void Provenance_and_synthetic_markers_are_present_on_all_records()
    {
        Assert.All(_pack.Frameworks, f => Assert.Equal(SyntheticFixtureLoader.SyntheticClassification, f.Classification));
        Assert.All(_pack.Sources, s => Assert.Equal(SyntheticFixtureLoader.SyntheticClassification, s.Classification));
        Assert.All(_pack.Requirements, r => Assert.Equal(SyntheticFixtureLoader.SyntheticClassification, r.Classification));
        Assert.All(_pack.FrameworkVersions, v => Assert.Equal(SyntheticFixtureLoader.SyntheticClassification, v.Classification));
        Assert.All(_pack.Explanations, e => Assert.Equal(SyntheticFixtureLoader.SyntheticClassification, e.Classification));
        Assert.All(_pack.QueryScenarios, q => Assert.Equal(SyntheticFixtureLoader.SyntheticClassification, q.Classification));
        Assert.All(_pack.Sources, s => Assert.False(string.IsNullOrWhiteSpace(s.Provenance)));
        Assert.All(_pack.Requirements, r => Assert.False(string.IsNullOrWhiteSpace(r.Provenance)));
        Assert.All(_pack.Explanations, e => Assert.False(string.IsNullOrWhiteSpace(e.Provenance)));
        Assert.All(_pack.QueryScenarios, q => Assert.False(string.IsNullOrWhiteSpace(q.Provenance)));
    }

    [Fact]
    public void Source_instruments_are_separate_from_accredaone_explanations()
    {
        Assert.All(_pack.Sources, s => Assert.Equal("source_instrument", s.ContentKind));
        Assert.All(_pack.Explanations, e => Assert.Equal("accredaone_explanation", e.ContentKind));
        Assert.DoesNotContain(_pack.Explanations.Select(e => e.Id), id => _pack.Sources.Any(s => s.Id == id));
    }

    [Fact]
    public void Version_integrity_includes_historical_current_and_unverified_noaf_records()
    {
        var noafId = _pack.Frameworks.Single(f => f.Code == "NOAF").Id;
        var versions = _pack.FrameworkVersions.Where(v => v.FrameworkId == noafId).ToList();

        Assert.Contains(versions, v => v.VersionStatus == "historical");
        Assert.Contains(versions, v => v.VersionStatus == "current");
        Assert.Contains(versions, v => v.VersionStatus == "unverified_publication_candidate");
        Assert.Single(versions, v => v.VersionStatus == "current");
    }

    [Fact]
    public void Only_verified_current_version_is_applicable_for_noaf()
    {
        var noafId = _pack.Frameworks.Single(f => f.Code == "NOAF").Id;
        var current = SyntheticFixtureBoundaryVerifier.GetVerifiedCurrentVersionForFramework(_pack, noafId);

        Assert.NotNull(current);
        Assert.Equal("SYN-VER-NOAF-CURR-001", current!.Id);
        Assert.Equal("NOAF-2026-SYN-CURR", current.VersionLabel);

        var historical = _pack.FrameworkVersions.Single(v => v.Id == "SYN-VER-NOAF-HIST-001");
        var unverified = _pack.FrameworkVersions.Single(v => v.Id == "SYN-VER-NOAF-UNVER-001");

        Assert.False(SyntheticFixtureBoundaryVerifier.IsApplicableCurrentVersion(historical));
        Assert.False(SyntheticFixtureBoundaryVerifier.IsApplicableCurrentVersion(unverified));
        Assert.True(SyntheticFixtureBoundaryVerifier.IsApplicableCurrentVersion(current));
    }

    [Fact]
    public void Version_records_retain_valid_framework_relationships()
    {
        var frameworkIds = _pack.Frameworks.Select(f => f.Id).ToHashSet(StringComparer.Ordinal);
        var noafId = _pack.Frameworks.Single(f => f.Code == "NOAF").Id;
        var noafSourceIds = _pack.Sources.Where(s => s.FrameworkId == noafId).Select(s => s.Id).ToHashSet(StringComparer.Ordinal);

        foreach (var version in _pack.FrameworkVersions)
        {
            Assert.Contains(version.FrameworkId, frameworkIds);
        }

        foreach (var requirement in _pack.Requirements.Where(r => r.FrameworkId == noafId))
        {
            Assert.Contains(requirement.SourceInstrumentId, noafSourceIds);
        }
    }

    [Fact]
    public void Eligibility_filter_excludes_disabled_framework_from_eligible_set()
    {
        var eligible = SyntheticFixtureBoundaryVerifier.GetEligibleFrameworks(_pack);
        Assert.Equal(2, eligible.Count);
        Assert.DoesNotContain(eligible, f => f.Code == "LCF");
        Assert.Contains(_pack.Frameworks, f => f.Code == "LCF" && f.EligibilityStatus == "disabled");
    }

    [Fact]
    public void Explicit_eligible_framework_selection_returns_bounded_results_for_noaf()
    {
        var scenario = _pack.QueryScenarios.Single(q => q.Id == "SYN-QRY-001");
        Assert.Equal("explicit_framework", scenario.ScenarioType);
        Assert.Equal("NOAF", scenario.FrameworkCode);

        var framework = SyntheticFixtureBoundaryVerifier.FindFrameworkByCode(_pack, scenario.FrameworkCode!);
        Assert.NotNull(framework);
        Assert.Equal("eligible", framework!.EligibilityStatus, ignoreCase: true);

        var resolution = SyntheticFixtureBoundaryVerifier.ResolveQueryScenario(_pack, scenario);

        Assert.Equal("bounded_results", resolution.Outcome);
        Assert.Equal(framework.Id, resolution.FrameworkId);
        Assert.Equal(4, resolution.RequirementIds.Count);
        Assert.Equal(scenario.ExpectedOutcome, resolution.Outcome);
    }

    [Fact]
    public void Explicit_eligible_framework_selection_returns_bounded_results_for_mcqf()
    {
        var scenario = _pack.QueryScenarios.Single(q => q.Id == "SYN-QRY-002");
        var framework = SyntheticFixtureBoundaryVerifier.FindFrameworkByCode(_pack, "MCQF");
        Assert.NotNull(framework);

        var resolution = SyntheticFixtureBoundaryVerifier.ResolveQueryScenario(_pack, scenario);

        Assert.Equal("bounded_results", resolution.Outcome);
        Assert.Equal(framework!.Id, resolution.FrameworkId);
        Assert.Equal(4, resolution.RequirementIds.Count);
        Assert.Equal(scenario.ExpectedOutcome, resolution.Outcome);
    }

    [Fact]
    public void No_framework_selection_returns_bounded_cross_framework_groups_for_eligible_frameworks_only()
    {
        var scenario = _pack.QueryScenarios.Single(q => q.Id == "SYN-QRY-003");
        Assert.Equal("no_framework_selection", scenario.ScenarioType);
        Assert.Null(scenario.FrameworkCode);

        var resolution = SyntheticFixtureBoundaryVerifier.ResolveQueryScenario(_pack, scenario);

        Assert.Equal(SyntheticFixtureBoundaryVerifier.OutcomeBoundedCrossFrameworkResults, resolution.Outcome);
        Assert.Null(resolution.FrameworkId);
        Assert.Null(resolution.ClarificationReason);
        Assert.Equal(scenario.ExpectedOutcome, resolution.Outcome);

        Assert.NotNull(resolution.RequirementIdsByFrameworkCode);
        Assert.Equal(["MCQF", "NOAF"], resolution.RequirementIdsByFrameworkCode!.Keys.OrderBy(c => c, StringComparer.Ordinal));
        Assert.Equal(4, resolution.RequirementIdsByFrameworkCode["NOAF"].Count);
        Assert.Equal(4, resolution.RequirementIdsByFrameworkCode["MCQF"].Count);
        Assert.DoesNotContain(resolution.RequirementIdsByFrameworkCode.Keys, code => code == "LCF");
        Assert.Equal(8, resolution.RequirementIds.Count);
        Assert.All(resolution.RequirementIdsByFrameworkCode["NOAF"], id => Assert.StartsWith("SYN-REQ-NOAF-", id, StringComparison.Ordinal));
        Assert.All(resolution.RequirementIdsByFrameworkCode["MCQF"], id => Assert.StartsWith("SYN-REQ-MCQF-", id, StringComparison.Ordinal));
    }

    [Fact]
    public void Unknown_framework_code_returns_no_results_without_throwing()
    {
        var scenario = _pack.QueryScenarios.Single(q => q.Id == "SYN-QRY-004");
        Assert.Equal("ambiguous_or_unsupported", scenario.ScenarioType);

        var resolution = SyntheticFixtureBoundaryVerifier.ResolveQueryScenario(_pack, scenario);

        Assert.Equal("no_results", resolution.Outcome);
        Assert.Null(resolution.FrameworkId);
        Assert.Empty(resolution.RequirementIds);
        Assert.Equal(scenario.ExpectedOutcome, resolution.Outcome);
    }

    [Fact]
    public void Explicit_disabled_framework_selection_returns_no_results()
    {
        var scenario = new SyntheticQueryScenarioRecord
        {
            Id = "SYN-QRY-TEMP-DISABLED",
            ScenarioType = "explicit_framework",
            FrameworkCode = "LCF",
            Classification = SyntheticFixtureLoader.SyntheticClassification,
            Provenance = "Temporary scenario for disabled-framework boundary test",
            ExpectedOutcome = "no_results",
        };

        var framework = SyntheticFixtureBoundaryVerifier.FindFrameworkByCode(_pack, "LCF");
        Assert.NotNull(framework);
        Assert.Equal("disabled", framework!.EligibilityStatus, ignoreCase: true);

        var resolution = SyntheticFixtureBoundaryVerifier.ResolveQueryScenario(_pack, scenario);

        Assert.Equal("no_results", resolution.Outcome);
        Assert.Null(resolution.FrameworkId);
        Assert.Empty(resolution.RequirementIds);
    }

    [Fact]
    public void Explicit_unknown_framework_code_returns_no_results()
    {
        var scenario = new SyntheticQueryScenarioRecord
        {
            Id = "SYN-QRY-TEMP-UNKNOWN",
            ScenarioType = "explicit_framework",
            FrameworkCode = "NOT-A-REAL-CODE",
            Classification = SyntheticFixtureLoader.SyntheticClassification,
            Provenance = "Temporary scenario for unknown-framework boundary test",
            ExpectedOutcome = "no_results",
        };

        var resolution = SyntheticFixtureBoundaryVerifier.ResolveQueryScenario(_pack, scenario);

        Assert.Equal("no_results", resolution.Outcome);
        Assert.Null(resolution.FrameworkId);
        Assert.Empty(resolution.RequirementIds);
    }

    [Fact]
    public void GetVerifiedCurrentVersionForFramework_never_returns_unverified_or_historical_status()
    {
        var noafId = _pack.Frameworks.Single(f => f.Code == "NOAF").Id;
        var verified = SyntheticFixtureBoundaryVerifier.GetVerifiedCurrentVersionForFramework(_pack, noafId);

        Assert.NotNull(verified);
        Assert.Equal("SYN-VER-NOAF-CURR-001", verified!.Id);
        Assert.True(SyntheticFixtureBoundaryVerifier.IsVerifiedCurrentVersionStatus(verified.VersionStatus));

        foreach (var version in _pack.FrameworkVersions.Where(v => v.FrameworkId == noafId))
        {
            if (version.VersionStatus is "historical" or "unverified_publication_candidate")
            {
                Assert.False(SyntheticFixtureBoundaryVerifier.IsVerifiedCurrentVersionStatus(version.VersionStatus));
            }
        }
    }
}
