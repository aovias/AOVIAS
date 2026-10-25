namespace Shared.Logging.Platform;

/// <summary>
/// Registration metadata exposed by PRAMAN workloads for AOVIAS Runtime discovery.
/// </summary>
public sealed class PramanServiceRegistration
{
    public required string ApplicationId { get; init; } = "praman";

    public required string ServiceId { get; init; }

    public required string ServiceName { get; init; }

    public required string Version { get; init; }

    public required string HealthEndpoint { get; init; }

    public required string ReadinessEndpoint { get; init; }

    public required string LivenessEndpoint { get; init; }

    public required IReadOnlyList<string> Capabilities { get; init; }

    public required IReadOnlyList<string> Dependencies { get; init; }

    public required string Category { get; init; }

    public string? PackageId { get; init; }

    public string? DeploymentPackage { get; init; }

    public IReadOnlyDictionary<string, string> DeploymentMetadata { get; init; } =
        new Dictionary<string, string>();
}
