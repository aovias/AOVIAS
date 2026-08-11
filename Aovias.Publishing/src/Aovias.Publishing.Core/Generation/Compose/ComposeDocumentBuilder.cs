using Aovias.Deployment.Contracts;
using Aovias.Publishing.Contracts;

namespace Aovias.Publishing.Core.Generation.Compose;

/// <summary>
/// Builds a schema-driven <see cref="ComposeDocument"/> for a service from its
/// manifest, volume contract, and health contract. All host-dependent values are
/// emitted as deployment variable references resolved from the runtime <c>.env</c>.
/// </summary>
public static class ComposeDocumentBuilder
{
    private const string NetworkAlias = "aovias-platform";

    public static ComposeDocument Build(
        DeployManifest manifest,
        VolumeContract volume,
        HealthContract health)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        ArgumentNullException.ThrowIfNull(volume);
        ArgumentNullException.ThrowIfNull(health);

        var port = manifest.Ports.FirstOrDefault();
        if (port <= 0)
        {
            throw new InvalidOperationException("Manifest must declare at least one container port.");
        }

        var service = new ComposeService
        {
            Name = manifest.Container,
            Image = $"{manifest.ImageName}:{manifest.ImageTag}",
            ContainerName = manifest.Container,
            Healthcheck = health.RequiresHttpProbe
                ? new ComposeHealthcheck
                {
                    Test =
                    [
                        "CMD",
                        "wget",
                        "-qO-",
                        $"http://127.0.0.1:{port}{NormalizeEndpoint(health.HealthUrl)}"
                    ]
                }
                : null
        };

        service.Ports.Add($"{port}:{port}");

        service.Environment.Add(new("ASPNETCORE_URLS", $"http://+:{port}"));
        service.Environment.Add(new("DOTNET_ENVIRONMENT", "${DOTNET_ENVIRONMENT:-Production}"));
        service.Environment.Add(new("COMPLIANCE_LOG_ROOT", "${AOVIAS_LOGS_ROOT}"));
        service.Environment.Add(new("AOVIAS_CONFIG_ROOT", "${AOVIAS_CONFIG_ROOT}"));
        service.Environment.Add(new("AOVIAS_ACTIVE_PROFILE", "${AOVIAS_ACTIVE_PROFILE}"));
        service.Environment.Add(new("AOVIAS_PROFILES_DIRECTORY", "${AOVIAS_PROFILES_DIRECTORY:-profiles}"));
        service.Environment.Add(new("AOVIAS_SECRETS_DIRECTORY", "${AOVIAS_SECRETS_DIRECTORY:-secrets}"));
        service.Environment.Add(new("AOVIAS_DEPLOYMENTS_ROOT", "${AOVIAS_DEPLOYMENTS_ROOT}"));

        foreach (var mount in ComposeMountBuilder.BuildVolumeEntries(volume))
        {
            service.Volumes.Add(mount);
        }

        service.NetworkAliases.Add(NetworkAlias);

        var document = new ComposeDocument();
        document.Services.Add(service);
        document.Networks.Add(new ComposeNetwork
        {
            Alias = NetworkAlias,
            Name = "${AOVIAS_DOCKER_NETWORK}",
            External = true
        });

        return document;
    }

    private static string NormalizeEndpoint(string? endpoint)
    {
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            return "/health";
        }

        return endpoint.StartsWith('/') ? endpoint : $"/{endpoint}";
    }
}
