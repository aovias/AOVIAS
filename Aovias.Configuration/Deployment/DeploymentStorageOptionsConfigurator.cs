using Aovias.Configuration.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Aovias.Configuration.Deployment;

/// <summary>
/// Derives <see cref="DeploymentStorageSettings.Root"/> from canonical or legacy release-management configuration.
/// </summary>
internal sealed class DeploymentStorageOptionsConfigurator : IConfigureOptions<DeploymentStorageSettings>
{
    private readonly IConfiguration configuration;

    public DeploymentStorageOptionsConfigurator(IConfiguration configuration)
    {
        this.configuration = configuration;
    }

    public void Configure(DeploymentStorageSettings options)
    {
        var legacyReleaseRoot = configuration[$"{ReleaseManagementSettings.SectionName}:ReleaseRoot"];
        options.Root = ConfiguredDeploymentPathResolver.NormalizeLegacyDeploymentRoot(options.Root, legacyReleaseRoot);
    }
}
