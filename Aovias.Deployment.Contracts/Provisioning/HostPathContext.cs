namespace Aovias.Deployment.Contracts.Provisioning;

/// <summary>
/// Host-side path roots used to resolve volume mount tokens during deployment preparation.
/// </summary>
public sealed class HostPathContext
{
    public required string HostConfigRoot { get; init; }

    public required string HostLogsRoot { get; init; }

    public required string HostPackagesRoot { get; init; }

    public string HostDeploymentsRoot { get; init; } = string.Empty;

    public string ResolveHostToken(string token)
        => token switch
        {
            "${AOVIAS_HOST_CONFIG_ROOT}" => HostConfigRoot,
            "${AOVIAS_HOST_LOGS_ROOT}" => HostLogsRoot,
            "${AOVIAS_HOST_PACKAGES_ROOT}" => HostPackagesRoot,
            "${AOVIAS_HOST_DEPLOYMENTS_ROOT}" => string.IsNullOrWhiteSpace(HostDeploymentsRoot)
                ? HostConfigRoot
                : HostDeploymentsRoot,
            _ => token
        };
}
