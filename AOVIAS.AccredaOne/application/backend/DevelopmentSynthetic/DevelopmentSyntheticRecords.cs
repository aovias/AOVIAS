namespace AccredaOne.Api.DevelopmentSynthetic;

public sealed class DevelopmentSyntheticManifest
{
    public required string PackId { get; init; }

    public required string Classification { get; init; }

    public required string Provenance { get; init; }

    public required string Environment { get; init; }
}

public sealed class DevelopmentSyntheticFrameworkRecord
{
    public required string Id { get; init; }

    public required string Code { get; init; }

    public required string DisplayName { get; init; }

    public required string Classification { get; init; }

    public required string Provenance { get; init; }

    public required string EligibilityStatus { get; init; }
}

public sealed class DevelopmentSyntheticSourceRecord
{
    public required string Id { get; init; }

    public required string FrameworkId { get; init; }

    public required string Title { get; init; }

    public required string Classification { get; init; }

    public required string Provenance { get; init; }

    public required string ContentKind { get; init; }
}

public sealed class DevelopmentSyntheticRequirementRecord
{
    public required string Id { get; init; }

    public required string FrameworkId { get; init; }

    public required string SourceInstrumentId { get; init; }

    public required string Label { get; init; }

    public string? HierarchyContext { get; init; }

    public required string VersionStatus { get; init; }

    public required string Classification { get; init; }

    public required string Provenance { get; init; }
}

public sealed class DevelopmentSyntheticExplanationRecord
{
    public required string Id { get; init; }

    public required string FrameworkId { get; init; }

    public required string Title { get; init; }

    public required string Body { get; init; }

    public required string Classification { get; init; }

    public required string Provenance { get; init; }

    public required string ContentKind { get; init; }
}

public sealed class DevelopmentSyntheticDataset
{
    public required DevelopmentSyntheticManifest Manifest { get; init; }

    public required IReadOnlyList<DevelopmentSyntheticFrameworkRecord> Frameworks { get; init; }

    public required IReadOnlyList<DevelopmentSyntheticSourceRecord> Sources { get; init; }

    public required IReadOnlyList<DevelopmentSyntheticRequirementRecord> Requirements { get; init; }

    public required IReadOnlyList<DevelopmentSyntheticExplanationRecord> Explanations { get; init; }
}

internal sealed class DevelopmentSyntheticResolution
{
    public required string Outcome { get; init; }

    public IReadOnlyList<string> FrameworkIds { get; init; } = [];
}
