namespace Aovias.Configuration.Deployment;

/// <summary>
/// Strongly-typed deployment profile resolved at deploy time from deployment-host
/// settings. Publishing emits environment-neutral templates; the Runtime Agent and
/// deployment profiles supply concrete values during deployment.
/// </summary>
public sealed class DeploymentProfile
{
    /// <summary>Profile name (for example <c>local</c>, <c>demo</c>, <c>uat</c>, <c>production</c>).</summary>
    public required string Name { get; init; }

    /// <summary>Value for <c>DOTNET_ENVIRONMENT</c>.</summary>
    public required string DotNetEnvironment { get; init; }

    /// <summary>Value for <c>AOVIAS_ACTIVE_PROFILE</c> (equal to <see cref="Name"/>).</summary>
    public required string ActiveProfile { get; init; }

    /// <summary>Container-side configuration root (always a Linux path).</summary>
    public required string ConfigRoot { get; init; }

    /// <summary>Container-side packages root (always a Linux path).</summary>
    public required string PackagesRoot { get; init; }

    /// <summary>Container-side logs root (always a Linux path).</summary>
    public required string LogsRoot { get; init; }

    /// <summary>Host-side configuration root (mounted read-only into the container).</summary>
    public required string HostConfigRoot { get; init; }

    /// <summary>Host-side packages root.</summary>
    public required string HostPackagesRoot { get; init; }

    /// <summary>Host-side logs root.</summary>
    public required string HostLogsRoot { get; init; }

    /// <summary>Container-side deployment storage root.</summary>
    public required string DeploymentsRoot { get; init; }

    /// <summary>Host-side deployment storage root bind-mounted into the container.</summary>
    public required string HostDeploymentsRoot { get; init; }

    /// <summary>Docker network the service attaches to.</summary>
    public required string DockerNetwork { get; init; }

    /// <summary>Relative profiles directory name.</summary>
    public string ProfilesDirectory { get; init; } = "profiles";

    /// <summary>Relative secrets directory name.</summary>
    public string SecretsDirectory { get; init; } = "secrets";

    /// <summary>
    /// True when the host paths follow a Windows drive convention (used by the
    /// profile-aware environment-neutrality gate to permit a profile's declared
    /// host roots while still rejecting developer/machine identifiers).
    /// </summary>
    public bool UsesWindowsHostConvention =>
        IsWindowsDrivePath(HostConfigRoot)
        || IsWindowsDrivePath(HostPackagesRoot)
        || IsWindowsDrivePath(HostLogsRoot)
        || IsWindowsDrivePath(HostDeploymentsRoot);

    /// <summary>
    /// Renders the canonical, ordered environment variable map produced for this
    /// profile. This is the single definition consumed by the environment file
    /// generator and the docker-compose generator.
    /// </summary>
    public IReadOnlyList<KeyValuePair<string, string>> ToEnvironmentVariables()
        =>
        [
            new("DOTNET_ENVIRONMENT", DotNetEnvironment),
            new("AOVIAS_CONFIG_ROOT", ConfigRoot),
            new("AOVIAS_ACTIVE_PROFILE", ActiveProfile),
            new("AOVIAS_PROFILES_DIRECTORY", ProfilesDirectory),
            new("AOVIAS_SECRETS_DIRECTORY", SecretsDirectory),
            new("AOVIAS_PACKAGES_ROOT", PackagesRoot),
            new("AOVIAS_LOGS_ROOT", LogsRoot),
            new("AOVIAS_DEPLOYMENTS_ROOT", DeploymentsRoot),
            new("AOVIAS_DOCKER_NETWORK", DockerNetwork),
            new("AOVIAS_HOST_CONFIG_ROOT", HostConfigRoot),
            new("AOVIAS_HOST_PACKAGES_ROOT", HostPackagesRoot),
            new("AOVIAS_HOST_LOGS_ROOT", HostLogsRoot),
            new("AOVIAS_HOST_DEPLOYMENTS_ROOT", HostDeploymentsRoot),
            new("COMPLIANCE_LOG_ROOT", LogsRoot)
        ];

    private static bool IsWindowsDrivePath(string? value)
        => !string.IsNullOrEmpty(value)
           && value.Length >= 2
           && char.IsLetter(value[0])
           && value[1] == ':';
}
