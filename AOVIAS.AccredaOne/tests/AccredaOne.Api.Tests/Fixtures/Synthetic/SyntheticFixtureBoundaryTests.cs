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
        var noaf = _pack.Frameworks.Single(f => f.Code == "NOAF");
        var mcqf = _pack.Frameworks.Single(f => f.Code == "MCQF");

        var noafIds = SyntheticFixtureBoundaryVerifier.GetCurrentRequirementsForFramework(_pack, noaf.Id).Select(r => r.Id).ToList();
        var mcqfIds = SyntheticFixtureBoundaryVerifier.GetCurrentRequirementsForFramework(_pack, mcqf.Id).Select(r => r.Id).ToList();

        Assert.Equal(4, noafIds.Count);
        Assert.Equal(4, mcqfIds.Count);
        Assert.Empty(noafIds.Intersect(mcqfIds));
        Assert.Equal("SYN-REQ-NOAF-001", noafIds[0]);
        Assert.Equal("SYN-REQ-MCQF-001", mcqfIds[0]);
    }

    [Fact]
    public void Provenance_and_synthetic_markers_are_present_on_all_records()
    {
        Assert.All(_pack.Frameworks, f => Assert.Equal(SyntheticFixtureLoader.SyntheticClassification, f.Classification));
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
    public void Eligibility_filter_excludes_disabled_framework_from_eligible_set()
    {
        var eligible = SyntheticFixtureBoundaryVerifier.GetEligibleFrameworks(_pack);
        Assert.Equal(2, eligible.Count);
        Assert.DoesNotContain(eligible, f => f.Code == "LCF");
        Assert.Contains(_pack.Frameworks, f => f.Code == "LCF" && f.EligibilityStatus == "disabled");
    }

    [Theory]
    [InlineData("SYN-QRY-001", "bounded_results", "SYN-FW-NOAF-001", 4)]
    [InlineData("SYN-QRY-002", "bounded_results", "SYN-FW-MCQF-001", 4)]
    [InlineData("SYN-QRY-003", "clarification_required", null, 0)]
    [InlineData("SYN-QRY-004", "no_results", null, 0)]
    public void Query_scenarios_resolve_to_bounded_outcomes(
        string scenarioId,
        string expectedOutcome,
        string? expectedFrameworkId,
        int expectedRequirementCount)
    {
        var scenario = _pack.QueryScenarios.Single(q => q.Id == scenarioId);
        var resolution = SyntheticFixtureBoundaryVerifier.ResolveQueryScenario(_pack, scenario);

        Assert.Equal(expectedOutcome, resolution.Outcome);
        Assert.Equal(expectedFrameworkId, resolution.FrameworkId);
        Assert.Equal(expectedRequirementCount, resolution.RequirementIds.Count);
    }
}
