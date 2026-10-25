using System.Reflection;
using System.Text.Json;
using Aovias.Deployment.Contracts.Serialization;

namespace Aovias.Deployment.Contracts.Capabilities;

/// <summary>
/// The single, authoritative catalog of platform capabilities (Sprint M2.3 D3). Capability
/// definitions, their compatibility metadata (satisfied categories, version, aliases) and their
/// provider mappings live here and only here — nothing else in the platform is permitted to
/// define a capability inline.
/// </summary>
public static class CapabilityRegistry
{
    private const string ResourceName = "Aovias.Deployment.Contracts.Capabilities.capability-registry.json";
    private static readonly Lazy<IReadOnlyList<Capability>> CapabilityCatalog = new(LoadAndValidate);

    public static IReadOnlyList<Capability> All => CapabilityCatalog.Value;

    public static Capability? FindById(string id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        return All.FirstOrDefault(capability =>
            string.Equals(capability.Id, id, StringComparison.OrdinalIgnoreCase)
            || capability.Aliases.Any(alias => string.Equals(alias, id, StringComparison.OrdinalIgnoreCase)));
    }

    /// <summary>Every capability that satisfies <paramref name="category"/>, either as its primary category or via <see cref="Capability.SatisfiesCategories"/>.</summary>
    public static IReadOnlyList<Capability> FindByCategory(string category)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(category);
        return All.Where(capability =>
                string.Equals(capability.Category, category, StringComparison.OrdinalIgnoreCase)
                || capability.SatisfiesCategories.Any(satisfied => string.Equals(satisfied, category, StringComparison.OrdinalIgnoreCase)))
            .ToList();
    }

    /// <summary>Whether any registered capability could ever satisfy this category (used by <see cref="RequirementValidator"/> for "unknown requirement" detection).</summary>
    public static bool IsKnownCategory(string category) => FindByCategory(category).Count > 0;

    /// <summary>
    /// Detects cycles in <c>Capability.DependsOnCapabilityIds</c> via depth-first search
    /// (Sprint M2.3 D9). Returns the ids participating in the first cycle found, or an empty
    /// list if the graph is acyclic. Exposed publicly so callers can validate a candidate set of
    /// capabilities (e.g. an environment profile) before it is added to the registry.
    /// </summary>
    public static IReadOnlyList<string> DetectCircularDependencies(IReadOnlyList<Capability> capabilities)
    {
        var byId = capabilities
            .SelectMany(capability => new[] { capability.Id }.Concat(capability.Aliases).Select(key => (key, capability)))
            .GroupBy(pair => pair.key, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First().capability, StringComparer.OrdinalIgnoreCase);

        var state = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase); // 0 = unvisited, 1 = in-progress, 2 = done
        var stack = new List<string>();

        foreach (var capability in capabilities)
        {
            var cycle = Visit(capability.Id);
            if (cycle.Count > 0)
            {
                return cycle;
            }
        }

        return [];

        List<string> Visit(string id)
        {
            if (state.TryGetValue(id, out var existing))
            {
                if (existing == 1)
                {
                    var cycleStart = stack.IndexOf(id);
                    return cycleStart >= 0 ? stack.Skip(cycleStart).Append(id).ToList() : [id];
                }

                return [];
            }

            state[id] = 1;
            stack.Add(id);

            if (byId.TryGetValue(id, out var capability))
            {
                foreach (var dependency in capability.DependsOnCapabilityIds)
                {
                    var cycle = Visit(dependency);
                    if (cycle.Count > 0)
                    {
                        return cycle;
                    }
                }
            }

            stack.RemoveAt(stack.Count - 1);
            state[id] = 2;
            return [];
        }
    }

    private static IReadOnlyList<Capability> LoadAndValidate()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException($"Embedded capability registry '{ResourceName}' was not found.");
        var catalog = JsonSerializer.Deserialize<Catalog>(stream, DeploymentContractJson.Options)
            ?? throw new InvalidOperationException("Capability registry is invalid.");

        var duplicates = catalog.Capabilities.Select(c => c.Id)
            .GroupBy(id => id, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToList();
        if (duplicates.Count > 0)
        {
            throw new InvalidOperationException($"Capability registry declares duplicate capability id(s): {string.Join(", ", duplicates)}.");
        }

        var cycle = DetectCircularDependencies(catalog.Capabilities);
        if (cycle.Count > 0)
        {
            throw new InvalidOperationException($"Capability registry contains a circular dependency: {string.Join(" -> ", cycle)}.");
        }

        return catalog.Capabilities;
    }

    private sealed class Catalog
    {
        public IReadOnlyList<Capability> Capabilities { get; init; } = [];
    }
}
