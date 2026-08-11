using System.IO.Compression;
using System.Net.Http;
using System.Text;
using Aovias.DeploymentValidation.Models;
using Aovias.DeploymentValidation.Validators;
using Aovias.Publishing.Contracts;
using Microsoft.Extensions.Logging;

namespace Aovias.DeploymentValidation.Services;

/// <summary>
/// Temporary workspace used to simulate deployment during validation.
/// </summary>
public sealed class TemporaryDeploymentWorkspace : IAsyncDisposable
{
    private readonly IValidationCommandRunner commandRunner;
    private readonly ILogger<TemporaryDeploymentWorkspace> logger;
    private readonly string networkName;
    private bool disposed;

    private readonly string? serviceImageTarPath;

    private TemporaryDeploymentWorkspace(
        IValidationCommandRunner commandRunner,
        ILogger<TemporaryDeploymentWorkspace> logger,
        string rootPath,
        string extractedPath,
        string composePath,
        string envPath,
        string configRoot,
        string networkName,
        string? serviceImageTarPath = null)
    {
        this.commandRunner = commandRunner;
        this.logger = logger;
        RootPath = rootPath;
        ExtractedPackagePath = extractedPath;
        ComposeFilePath = composePath;
        EnvFilePath = envPath;
        ValidationConfigRoot = configRoot;
        this.networkName = networkName;
        this.serviceImageTarPath = serviceImageTarPath;
    }

    public static TemporaryDeploymentWorkspace OpenPrepared(
        IValidationCommandRunner commandRunner,
        ILogger<TemporaryDeploymentWorkspace> logger,
        string rootPath,
        string extractedPath,
        string composePath,
        string envPath,
        string configRoot,
        string networkName,
        string? serviceImageTarPath)
        => new(
            commandRunner,
            logger,
            rootPath,
            extractedPath,
            composePath,
            envPath,
            configRoot,
            networkName,
            serviceImageTarPath);

    public string RootPath { get; }

    public string ExtractedPackagePath { get; }

    public string ComposeFilePath { get; }

    public string EnvFilePath { get; }

    public string ValidationConfigRoot { get; }

    public string HostLogsRoot => Path.Combine(RootPath, "logs");

    public string HostPackagesRoot => Path.Combine(RootPath, "packages");

    public string HostDeploymentsRoot => Path.Combine(RootPath, "deployments");

    public static async Task<TemporaryDeploymentWorkspace> CreateAsync(
        DeploymentValidationContext context,
        IValidationCommandRunner commandRunner,
        ILogger<TemporaryDeploymentWorkspace> logger,
        CancellationToken cancellationToken)
    {
        var root = Path.Combine(
            context.WorkingDirectory ?? Path.GetTempPath(),
            "drv-workspace",
            Guid.NewGuid().ToString("N"));

        var extracted = Path.Combine(root, "package");
        Directory.CreateDirectory(extracted);
        ZipFile.ExtractToDirectory(context.PackagePath, extracted);

        var configRoot = Path.Combine(root, "config");
        await SeedValidationConfigAsync(configRoot, context, cancellationToken);

        var composePath = Path.Combine(extracted, DeployPackageLayout.ComposeFile);
        ValidationComposePortPatcher.PatchComposeFile(composePath);
        var envPath = Path.Combine(root, ".env");
        var envTemplatePath = Path.Combine(extracted, DeployPackageLayout.EnvTemplateFile);
        var envContent = File.Exists(envTemplatePath)
            ? await File.ReadAllTextAsync(envTemplatePath, cancellationToken)
            : string.Empty;
        var validationNetworkName = "aovias-drv-" + Guid.NewGuid().ToString("N")[..12];
        envContent = BuildRuntimeEnvironmentFile(envContent, configRoot, root, context, validationNetworkName);
        await File.WriteAllTextAsync(envPath, envContent, cancellationToken);

        Directory.CreateDirectory(Path.Combine(root, "packages"));
        Directory.CreateDirectory(Path.Combine(root, "logs"));
        Directory.CreateDirectory(Path.Combine(root, "deployments"));
        Directory.CreateDirectory(Path.Combine(root, "evidence"));

        await EnsureDockerNetworkAsync(context, commandRunner, validationNetworkName, cancellationToken);

        return new TemporaryDeploymentWorkspace(
            commandRunner,
            logger,
            root,
            extracted,
            composePath,
            envPath,
            configRoot,
            validationNetworkName);
    }

