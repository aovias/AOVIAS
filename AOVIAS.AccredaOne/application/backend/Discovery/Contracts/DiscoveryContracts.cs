namespace AccredaOne.Api.Discovery.Contracts;

public sealed class DiscoveryQueryRequest
{
    public required string Query { get; init; }

    public string? FrameworkCode { get; init; }
}

public sealed class DiscoveryQueryResponse
{
    public required string Outcome { get; init; }

    public required string DatasetPackId { get; init; }

    public required string DatasetClassification { get; init; }

    public IReadOnlyList<string> RequirementIds { get; init; } = [];

    public IReadOnlyList<DiscoveryFrameworkGroupResponse> FrameworkGroups { get; init; } = [];
}

public sealed class DiscoveryFrameworkGroupResponse
{
    public required string FrameworkId { get; init; }

    public required string FrameworkCode { get; init; }

    public required string DisplayName { get; init; }

    public required string FrameworkProvenance { get; init; }

    public required DiscoverySourceInstrumentResponse SourceInstrument { get; init; }

    public DiscoveryAccredaOneExplanationResponse? AccredaOneExplanation { get; init; }

    public IReadOnlyList<DiscoveryRequirementResponse> Requirements { get; init; } = [];
}

public sealed class DiscoveryRequirementResponse
{
    public required string Id { get; init; }

    public required string Label { get; init; }

    public required string Provenance { get; init; }

    public string? HierarchyContext { get; init; }

    public required string SourceInstrumentId { get; init; }
}

public sealed class DiscoverySourceInstrumentResponse
{
    public required string Id { get; init; }

    public required string Title { get; init; }

    public required string Provenance { get; init; }

    public required string ContentKind { get; init; }
}

public sealed class DiscoveryAccredaOneExplanationResponse
{
    public required string Id { get; init; }

    public required string Title { get; init; }

    public required string Body { get; init; }

    public required string Provenance { get; init; }

    public required string ContentKind { get; init; }
}
