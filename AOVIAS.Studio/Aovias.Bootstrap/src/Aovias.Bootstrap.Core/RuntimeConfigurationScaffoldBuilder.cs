using System.Text;
using System.Text.Json;
using Aovias.Bootstrap.Contracts;
using Aovias.Configuration.Settings;
using Aovias.Deployment.Contracts;

namespace Aovias.Bootstrap.Core;

/// <summary>
/// Generates the <c>settings.deployment-host.json</c> content for a freshly-bootstrapped
/// environment's active profile. Deliberately produces exactly the flat shape
/// <c>Aovias.Configuration.Settings.DeploymentHostSettings</c> already binds against
/// (<c>DeploymentPathResolver.Resolve</c> calls <c>.Build().Get&lt;DeploymentHostSettings&gt;()</c>
/// directly, with no section wrapper) — this is not a new configuration schema, only a new writer
/// for the existing one, so Runtime Agent's own <see cref="!:Aovias.Configuration.Deployment.DeploymentPathResolver"/>
/// reads it unchanged once bootstrap uploads it.
/// </summary>
public static class RuntimeConfigurationScaffoldBuilder
{
    public static string BuildDeploymentHostSettingsJson(BootstrapTarget target)
    {
        ArgumentNullException.ThrowIfNull(target);

        // Real-world defect: despite the "Host*" naming, Aovias.Configuration.Deployment.
        // ConfiguredDeploymentPathResolver reads host.HostConfigRoot/HostLogsRoot/HostPackagesRoot as
        // ITS OWN ConfigRoot/LogsRoot/PackagesRoot — and that resolver runs inside the container, so
        // these values must be the container-internal canonical paths (the same PlatformRuntimeDefaults
        // constants BuildRuntimeAgentEnvFile's AOVIAS_CONFIG_ROOT/AOVIAS_LOGS_ROOT already use), not
        // the physical VPS bind-mount source paths. Using the host machine's paths here made the
        // Runtime Agent validate directories that were never actually bind-mounted at that literal
        // location inside its own filesystem, so the non-root container user got a genuine
        // UnauthorizedAccessException on its own ephemeral root-owned path — indistinguishable from a
        // host permission problem from the outside, but not one: the host directories were correct
        // the whole time.
        var document = new
        {
            DotNetEnvironment = target.EnvironmentType == Aovias.Release.Contracts.RemoteDeployment.EnvironmentType.Production
                ? "Production"
                : "Staging",
            ConfigRoot = PlatformRuntimeDefaults.ConfigRoot,
            PackagesRoot = PlatformRuntimeDefaults.PackagesRoot,
            LogsRoot = PlatformRuntimeDefaults.LogsRoot,
            HostConfigRoot = PlatformRuntimeDefaults.ConfigRoot,
            HostPackagesRoot = PlatformRuntimeDefaults.PackagesRoot,
            HostLogsRoot = PlatformRuntimeDefaults.LogsRoot,
            DockerNetwork = target.DockerNetworkName,
            ProfilesDirectory = "profiles",
            SecretsDirectory = "secrets"
        };

        return JsonSerializer.Serialize(document, new JsonSerializerOptions { WriteIndented = true });
    }

    /// <summary>The remote path this content must be uploaded to for <c>DeploymentPathResolver.Resolve</c> to find it, given the profile's config root and name.</summary>
    public static string ResolveRemotePath(BootstrapTarget target)
        => $"{target.RemoteConfigRoot.TrimEnd('/')}/profiles/{target.ProfileName}/settings.deployment-host.json";

