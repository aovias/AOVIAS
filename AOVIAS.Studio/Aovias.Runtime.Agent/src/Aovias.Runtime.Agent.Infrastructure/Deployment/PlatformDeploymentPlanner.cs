using Aovias.Release.Contracts.RemoteDeployment;

namespace Aovias.Runtime.Agent.Infrastructure.Deployment;

/// <summary>
/// Orders services from a platform manifest. Application/Service scopes never pull infrastructure
/// packages into the ordered set — that filtering is applied via
/// <see cref="PlatformDeploymentScope.ResolveEffectiveServiceIds"/> before ordering.
/// </summary>
public static class PlatformDeploymentPlanner
{
    public static bool IsInfrastructureService(string serviceId)
        => PlatformDeploymentScope.IsInfrastructureService(serviceId);

    public static PlatformDeploymentMode InferMode(IReadOnlyList<string>? selectedServiceIds)
        => PlatformDeploymentScope.InferMode(selectedServiceIds);

    public static IReadOnlyList<string> ResolveEffectiveServiceIds(
        PlatformDeploymentManifest manifest,
        IReadOnlyList<string>? selectedServiceIds,
        PlatformDeploymentMode mode)
        => PlatformDeploymentScope.ResolveEffectiveServiceIds(manifest, selectedServiceIds, mode);

    public static IReadOnlyList<string> GetRequiredApplicationDependencies(
        PlatformDeploymentManifest manifest,
        IReadOnlyList<string> selectedServiceIds)
        => PlatformDeploymentScope.GetRequiredApplicationDependencies(manifest, selectedServiceIds);

    public static IReadOnlyList<PlatformDeploymentServiceEntry> OrderServices(
        PlatformDeploymentManifest manifest,
        IReadOnlyList<string>? selectedServiceIds = null)
    {
        var services = manifest.Services
            .Where(service => service.DeploymentReady)
            .Where(service => selectedServiceIds is null ||
                              selectedServiceIds.Contains(service.ServiceId, StringComparer.OrdinalIgnoreCase))
            .ToDictionary(service => service.ServiceId, StringComparer.OrdinalIgnoreCase);

        if (services.Count == 0)
        {
            return [];
        }

        var ordered = new List<PlatformDeploymentServiceEntry>();
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var visiting = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var serviceId in manifest.StartupOrder.Where(services.ContainsKey))
        {
            Visit(serviceId, services, visited, visiting, ordered);
        }

        foreach (var service in services.Values.OrderBy(entry => entry.StartupOrder))
        {
            Visit(service.ServiceId, services, visited, visiting, ordered);
        }

        return ordered;
    }

    private static void Visit(
        string serviceId,
        IReadOnlyDictionary<string, PlatformDeploymentServiceEntry> services,
        ISet<string> visited,
        ISet<string> visiting,
        IList<PlatformDeploymentServiceEntry> ordered)
    {
        if (visited.Contains(serviceId))
        {
            return;
        }

        if (!services.TryGetValue(serviceId, out var service))
        {
            // Dependency not in the scoped set (e.g. runtime-agent during Application deploy) — skip.
            return;
        }

        if (!visiting.Add(serviceId))
        {
            throw new InvalidOperationException($"Circular dependency detected for service '{serviceId}'.");
        }

        foreach (var dependency in service.DependsOn)
        {
            Visit(dependency, services, visited, visiting, ordered);
        }

        visiting.Remove(serviceId);
        visited.Add(serviceId);
        ordered.Add(service);
    }
}
