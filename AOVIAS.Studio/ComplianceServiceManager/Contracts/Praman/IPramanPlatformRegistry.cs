namespace ComplianceServiceManager.Contracts.Praman;

/// <summary>
/// PRAMAN service registration entry loaded from AOVIAS configuration.
/// </summary>
public sealed class PramanServiceDefinition
{
    public required string ServiceId { get; init; }

    public required string Name { get; init; }

    public required string Version { get; init; }

    public required string Category { get; init; }

    public required string PackageId { get; init; }

    public required string HealthEndpoint { get; init; }

    public required string ReadinessEndpoint { get; init; }

    public required string LivenessEndpoint { get; init; }

    public required string RegistrationEndpoint { get; init; }

    public IReadOnlyList<string> Capabilities { get; init; } = [];

    public IReadOnlyList<string> Dependencies { get; init; } = [];

    public string? MigrationPackage { get; init; }

    public string? SeedData { get; init; }
}

/// <summary>
/// PRAMAN platform registry contract for Command Center.
/// </summary>
public sealed class PramanPlatformRegistryDocument
{
    public required string ApplicationId { get; init; }

    public required string DisplayName { get; init; }

    public required string PlatformVersion { get; init; }

    public required string RuntimeVersion { get; init; }

    public required IReadOnlyList<PramanServiceDefinition> Services { get; init; }
}

/// <summary>
/// Loads PRAMAN platform registration metadata from AOVIAS configuration profiles.
/// </summary>
public interface IPramanPlatformRegistry
{
    Task<PramanPlatformRegistryDocument> LoadAsync(CancellationToken cancellationToken = default);
}