    /// <summary>
    /// The <c>.env</c> file Docker Compose auto-loads from the same directory as the Runtime Agent's
    /// <c>docker-compose.yml</c> (produced by <c>Aovias.Publishing.Core</c>'s <c>ComposeGenerator</c>,
    /// which asserts the compose file references exactly these <c>AOVIAS_*</c> tokens — see
    /// <c>RequiredComposeTokens</c>). Nothing upstream of Bootstrap ever writes this file for a
    /// deployed target (publishing only ships a <c>.env.template</c> for a human to hand-author);
    /// without it every substitution is silently blank and <c>docker compose up</c> fails on the
    /// first empty bind-mount spec (e.g. <c>:/opt/aovias/deployments</c>). Container-side values
    /// reuse the same canonical constants the Runtime Agent image itself is built against
    /// (<see cref="PlatformRuntimeDefaults"/>/<see cref="PlatformDeploymentDefaults"/>) rather than
    /// duplicating those paths as new literals; host-side values mirror the same host paths
    /// <see cref="BuildDeploymentHostSettingsJson"/> already computes for this same profile.
    /// </summary>
    public static string BuildRuntimeAgentEnvFile(BootstrapTarget target)
    {
        ArgumentNullException.ThrowIfNull(target);

        var runtimeRoot = target.RemoteConfigRoot.TrimEnd('/');
        var parentSlash = runtimeRoot.LastIndexOf('/');
        var runtimeParent = parentSlash > 0 ? runtimeRoot[..parentSlash] : runtimeRoot;

        var lines = new (string Key, string Value)[]
        {
            ("AOVIAS_CONFIG_ROOT", PlatformRuntimeDefaults.ConfigRoot),
            ("AOVIAS_LOGS_ROOT", PlatformRuntimeDefaults.LogsRoot),
            ("AOVIAS_ACTIVE_PROFILE", target.ProfileName),
            ("AOVIAS_DEPLOYMENTS_ROOT", PlatformDeploymentDefaults.DeploymentRoot),
            ("AOVIAS_HOST_DEPLOYMENTS_ROOT", $"{runtimeParent}/deployments"),
            ("AOVIAS_HOST_CONFIG_ROOT", runtimeRoot),
            ("AOVIAS_HOST_LOGS_ROOT", $"{runtimeParent}/logs"),
            ("AOVIAS_HOST_PACKAGES_ROOT", $"{runtimeParent}/packages"),
            ("AOVIAS_DOCKER_NETWORK", target.DockerNetworkName)
        };

        var builder = new StringBuilder();
        foreach (var (key, value) in lines)
        {
            builder.Append(key).Append('=').Append(value).Append('\n');
        }

        return builder.ToString();
    }

    /// <summary>
    /// Sprint M3.3 — generates <c>settings.environment-registration.json</c>, the file
    /// <c>Aovias.Configuration.Loading.ProfileConfigurationLoader</c>'s <c>FileSectionMap</c> now maps
    /// to <see cref="Aovias.Configuration.Settings.EnvironmentRegistrationSettings"/>'s section. Before
    /// this sprint, Bootstrap knew <see cref="BootstrapTarget.CommandCenterBaseUrl"/> and
    /// <see cref="BootstrapTarget.RegistrationToken"/> (it already uses them directly in the
    /// Runtime Agent Registration phase's own one-time registration call) but never persisted them
    /// into the Runtime Agent's own configuration — so the M4.3
    /// <c>EnvironmentRegistrationHostedService</c>'s automatic re-registration on every subsequent
    /// restart (the whole point of that sprint: "no external tool or operator step required") would
    /// find <c>Enabled=false</c> (the type's default) and never run. This closes that gap using the
    /// same flat, unwrapped shape <see cref="BuildDeploymentHostSettingsJson"/> already establishes
    /// for this profile's other generated file.
    /// </summary>
    public static string BuildEnvironmentRegistrationSettingsJson(BootstrapTarget target)
    {
        ArgumentNullException.ThrowIfNull(target);

        var document = new
        {
            Enabled = true,
            target.CommandCenterBaseUrl,
            target.RegistrationToken,
            EnvironmentType = target.EnvironmentType.ToString()
        };

        return JsonSerializer.Serialize(document, new JsonSerializerOptions { WriteIndented = true });
    }

    /// <summary>The remote path <see cref="BuildEnvironmentRegistrationSettingsJson"/>'s content must be uploaded to for <c>ProfileConfigurationLoader</c> to find it.</summary>
    public static string ResolveEnvironmentRegistrationRemotePath(BootstrapTarget target)
        => $"{target.RemoteConfigRoot.TrimEnd('/')}/profiles/{target.ProfileName}/settings.environment-registration.json";

