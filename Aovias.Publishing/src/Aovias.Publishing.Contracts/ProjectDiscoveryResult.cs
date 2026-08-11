namespace Aovias.Publishing.Contracts;

/// <summary>
/// Result of automatic project discovery.
/// </summary>
public sealed class ProjectDiscoveryResult
{
    public required PublishingProjectType ProjectType { get; init; }

    public required string ProjectPath { get; init; }

    public required string ProjectRoot { get; init; }

    public required string ServiceId { get; init; }

    public required string ServiceName { get; init; }

    public required string ProviderName { get; init; }

    public int Confidence { get; init; }

    public int ContainerPort { get; init; } = 8080;

    public string HealthEndpoint { get; init; } = "/health";

    public string? EntryPoint { get; init; }

    public string? Framework { get; init; }

    /// <summary>
    /// Additional MSBuild properties (key=value) the provider determined should
    /// be passed to <c>dotnet publish</c> for this project. Keeps build behavior
    /// driven by discovery metadata rather than ad-hoc filename checks.
    /// </summary>
    public IReadOnlyDictionary<string, string> PublishProperties { get; init; }
        = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}
