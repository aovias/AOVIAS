using System.Text.Json.Serialization;

namespace Aovias.Configuration.Layout;

/// <summary>
/// Deserialized <c>repository-layout.json</c> contract (Phase 1C-A). Not consumed by legacy path resolvers until explicitly wired.
/// </summary>
public sealed class RepositoryLayoutManifest
{
    [JsonPropertyName("schemaVersion")]
    public string SchemaVersion { get; init; } = string.Empty;

    [JsonPropertyName("products")]
    public List<RepositoryLayoutProduct> Products { get; init; } = [];

    [JsonPropertyName("solutions")]
    public List<RepositoryLayoutSolutionEntry> Solutions { get; init; } = [];

    [JsonPropertyName("discovery")]
    public RepositoryLayoutDiscovery Discovery { get; init; } = new();

    [JsonPropertyName("unresolvedDecisions")]
    public List<RepositoryLayoutUnresolvedDecision> UnresolvedDecisions { get; init; } = [];
}

public sealed class RepositoryLayoutProduct
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("owner")]
    public string Owner { get; init; } = string.Empty;

    [JsonPropertyName("paths")]
    public Dictionary<string, RepositoryLayoutPathPair> Paths { get; init; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class RepositoryLayoutPathPair
{
    [JsonPropertyName("current")]
    public string? Current { get; init; }

    [JsonPropertyName("target")]
    public string? Target { get; init; }
}

public sealed class RepositoryLayoutSolutionEntry
{
    [JsonPropertyName("path")]
    public RepositoryLayoutPathPair Path { get; init; } = new();

    [JsonPropertyName("owner")]
    public string Owner { get; init; } = string.Empty;
}

public sealed class RepositoryLayoutDiscovery
{
    [JsonPropertyName("current")]
    public RepositoryLayoutDiscoveryProfile Current { get; init; } = new();

    [JsonPropertyName("target")]
    public RepositoryLayoutDiscoveryProfile Target { get; init; } = new();
}

public sealed class RepositoryLayoutDiscoveryProfile
{
    [JsonPropertyName("platformRoot")]
    public RepositoryLayoutPlatformRootMarkers PlatformRoot { get; init; } = new();

    /// <summary>Publishing uses <see cref="PlatformRoot"/>; CSM uses this stricter marker set (typically all-required, no any-of).</summary>
    [JsonPropertyName("csmPlatformRoot")]
    public RepositoryLayoutPlatformRootMarkers CsmPlatformRoot { get; init; } = new();

    [JsonPropertyName("defaultApplicationRoots")]
    public List<string> DefaultApplicationRoots { get; init; } = [];

    [JsonPropertyName("deployableProjectScanRoots")]
    public List<string> DeployableProjectScanRoots { get; init; } = [];

    [JsonPropertyName("runtimeAgentProjectRelative")]
    public string RuntimeAgentProjectRelative { get; init; } = string.Empty;

    [JsonPropertyName("webAppAngularManifestRelative")]
    public string WebAppAngularManifestRelative { get; init; } = string.Empty;

    [JsonPropertyName("runtimeAgentDockerCopyRoots")]
    public List<string> RuntimeAgentDockerCopyRoots { get; init; } = [];
}

public sealed class RepositoryLayoutPlatformRootMarkers
{
    [JsonPropertyName("requiredAll")]
    public List<string> RequiredAll { get; init; } = [];

    [JsonPropertyName("requiredAny")]
    public List<string> RequiredAny { get; init; } = [];
}

public sealed class RepositoryLayoutUnresolvedDecision
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("topic")]
    public string Topic { get; init; } = string.Empty;
}

public enum RepositoryLayoutProfile
{
    Current,
    Target
}