    public static string BuildRuntimeEnvironmentFile(
        string templateContent,
        string configRoot,
        string root,
        DeploymentValidationContext context,
        string dockerNetworkName)
    {
        var lines = new List<string>();
        if (!string.IsNullOrWhiteSpace(templateContent))
        {
            lines.AddRange(templateContent.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        }
        else
        {
            lines.AddRange(
            [
                "DOTNET_ENVIRONMENT=Production",
                "AOVIAS_CONFIG_ROOT=/opt/aovias/runtime/config",
                $"AOVIAS_ACTIVE_PROFILE={context.ValidationProfile}",
                "AOVIAS_PROFILES_DIRECTORY=profiles",
                "AOVIAS_SECRETS_DIRECTORY=secrets",
                "AOVIAS_PACKAGES_ROOT=/opt/aovias/runtime/packages",
                "AOVIAS_LOGS_ROOT=/opt/aovias/runtime/logs",
                $"AOVIAS_DOCKER_NETWORK={dockerNetworkName}"
            ]);
        }

        UpsertEnv(lines, "AOVIAS_HOST_CONFIG_ROOT", ToEnvPath(configRoot));
        UpsertEnv(lines, "AOVIAS_HOST_PACKAGES_ROOT", ToEnvPath(Path.Combine(root, "packages")));
        UpsertEnv(lines, "AOVIAS_HOST_LOGS_ROOT", ToEnvPath(Path.Combine(root, "logs")));
        // Sprint G1: any other AOVIAS_HOST_* token declared in DeploymentEnvironmentTemplate's
        // canonical variable list (Aovias.Publishing.Core) is written into the package's
        // .env.template as its own unexpanded placeholder (e.g. AOVIAS_HOST_DEPLOYMENTS_ROOT=
        // ${AOVIAS_HOST_DEPLOYMENTS_ROOT}), meant to be filled in by a real deployment's host
        // environment. `docker compose config` cannot resolve a self-referencing placeholder
        // with no OS environment variable behind it, so it silently substitutes an empty string,
        // producing "invalid spec: empty section between colons". Every host-path token used by
        // any package's volume mounts must get a real workspace directory here, the same as the
        // three above — this line was missing for AOVIAS_HOST_DEPLOYMENTS_ROOT (runtime-agent)
        // and AOVIAS_HOST_EVIDENCE_ROOT (compliance-service), so both failed the same way the
        // first time a "Publish Solution" run actually reached this validation stage.
        UpsertEnv(lines, "AOVIAS_HOST_DEPLOYMENTS_ROOT", ToEnvPath(Path.Combine(root, "deployments")));
        UpsertEnv(lines, "AOVIAS_HOST_EVIDENCE_ROOT", ToEnvPath(Path.Combine(root, "evidence")));
        UpsertEnv(lines, "AOVIAS_ACTIVE_PROFILE", context.ValidationProfile);
        UpsertEnv(lines, "AOVIAS_DOCKER_NETWORK", dockerNetworkName);
        return string.Join(Environment.NewLine, lines) + Environment.NewLine;
    }

    private static void UpsertEnv(List<string> lines, string key, string value)
    {
        for (var index = 0; index < lines.Count; index++)
        {
            if (lines[index].StartsWith($"{key}=", StringComparison.OrdinalIgnoreCase))
            {
                lines[index] = $"{key}={value}";
                return;
            }
        }

        lines.Add($"{key}={value}");
    }

    private static string? ReadEnvValue(string envContent, string key)
    {
        foreach (var line in envContent.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (line.StartsWith($"{key}=", StringComparison.OrdinalIgnoreCase))
            {
                return line[(key.Length + 1)..].Trim();
            }
        }

        return null;
    }

    public static async Task EnsureDockerNetworkAsync(
        DeploymentValidationContext context,
        IValidationCommandRunner commandRunner,
        string networkName,
        CancellationToken cancellationToken)
    {
        var inspect = await commandRunner.RunAsync(
            context.DockerExecutable,
            $"network inspect {networkName}",
            context.WorkingDirectory ?? string.Empty,
            60,
            context,
            cancellationToken);

        if (inspect.IsSuccess)
        {
            return;
        }

        await commandRunner.RunAsync(
            context.DockerExecutable,
            $"network create {networkName}",
            context.WorkingDirectory ?? string.Empty,
            60,
            context,
            cancellationToken);
    }

    public async Task<ValidationCommandResult> LoadImageAsync(
        DeploymentValidationContext context,
        CancellationToken cancellationToken)
    {
        var imageTar = !string.IsNullOrWhiteSpace(context.ServiceImageTarPath) && File.Exists(context.ServiceImageTarPath)
            ? context.ServiceImageTarPath
            : !string.IsNullOrWhiteSpace(serviceImageTarPath) && File.Exists(serviceImageTarPath)
                ? serviceImageTarPath
                : Path.Combine(ExtractedPackagePath, DeployPackageLayout.ServiceImageTar);

        var load = await commandRunner.RunAsync(
            context.DockerExecutable,
            $"load -i \"{imageTar}\"",
            ExtractedPackagePath,
            context.TimeoutSeconds,
            context,
            cancellationToken);

        if (load.IsSuccess && ShouldDeleteLoadedTar(imageTar))
        {
            var tarBytes = DrvResourceMonitor.GetDirectorySizeBytes(imageTar);
            try
            {
                File.Delete(imageTar);
                context.ResourceMonitor?.RecordFreed(tarBytes);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to delete service image tar after docker load.");
            }
        }

        return load;
    }

    private bool ShouldDeleteLoadedTar(string imageTar)
    {
        if (!File.Exists(imageTar))
        {
            return false;
        }

        var tarPath = Path.GetFullPath(imageTar)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var extractedRoot = Path.GetFullPath(ExtractedPackagePath)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        return tarPath.StartsWith(extractedRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
               || string.Equals(Path.GetDirectoryName(tarPath), extractedRoot, StringComparison.OrdinalIgnoreCase);
    }

    public async Task<ValidationCommandResult> ComposeUpAsync(
        DeploymentValidationContext context,
        CancellationToken cancellationToken)
        => await RunComposeAsync(context, "up -d", cancellationToken);

    public async Task ResolvePublishedHostPortAsync(
        DeploymentValidationContext context,
        CancellationToken cancellationToken)
    {
        var containerName = context.PackageContents?.Manifest.Container;
        var containerPort = context.ResolveContainerPort();
        if (string.IsNullOrWhiteSpace(containerName) || containerPort <= 0)
        {
            return;
        }

        var mapping = await commandRunner.RunAsync(
            context.DockerExecutable,
            $"port {containerName} {containerPort}/tcp",
            ExtractedPackagePath,
            context.TimeoutSeconds,
            context,
            cancellationToken);

        if (mapping.IsSuccess
            && ValidationComposePortPatcher.TryParseDockerPortMapping(mapping.StandardOutput, out var hostPort))
        {
            context.ResolvedHostProbePort = hostPort;
            logger.LogInformation(
                "Resolved DRV host probe port {HostPort} for container {Container} (container port {ContainerPort}).",
                hostPort,
                containerName,
                containerPort);
        }
    }

    public async Task<ValidationCommandResult> ComposeDownAsync(
        DeploymentValidationContext context,
        CancellationToken cancellationToken)
        => await RunComposeAsync(context, "down --remove-orphans", cancellationToken);

    public async Task<IReadOnlyList<ApiProbeResult>> ProbeRuntimeEndpointsAsync(
        DeploymentValidationContext context,
        CancellationToken cancellationToken)
    {
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
        var probePort = context.ResolveProbePort();
        var baseUrl = $"http://127.0.0.1:{probePort}";
        var health = context.ResolveHealthContract();
        var healthEndpoint = health.HealthUrl;
        var probes = new List<(string Method, string Path, int Expected)>
        {
            ("GET", healthEndpoint, health.ExpectedStatus)
        };

        foreach (var probe in health.AdditionalProbes)
        {
            probes.Add((probe.Method, probe.Path, probe.ExpectedStatus));
        }

        var distinctProbes = probes
            .GroupBy(probe => $"{probe.Method} {probe.Path}", StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToList();

        var retries = Math.Max(health.Retries, 1);
        var delaySeconds = Math.Max(health.TimeoutSeconds, 1);
        var results = new List<ApiProbeResult>();

        for (var attempt = 0; attempt < retries; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            results.Clear();

            foreach (var (method, path, expected) in distinctProbes)
            {
                try
                {
                    using var request = new HttpRequestMessage(new HttpMethod(method), $"{baseUrl}{path}");
                    if (path.StartsWith("/api/deployment/", StringComparison.OrdinalIgnoreCase)
                        && !path.Equals("/api/deployment/status", StringComparison.OrdinalIgnoreCase))
                    {
                        var apiKey = ValidationApiKeyResolver.Resolve(context);
                        if (!string.IsNullOrWhiteSpace(apiKey))
                        {
                            request.Headers.TryAddWithoutValidation("X-Api-Key", apiKey);
                        }
                    }

                    using var response = await client.SendAsync(request, cancellationToken);
                    var body = await response.Content.ReadAsStringAsync(cancellationToken);
                    results.Add(new ApiProbeResult
                    {
                        Method = method,
                        Endpoint = path,
                        ExpectedStatus = expected,
                        StatusCode = (int)response.StatusCode,
                        Body = Truncate(body)
                    });
                }
                catch (Exception exception)
                {
                    results.Add(new ApiProbeResult
                    {
                        Method = method,
                        Endpoint = path,
                        ExpectedStatus = expected,
                        StatusCode = 0,
                        Body = exception.Message
                    });
                }
            }

            if (results.Any(result =>
                    result.Endpoint.Equals(healthEndpoint, StringComparison.OrdinalIgnoreCase)
                    && result.StatusCode == health.ExpectedStatus))
            {
                return results;
            }

            if (attempt < retries - 1)
            {
                await Task.Delay(TimeSpan.FromSeconds(delaySeconds), cancellationToken);
            }
        }

        return results;
    }

    private static string Truncate(string value)
        => value.Length <= 2000 ? value : value[..2000];

    public async ValueTask DisposeAsync()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        try
        {
            var context = new DeploymentValidationContext
            {
                PackagePath = string.Empty,
                DockerExecutable = "docker",
                WorkingDirectory = RootPath
            };
            await ComposeDownAsync(context, CancellationToken.None);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Failed to tear down compose stack during DRV cleanup.");
        }

        try
        {
            if (!string.IsNullOrWhiteSpace(networkName))
            {
                var context = new DeploymentValidationContext
                {
                    PackagePath = string.Empty,
                    DockerExecutable = "docker",
                    WorkingDirectory = RootPath
                };
                await commandRunner.RunAsync(
                    context.DockerExecutable,
                    $"network rm {networkName}",
                    RootPath,
                    60,
                    context,
                    CancellationToken.None);
            }
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Failed to remove validation network during DRV cleanup.");
        }

        try
        {
            if (Directory.Exists(RootPath))
            {
                Directory.Delete(RootPath, recursive: true);
            }
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Failed to delete temporary deployment workspace.");
        }
    }

    private async Task<ValidationCommandResult> RunComposeAsync(
        DeploymentValidationContext context,
        string subcommand,
        CancellationToken cancellationToken)
        => await commandRunner.RunAsync(
            context.DockerExecutable,
            $"compose --env-file \"{EnvFilePath}\" -f \"{ComposeFilePath}\" {subcommand}",
            ExtractedPackagePath,
            context.TimeoutSeconds,
            context,
            cancellationToken);

    private static async Task SeedValidationConfigAsync(
        string configRoot,
        DeploymentValidationContext context,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(context.ValidationConfigRoot)
            && Directory.Exists(context.ValidationConfigRoot))
        {
            ValidationConfigSeeder.Seed(
                context.ValidationConfigRoot,
                configRoot,
                context.ValidationProfile,
                context.ResolveDeploymentBundle()?.Certificate);
            return;
        }

        var profileDirectory = Path.Combine(configRoot, "profiles", context.ValidationProfile);
        Directory.CreateDirectory(profileDirectory);
        await File.WriteAllTextAsync(
            Path.Combine(profileDirectory, "settings.api.json"),
            $$"""
              {
                "enabled": true,
                "internalUrl": "http://0.0.0.0:{{context.ContainerPort}}",
                "healthEndpoint": "/health",
                "pathBase": ""
              }
              """,
            cancellationToken);
        await File.WriteAllTextAsync(
            Path.Combine(profileDirectory, "settings.logging.json"),
            """
              {
                "enabled": true,
                "minimumLevel": "Information"
              }
              """,
            cancellationToken);
        await File.WriteAllTextAsync(
            Path.Combine(profileDirectory, "settings.runtime.json"),
            """
              {
                "enabled": true
              }
              """,
            cancellationToken);
        await File.WriteAllTextAsync(
            Path.Combine(profileDirectory, "settings.security.json"),
            """
              {
                "enabled": false
              }
              """,
            cancellationToken);

        IdentitySigningCertificateProvisioner.EnsureProvisioned(
            configRoot,
            context.ResolveDeploymentBundle()?.Certificate);
    }

    public static void PatchValidationLoggingSettings(string configRoot, string validationProfile)
    {
        var loggingPath = Path.Combine(configRoot, "profiles", validationProfile, "settings.logging.json");
        if (!File.Exists(loggingPath))
        {
            return;
        }

        var content = File.ReadAllText(loggingPath);
        if (content.Contains("/opt/aovias/logs", StringComparison.OrdinalIgnoreCase))
        {
            content = content.Replace(
                "/opt/aovias/logs",
                "/opt/aovias/runtime/logs",
                StringComparison.OrdinalIgnoreCase);
        }

        if (content.Contains("\"logRoot\"", StringComparison.OrdinalIgnoreCase))
        {
            content = content.Replace(
                "\"logRoot\": \"/var/log/aovias/",
                "\"logRoot\": \"/opt/aovias/runtime/logs/",
                StringComparison.OrdinalIgnoreCase);
        }

        File.WriteAllText(loggingPath, content);
    }

    /// <summary>
    /// Rewrites host-specific deployment paths to container-neutral locations used by DRV bootstrap probes.
    /// </summary>
    public static void PatchValidationRuntimePaths(string configRoot, string validationProfile)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(configRoot);
        ArgumentException.ThrowIfNullOrWhiteSpace(validationProfile);

        var profileDirectory = Path.Combine(configRoot, "profiles", validationProfile);
        Directory.CreateDirectory(profileDirectory);

        File.WriteAllText(
            Path.Combine(profileDirectory, "settings.deployment-host.json"),
            """
              {
                "dotNetEnvironment": "Production",
                "configRoot": "/opt/aovias/runtime/config",
                "packagesRoot": "/opt/aovias/runtime/packages",
                "logsRoot": "/opt/aovias/runtime/logs",
                "hostConfigRoot": "/opt/aovias/runtime/config",
                "hostPackagesRoot": "/opt/aovias/runtime/packages",
                "hostLogsRoot": "/opt/aovias/runtime/logs",
                "dockerNetwork": "aovias-network",
                "profilesDirectory": "profiles",
                "secretsDirectory": "secrets"
              }
              """);

        File.WriteAllText(
            Path.Combine(profileDirectory, "settings.deployment-storage.json"),
            """
              {
                "root": "/opt/aovias/runtime/deployments",
                "incoming": "incoming",
                "releases": "releases",
                "temp": "temp",
                "archive": "archive",
                "staging": "staging",
                "extraction": "extraction",
                "active": "active",
                "rollback": "rollback",
                "audit": "audit"
              }
              """);
    }