    /// <summary>The remote path any of <see cref="BuildRequiredProfileSettings"/>'s files must be uploaded to.</summary>
    public static string ResolveProfileFileRemotePath(BootstrapTarget target, string fileName)
        => $"{target.RemoteConfigRoot.TrimEnd('/')}/profiles/{target.ProfileName}/{fileName}";

    /// <summary>
    /// Defect correction (Bootstrap Configuration Validation &amp; Runtime Startup Diagnostics):
    /// before this, Bootstrap generated and uploaded exactly two of the roughly twenty
    /// <c>settings.*.json</c> files <c>Aovias.Configuration.Extensions.ModularConfigurationExtensions</c>
    /// requires the Runtime Agent's active profile to have (see
    /// <c>Aovias.Configuration.Loading.ProfileConfigurationLoader.FileSectionMap</c> for the
    /// authoritative file-to-settings-type map) — every other file was simply absent on a freshly
    /// bootstrapped host, so the container crashed on its own first startup the moment it tried to
    /// bind whichever required settings class came first (logging, in the reported defect: "The log
    /// root setting is missing or empty"). This returns every file the profile needs for the feature
    /// set Bootstrap itself exercises (Runtime, ReleaseManagement, DockerProvider, SignalR — the same
    /// four <c>Aovias.Runtime.Agent.PlatformFeatureActivation</c> flags Bootstrap's own phases already
    /// assume: Docker install/network, agent registration/heartbeat, and SignalR verification), as
    /// real typed <c>Aovias.Configuration.Settings</c> instances — the exact classes the Runtime Agent
    /// itself binds against — so both upload (<see cref="BuildAllRequiredProfileFiles"/>) and
    /// validation (<c>RuntimeConfigurationValidator</c>) work from one shared source of truth rather
    /// than two independently-maintained shapes.
    /// </summary>
    public static IReadOnlyDictionary<string, object> BuildRequiredProfileSettings(BootstrapTarget target)
    {
        ArgumentNullException.ThrowIfNull(target);

        var runtimeRoot = target.RemoteConfigRoot.TrimEnd('/');
        var parentSlash = runtimeRoot.LastIndexOf('/');
        var runtimeParent = parentSlash > 0 ? runtimeRoot[..parentSlash] : runtimeRoot;

        return new Dictionary<string, object>
        {
            ["settings.deployment-host.json"] = JsonSerializer.Deserialize<DeploymentHostSettings>(BuildDeploymentHostSettingsJson(target))!,
            ["settings.environment-registration.json"] = JsonSerializer.Deserialize<EnvironmentRegistrationSettings>(BuildEnvironmentRegistrationSettingsJson(target))!,

            ["settings.runtime.json"] = new RuntimeSettings
            {
                Environment = target.EnvironmentType.ToString(),
                ServerName = target.EnvironmentName,
                Hostname = target.Host,
                // Not resolved from the deployed artifact at generation time — the same honest
                // placeholder BootstrapOrchestrator.RunRuntimeAgentRegistrationAsync already uses for
                // AgentRegistrationRequest.RuntimeVersion before the agent's own heartbeat reports a
                // real version.
                PlatformVersion = "unknown",
                RuntimeVersion = "unknown",
                ServerId = $"srv-{target.ProfileName}",
                AgentDisplayName = $"AOVIAS Runtime Agent ({target.EnvironmentName})"
            },

            ["settings.storage.json"] = new StorageSettings
            {
                DataRoot = $"{runtimeParent}/storage/{target.ProfileName}/data",
                BackupRoot = $"{runtimeParent}/storage/{target.ProfileName}/backups",
                TempRoot = $"{runtimeParent}/storage/{target.ProfileName}/temp"
            },

            ["settings.runtime-manifest.json"] = new RuntimeManifestSettings
            {
                ManifestPath = $"{runtimeRoot}/profiles/{target.ProfileName}/manifest.json"
            },

            // The actual reported defect: no settings.logging.json was ever generated, so LogRoot
            // resolved empty and Aovias.Configuration.Settings.LoggingSettingsBinder.BindAndPrepare
            // threw during the Runtime Agent's own startup. LogRoot is the container-side path the
            // compose file already mounts logs at (AOVIAS_LOGS_ROOT in BuildRuntimeAgentEnvFile), not
            // the host path — this file is read from *inside* the container.
            ["settings.logging.json"] = new LoggingSettings
            {
                LogRoot = PlatformRuntimeDefaults.LogsRoot,
                MinimumLevel = "Information",
                RetentionDays = LoggingSettings.DefaultRetentionDays,
                RetainedFileCountLimit = LoggingSettings.DefaultRetentionDays,
                FileNamePrefix = LoggingSettings.DefaultFileNamePrefix
            },

            ["settings.api.json"] = new ApiSettings
            {
                BaseUrl = target.PublicUrl,
                PublicUrl = target.PublicUrl,
                // Kestrel's own bind address inside the container — deliberately not target.PublicUrl
                // (the externally-reachable address); Program.cs's ApiSettings.InternalUrl override
                // (WebHost.UseUrls) needs "listen on every interface", not "reach me at this host".
                InternalUrl = "http://0.0.0.0:7080",
                HealthEndpoint = "/health",
                TimeoutSeconds = 30,
                RetryCount = 3
            },

            ["settings.infrastructure.json"] = new InfrastructureSettings
            {
                PublicIp = target.Host,
                Domain = target.Host,
                HttpsPort = 443,
                HttpPort = 80,
                // Bootstrap does not provision an nginx/TLS-terminating reverse proxy (see the
                // Privilege Elevation defect-fix investigation) — declaring SSL enabled here when
                // nothing terminates it would misrepresent what Bootstrap actually set up.
                SslEnabled = false
            },

            // SigningKey/ApiKey are [Required] on the class but, per Aovias.Configuration.Extensions.
            // ModularConfigurationExtensions.RegisterValidatedSecurityOptions, not actually enforced
            // by the Runtime Agent's own startup validation (only JwtIssuer/JwtAudience are). Bootstrap
            // validates the full declared contract anyway (a stricter, proactive check is exactly
            // this defect fix's point) — the same "configured-in-secrets" placeholder convention the
            // demo profile's own hand-authored settings.security.json already uses, since the real
            // signing material belongs in the separate secrets/<profile>/ directory, not here.
            ["settings.security.json"] = new SecuritySettings
            {
                JwtIssuer = $"aovias-identity-{target.ProfileName}",
                JwtAudience = $"aovias-runtime-agent-{target.ProfileName}",
                SigningKey = "configured-in-secrets",
                ApiKey = "configured-in-secrets"
            },

            ["settings.release-management.json"] = new ReleaseManagementSettings
            {
                ReleaseRoot = $"{runtimeParent}/releases",
                PackageRoot = $"{runtimeParent}/storage/{target.ProfileName}/packages",
                TemporaryFolder = $"{runtimeParent}/releases/temp",
                ArchiveFolder = $"{runtimeParent}/releases/archive",
                IncomingFolder = $"{runtimeParent}/releases/incoming",
                StagingFolder = $"{runtimeParent}/releases/staging",
                ExtractionFolder = $"{runtimeParent}/releases/extraction",
                ActiveFolder = $"{runtimeParent}/releases/active",
                RollbackFolder = $"{runtimeParent}/releases/rollback",
                AuditFolder = $"{runtimeParent}/releases/audit",
                MaximumPackageSizeBytes = 524_288_000,
                SupportedPackageVersion = "1.0",
                SupportedRuntimeVersion = "unknown",
                UploadEndpoint = "/api/releases/upload",
                ValidateEndpoint = "/api/releases/validate",
                PlanEndpoint = "/api/releases/plan",
                DeployEndpoint = "/api/releases/deploy",
                RollbackEndpoint = "/api/releases/rollback",
                ListEndpoint = "/api/releases",
                HistoryEndpoint = "/api/releases/history",
                DetailEndpointTemplate = "/api/releases/{id}",
                TimeoutSeconds = 120,
                RetryCount = 3
            },

            ["settings.deployment.json"] = new DeploymentSettings
            {
                Enabled = true,
                AgentVersion = "unknown",
                PackageRoot = $"{runtimeParent}/packages",
                HealthCheckBaseUrl = target.PublicUrl,
                HealthCheckTimeoutSeconds = 60,
                HealthCheckRetryCount = 5,
                HealthCheckRetryDelayMilliseconds = 3000,
                AutomaticRollbackEnabled = true,
                ComposeProjectNamePrefix = "aovias",
                ContainerHealthTimeoutSeconds = 120,
                ContainerHealthPollIntervalMilliseconds = 3000
            },

            // Root has no [Required] attribute (it is legitimately derivable from the legacy
            // ReleaseManagement:ReleaseRoot key at runtime — see
            // Aovias.Configuration.Deployment.DeploymentStorageOptionsConfigurator) but Bootstrap
            // knows the real value already, so there is no reason to lean on that fallback chain.
            ["settings.deployment-storage.json"] = new DeploymentStorageSettings
            {
                Root = PlatformDeploymentDefaults.DeploymentRoot
            },

            ["settings.orchestration.json"] = new OrchestrationSettings
            {
                ExecutionTimeoutSeconds = 3600,
                WorkflowTimeoutSeconds = 1800,
                MaximumWorkers = 4,
                ParallelExecutionEnabled = false,
                RetryCount = 3,
                RetryDelayMilliseconds = 500
            },

            ["settings.docker.json"] = new DockerSettings
            {
                Enabled = true,
                Host = target.Host,
                Socket = "unix:///var/run/docker.sock",
                ApiVersion = "1.45",
                Network = target.DockerNetworkName,
                ComposeRoot = $"{runtimeParent}/compose",
                VolumesRoot = $"{runtimeParent}/volumes"
            },

            ["settings.providers.json"] = new ProviderSettings
            {
                DockerEnabled = true,
                FileSystemEnabled = true,
                HealthEnabled = true,
                NetworkEnabled = true,
                // Bootstrap does not provision nginx or certificates (see settings.infrastructure.json
                // above) — declaring these providers enabled would promise capabilities nothing set up.
                NginxEnabled = false,
                CertificateEnabled = false,
                RuntimeEndpoint = "/api/runtime"
            },

            // Subnet/Gateway are [Required] on the class but Docker itself assigns both when the
            // network is created without an explicit IPAM config (BootstrapOrchestrator.
            // RunDockerNetworkCreationAsync never specifies one) — Bootstrap has no real value to put
            // here, and the Runtime Agent's own custom validator
            // (ModularConfigurationExtensions.RegisterValidatedNetworkOptions) does not actually
            // enforce these two despite the attribute, only DockerNetwork. "auto" documents that
            // honestly rather than fabricating a CIDR Bootstrap never configured.
            ["settings.network.json"] = new NetworkSettings
            {
                DockerNetwork = target.DockerNetworkName,
                Subnet = "auto",
                Gateway = "auto"
            },

            ["settings.signalr.json"] = new SignalRSettings
            {
                Enabled = true,
                HubPath = "/hubs/deployment",
                KeepAliveSeconds = 15,
                ClientTimeoutSeconds = 60
            }
        };
    }

    /// <summary>Serializes every file from <see cref="BuildRequiredProfileSettings"/> to the JSON content Bootstrap must upload — the same typed instances <c>RuntimeConfigurationValidator</c> validates, so what gets validated is exactly what gets written, never two independently-drifting representations.</summary>
    public static IReadOnlyDictionary<string, string> BuildAllRequiredProfileFiles(BootstrapTarget target)
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        return BuildRequiredProfileSettings(target)
            .ToDictionary(pair => pair.Key, pair => JsonSerializer.Serialize(pair.Value, pair.Value.GetType(), options));
    }
}
