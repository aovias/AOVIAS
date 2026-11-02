namespace AccredaOne.Api.Tests.Fixtures.Synthetic;

public sealed class SyntheticFixtureManifest
{
    public required string PackId { get; init; }

    public required string Classification { get; init; }

    public required string Provenance { get; init; }

    public required SyntheticExpectedCounts ExpectedCounts { get; init; }
}

public sealed class SyntheticExpectedCounts
{
    public int FrameworksTotal { get; init; }

    public int FrameworksEligible { get; init; }

    public int FrameworksIneligible { get; init; }

    public int SourceInstruments { get; init; }

    public int RequirementsCurrentPerEligibleFramework { get; init; }

    public int FrameworkVersionRecords { get; init; }

    public int Explanations { get; init; }

    public int QueryScenarios { get; init; }
}

public sealed class SyntheticFrameworkRecord
{
    public required string Id { get; init; }

    public required string Code { get; init; }

    public required string DisplayName { get; init; }

    public required string Classification { get; init; }

    public required string Provenance { get; init; }

    public required string EligibilityStatus { get; init; }
}

public sealed class SyntheticSourceRecord
{
    public required string Id { get; init; }

    public required string FrameworkId { get; init; }

    public required string Title { get; init; }

    public required string Classification { get; init; }

    public required string Provenance { get; init; }

    public required string ContentKind { get; init; }
}

public sealed class SyntheticRequirementRecord
{
    public required string Id { get; init; }

    public required string FrameworkId { get; init; }

    public required string SourceInstrumentId { get; init; }

    public required string Label { get; init; }

    public required string VersionStatus { get; init; }

    public required string Classification { get; init; }

    public required string Provenance { get; init; }
}

public sealed class SyntheticFrameworkVersionRecord
{
    public required string Id { get; init; }

    public required string FrameworkId { get; init; }

    public required string VersionLabel { get; init; }

    public required string VersionStatus { get; init; }

    public required string Classification { get; init; }

    public required string Provenance { get; init; }
}

public sealed class SyntheticExplanationRecord
{
    public required string Id { get; init; }

    public required string FrameworkId { get; init; }

    public required string Title { get; init; }

    public required string Body { get; init; }

    public required string Classification { get; init; }

    public required string Provenance { get; init; }

    public required string ContentKind { get; init; }
}

public sealed class SyntheticQueryScenarioRecord
{
    public required string Id { get; init; }

    public required string ScenarioType { get; init; }

    public string? FrameworkCode { get; init; }

    public required string Classification { get; init; }

    public required string Provenance { get; init; }

    public required string ExpectedOutcome { get; init; }
}

public sealed class SyntheticFixturePack
{
    public required SyntheticFixtureManifest Manifest { get; init; }

    public required IReadOnlyList<SyntheticFrameworkRecord> Frameworks { get; init; }

    public required IReadOnlyList<SyntheticSourceRecord> Sources { get; init; }

    public required IReadOnlyList<SyntheticRequirementRecord> Requirements { get; init; }

    public required IReadOnlyList<SyntheticFrameworkVersionRecord> FrameworkVersions { get; init; }

    public required IReadOnlyList<SyntheticExplanationRecord> Explanations { get; init; }

    public required IReadOnlyList<SyntheticQueryScenarioRecord> QueryScenarios { get; init; }
}

public sealed class SyntheticQueryResolution
{
    public required string Outcome { get; init; }

    public string? FrameworkId { get; init; }

    public IReadOnlyList<string> RequirementIds { get; init; } = [];

    public string? ClarificationReason { get; init; }
}