    public static string BuildBootstrapProbeDockerArguments(
        string image,
        string entryPoint,
        string validationProfile,
        TemporaryDeploymentWorkspace workspace)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(image);
        ArgumentException.ThrowIfNullOrWhiteSpace(entryPoint);
        ArgumentException.ThrowIfNullOrWhiteSpace(validationProfile);
        ArgumentNullException.ThrowIfNull(workspace);

        const string containerConfigRoot = "/opt/aovias/runtime/config";
        const string containerLogsRoot = "/opt/aovias/runtime/logs";
        const string containerPackagesRoot = "/opt/aovias/runtime/packages";
        const string containerDeploymentsRoot = "/opt/aovias/runtime/deployments";

        return string.Join(
            ' ',
            [
                "run --rm",
                $"-e AOVIAS_CONFIG_ROOT={containerConfigRoot}",
                $"-e AOVIAS_ACTIVE_PROFILE={validationProfile}",
                "-e DOTNET_ENVIRONMENT=Production",
                $"-v \"{workspace.ValidationConfigRoot}:{containerConfigRoot}:ro\"",
                $"-v \"{workspace.HostLogsRoot}:{containerLogsRoot}:rw\"",
                $"-v \"{workspace.HostPackagesRoot}:{containerPackagesRoot}:rw\"",
                $"-v \"{workspace.HostDeploymentsRoot}:{containerDeploymentsRoot}:rw\"",
                "--entrypoint dotnet",
                image,
                entryPoint
            ]);
    }

    private static string ToEnvPath(string path) => path.Replace('\\', '/');

    public static void CopyDirectory(string source, string destination)
    {
        Directory.CreateDirectory(destination);
        foreach (var directory in Directory.GetDirectories(source, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(source, directory);
            Directory.CreateDirectory(Path.Combine(destination, relative));
        }

        foreach (var file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(source, file);
            var target = Path.Combine(destination, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(file, target, overwrite: true);
        }
    }
}
