using System.Reflection;
using System.Text.Json;
using Aovias.Deployment.Contracts.Serialization;

namespace Aovias.Deployment.Contracts.Capabilities;

/// <summary>
/// What a specific kind of deployment environment declares it provides (Sprint M2.3 D6).
/// Declarative by construction: a profile is a fixed list of capability ids, never inferred from
/// probing a live host. Loading a profile and resolving it to <see cref="Capability"/> instances
/// is the environment side of the D4 matching engine's input; the service side is
/// <see cref="Requirement"/>.
/// </summary>
public sealed class EnvironmentCapabilityProfile
{
    public required string Id { get; init; }

    public required string DisplayName { get; init; }

    public string Description { get; init; } = string.Empty;

    public IReadOnlyList<string> CapabilityIds { get; init; } = [];
}

/// <summary>Loads the embedded environment capability profile catalog (Sprint M2.3 D6).</summary>
public static class EnvironmentCapabilityProfileLoader
{
    private const string ResourceName = "Aovias.Deployment.Contracts.Capabilities.environment-capability-profiles.json";
    private static readonly Lazy<IReadOnlyDictionary<string, EnvironmentCapabilityProfile>> Profiles = new(LoadCatalog);

    public static EnvironmentCapabilityProfile Load(string profileId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(profileId);
        return Profiles.Value.TryGetValue(profileId, out var profile)
            ? profile
            : throw new InvalidOperationException($"Unknown environment capability profile '{profileId}'.");
    }

    public static IReadOnlyList<EnvironmentCapabilityProfile> All => Profiles.Value.Values.ToList();

    /// <summary>Resolves a profile's declared capability ids to their full <see cref="Capability"/> definitions via <see cref="CapabilityRegistry"/>.</summary>
    public static IReadOnlyList<Capability> ResolveCapabilities(string profileId)
    {
        var profile = Load(profileId);
        var unresolved = profile.CapabilityIds.Where(id => CapabilityRegistry.FindById(id) is null).ToList();
        if (unresolved.Count > 0)
        {
            throw new InvalidOperationException(
                $"Environment profile '{profileId}' declares unknown capability id(s): {string.Join(", ", unresolved)}.");
        }

        return profile.CapabilityIds.Select(id => CapabilityRegistry.FindById(id)!).ToList();
    }

    private static IReadOnlyDictionary<string, EnvironmentCapabilityProfile> LoadCatalog()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException($"Embedded environment capability profile catalog '{ResourceName}' was not found.");
        var catalog = JsonSerializer.Deserialize<Catalog>(stream, DeploymentContractJson.Options)
            ?? throw new InvalidOperationException("Environment capability profile catalog is invalid.");
        return catalog.Profiles.ToDictionary(profile => profile.Id, StringComparer.OrdinalIgnoreCase);
    }

    private sealed class Catalog
    {
        public IReadOnlyList<EnvironmentCapabilityProfile> Profiles { get; init; } = [];
    }
}
