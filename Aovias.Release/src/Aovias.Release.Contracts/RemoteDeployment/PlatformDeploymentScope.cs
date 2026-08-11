using Aovias.Release.Contracts.RemoteDeployment;

namespace Aovias.Release.Contracts.RemoteDeployment;

/// <summary>
/// Shared deployment-scope helpers used by Studio (request construction) and the Runtime Agent
/// (queue/execution). Keeps Platform / Application / Service modes consistent across the boundary.
/// </summary>
public static class PlatformDeploymentScope
{
    /// <summary>Infrastructure service ids — required only for <see cref="PlatformDeploymentMode.Platform"/>.</summary>
    public static readonly HashSet<string> InfrastructureServiceIds = new(StringComparer.OrdinalIgnoreCase)
    {
        "runtime-agent"
    };

    public static bool IsInfrastructureService(string serviceId)
        => InfrastructureServiceIds.Contains(serviceId);

    public static PlatformDeploymentMode InferMode(IReadOnlyList<string>? selectedServiceIds)
    {
        if (selectedServiceIds is null || selectedServiceIds.Count == 0)
        {
            return PlatformDeploymentMode.Platform;
        }

        return selectedServiceIds.Count == 1
            ? PlatformDeploymentMode.Service
            : PlatformDeploymentMode.Application;
    }

    public static string FormatModeLabel(PlatformDeploymentMode mode) => mode switch
    {
        PlatformDeploymentMode.Platform => "Platform Deployment",
        PlatformDeploymentMode.Application => "Multi-Service Deployment",
        PlatformDeploymentMode.Service => "Single Service Deployment",
        _ => mode.ToString()
    };

    /// <summary>
    /// Resolves the effective service set: Platform → all ready services;
    /// Application/Service → selected services plus non-infrastructure dependencies.
    /// </summary>
    public static IReadOnlyList<string> ResolveEffectiveServiceIds(
        PlatformDeploymentManifest manifest,
        IReadOnlyList<string>? selectedServiceIds,
        PlatformDeploymentMode mode)
    {
        if (mode == PlatformDeploymentMode.Platform
            || selectedServiceIds is null
            || selectedServiceIds.Count == 0)
        {
            return manifest.Services
                .Where(service => service.DeploymentReady)
                .Select(service => service.ServiceId)
                .ToList();
        }

        var byId = manifest.Services
            .Where(service => service.DeploymentReady)
            .ToDictionary(service => service.ServiceId, StringComparer.OrdinalIgnoreCase);

        var effective = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var stack = new Stack<string>();
        foreach (var selected in selectedServiceIds)
        {
            stack.Push(selected);
        }

        while (stack.Count > 0)
        {
            var serviceId = stack.Pop();
            if (!effective.Add(serviceId))
            {
                continue;
            }

            if (!byId.TryGetValue(serviceId, out var service))
            {
                continue;
            }

            foreach (var dependency in service.DependsOn)
            {
                if (IsInfrastructureService(dependency))
                {
                    continue;
                }

                if (!effective.Contains(dependency))
                {
                    stack.Push(dependency);
                }
            }
        }

        return effective.ToList();
    }

    public static IReadOnlyList<string> GetRequiredApplicationDependencies(
        PlatformDeploymentManifest manifest,
        IReadOnlyList<string> selectedServiceIds)
    {
        var mode = selectedServiceIds.Count == 1
            ? PlatformDeploymentMode.Service
            : PlatformDeploymentMode.Application;
        var effective = ResolveEffectiveServiceIds(manifest, selectedServiceIds, mode);

        return effective
            .Where(id => !selectedServiceIds.Contains(id, StringComparer.OrdinalIgnoreCase))
            .Where(id => !IsInfrastructureService(id))
            .ToList();
    }
}
