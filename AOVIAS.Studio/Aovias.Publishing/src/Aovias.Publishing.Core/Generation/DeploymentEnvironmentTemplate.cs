using System.Text;
using Aovias.Deployment.Contracts;

namespace Aovias.Publishing.Core.Generation;

/// <summary>
/// Canonical environment-neutral <c>.env.template</c> content. Publishing emits this
/// template unchanged for every profile; deployment profiles supply concrete values
/// at deploy time through the Runtime Agent.
/// </summary>
public static class DeploymentEnvironmentTemplate
{
    public static string Generate()
    {
        var builder = new StringBuilder();
        builder.AppendLine("# Environment-neutral deployment template.");
        builder.AppendLine("# Profile-specific host paths and runtime settings are resolved at deploy time.");
        builder.AppendLine("# Container paths use portable Linux runtime defaults.");

        foreach (var pair in GetCanonicalVariables())
        {
            builder.Append(pair.Key).Append('=').Append(pair.Value).AppendLine();
        }

        return builder.ToString().TrimEnd('\r', '\n') + Environment.NewLine;
    }

    public static IReadOnlyList<KeyValuePair<string, string>> GetCanonicalVariables()
        =>
        [
            new("DOTNET_ENVIRONMENT", "${DOTNET_ENVIRONMENT:-Production}"),
            new("AOVIAS_CONFIG_ROOT", PlatformRuntimeDefaults.ConfigRoot),
            new("AOVIAS_ACTIVE_PROFILE", "${AOVIAS_ACTIVE_PROFILE}"),
            new("AOVIAS_PROFILES_DIRECTORY", "${AOVIAS_PROFILES_DIRECTORY:-profiles}"),
            new("AOVIAS_SECRETS_DIRECTORY", "${AOVIAS_SECRETS_DIRECTORY:-secrets}"),
            new("AOVIAS_PACKAGES_ROOT", PlatformRuntimeDefaults.PackagesRoot),
            new("AOVIAS_LOGS_ROOT", PlatformRuntimeDefaults.LogsRoot),
            new("AOVIAS_DEPLOYMENTS_ROOT", PlatformDeploymentDefaults.DeploymentRoot),
            new("AOVIAS_DOCKER_NETWORK", "${AOVIAS_DOCKER_NETWORK}"),
            new("AOVIAS_HOST_CONFIG_ROOT", "${AOVIAS_HOST_CONFIG_ROOT}"),
            new("AOVIAS_HOST_PACKAGES_ROOT", "${AOVIAS_HOST_PACKAGES_ROOT}"),
            new("AOVIAS_HOST_LOGS_ROOT", "${AOVIAS_HOST_LOGS_ROOT}"),
            new("AOVIAS_HOST_DEPLOYMENTS_ROOT", PlatformDeploymentDefaults.HostDeploymentsRootToken),
            new("AOVIAS_HOST_EVIDENCE_ROOT", "${AOVIAS_HOST_EVIDENCE_ROOT}"),
            new("COMPLIANCE_LOG_ROOT", "${AOVIAS_LOGS_ROOT}")
        ];
}
