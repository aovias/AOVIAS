using System.Diagnostics;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;
using Aovias.Deployment.Contracts.Validation;
using Aovias.DeploymentValidation.Contracts;
using Aovias.Configuration.Loading;
using Aovias.DeploymentValidation.Models;
using Aovias.DeploymentValidation.Rules;
using Aovias.DeploymentValidation.Services;
using Aovias.Publishing.Contracts;
using Microsoft.Extensions.Logging;

namespace Aovias.DeploymentValidation.Validators;

public sealed class PackageValidationStage : IDeploymentValidationStage
{
    public string StageName => "Package";

    public ValidationStageDescriptor Descriptor { get; } = new(
        ValidationCategory.Package,
        ValidationExecutionScope.Publishing);

    public Task<ValidationStageResult> ValidateAsync(
        DeploymentValidationContext context,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var errors = new List<string>();
        var warnings = new List<string>();

        if (!File.Exists(context.PackagePath))
        {
            return Task.FromResult(ValidationStageResult.Fail(StageName, stopwatch.Elapsed, ["Package file was not found."]));
        }

        if (!context.PackagePath.EndsWith(DeployPackageLayout.Extension, StringComparison.OrdinalIgnoreCase))
        {
            errors.Add("Package extension must be .aovdeploy.");
        }

        using (var archive = ZipFile.OpenRead(context.PackagePath))
        {
            foreach (var required in DeployPackageLayout.RequiredFiles)
            {
                if (archive.GetEntry(required) is null)
                {
                    errors.Add($"Missing required file: {required}");
                }
            }

            var checksumEntry = archive.GetEntry(DeployPackageLayout.ChecksumsFile);
            if (checksumEntry is not null)
            {
                using var stream = checksumEntry.Open();
                using var document = JsonDocument.Parse(stream);
                foreach (var property in document.RootElement.EnumerateObject())
                {
                    var entry = archive.GetEntry(property.Name);
                    if (entry is null)
                    {
                        errors.Add($"Checksum references missing file: {property.Name}");
                        continue;
                    }

                    using var entryStream = entry.Open();
                    using var memory = new MemoryStream();
                    entryStream.CopyTo(memory);
                    var actual = Convert.ToHexString(SHA256.HashData(memory.ToArray())).ToLowerInvariant();
                    if (!string.Equals(actual, property.Value.GetString(), StringComparison.OrdinalIgnoreCase))
                    {
                        errors.Add($"Checksum mismatch for {property.Name}.");
                    }
                }
            }
            else
            {
                warnings.Add("Checksum file was not found.");
            }
        }

        var contents = context.PackageContents;
        if (contents is null)
        {
            errors.Add("Unable to parse deployment package contents.");
        }
        else
        {
            if (string.IsNullOrWhiteSpace(contents.Manifest.Service))
            {
                errors.Add("Manifest service name is required.");
            }

            if (string.IsNullOrWhiteSpace(contents.Metadata.Publisher))
            {
                errors.Add("Metadata publisher is required.");
            }

            if (string.IsNullOrWhiteSpace(contents.Metadata.PackageVersion))
            {
                errors.Add("Metadata package version is required.");
            }

            if (!contents.HasServiceImage)
            {
                errors.Add("service-image.tar is missing or empty.");
            }

            var allowWindowsHostPaths = ProfileNeutralityPolicy.AllowsWindowsHostPaths(contents, context.ValidationConfigRoot);

            errors.AddRange(PackageArtifactScanner.ScanText(
                JsonSerializer.Serialize(contents.Metadata),
                DeployPackageLayout.MetadataFile));
            errors.AddRange(PackageArtifactScanner.ScanText(
                JsonSerializer.Serialize(contents.Manifest),
                DeployPackageLayout.ManifestFile));
            errors.AddRange(PackageArtifactScanner.ScanText(
                contents.ComposeContent,
                DeployPackageLayout.ComposeFile,
                EnvironmentNeutralScanProfile.ComposeFile));
            errors.AddRange(PackageArtifactScanner.ScanText(
                contents.EnvTemplateContent,
                DeployPackageLayout.EnvTemplateFile,
                EnvironmentNeutralScanProfile.PackageArtifact,
                allowWindowsHostPaths));
            errors.AddRange(EnvTemplateValidationRules.Validate(
                contents.EnvTemplateContent,
                DeployPackageLayout.EnvTemplateFile));
            errors.AddRange(PackageArtifactScanner.ScanText(
                contents.ConfigurationTemplateContent,
                DeployPackageLayout.ConfigTemplateFile));
        }

        stopwatch.Stop();
        return Task.FromResult(errors.Count == 0
            ? ValidationStageResult.Pass(StageName, stopwatch.Elapsed, warnings)
            : ValidationStageResult.Fail(StageName, stopwatch.Elapsed, errors, warnings));
    }
}

public sealed class DockerImageValidationStage : IDeploymentValidationStage
{
    private readonly IValidationCommandRunner commandRunner;
    private readonly ITemporaryDeploymentWorkspaceFactory workspaceFactory;

    public DockerImageValidationStage(
        IValidationCommandRunner commandRunner,
        ITemporaryDeploymentWorkspaceFactory workspaceFactory)
    {
        this.commandRunner = commandRunner;
        this.workspaceFactory = workspaceFactory;
    }

    public string StageName => "Compatibility";

    public ValidationStageDescriptor Descriptor { get; } = new(
        ValidationCategory.Package,
        ValidationExecutionScope.Publishing);

    public async Task<ValidationStageResult> ValidateAsync(
        DeploymentValidationContext context,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var errors = new List<string>();
        var warnings = new List<string>();
        var contents = context.PackageContents
            ?? throw new InvalidOperationException("Package contents are required for Docker image validation.");

        context.Workspace ??= await workspaceFactory.CreateAsync(context, cancellationToken);

        var load = await context.Workspace.LoadImageAsync(context, cancellationToken);
        if (!load.IsSuccess)
        {
            return ValidationStageResult.Fail(StageName, stopwatch.Elapsed, [$"Docker load failed: {load.StandardError}"]);
        }

        var imageReference = $"{contents.Manifest.ImageName}:{contents.Manifest.ImageTag}";
        context.LoadedImageReference = imageReference;

        var inspect = await commandRunner.RunAsync(
            context.DockerExecutable,
            $"inspect {imageReference} --format \"{{{{.Os}}}}|{{{{json .Config.ExposedPorts}}}}|{{{{json .Config.Healthcheck}}}}|{{{{.Config.Entrypoint}}}}\"",
            context.Workspace.ExtractedPackagePath,
            context.TimeoutSeconds,
            context,
            cancellationToken);

        if (!inspect.IsSuccess)
        {
            return ValidationStageResult.Fail(StageName, stopwatch.Elapsed, [$"Docker inspect failed: {inspect.StandardError}"]);
        }

        var parts = inspect.StandardOutput.Trim().Split('|', 4);
        if (parts.Length >= 1 && !string.Equals(parts[0], "linux", StringComparison.OrdinalIgnoreCase))
        {
            errors.Add($"Image OS must be linux. Found '{parts[0]}'.");
        }

        if (contents.Manifest.Ports.Count > 0
            && !string.Equals(contents.Manifest.Type, nameof(PublishingProjectType.WorkerService), StringComparison.OrdinalIgnoreCase)
            && parts.Length >= 2
            && (string.IsNullOrWhiteSpace(parts[1]) || parts[1] == "null"))
        {
            errors.Add("Image must expose at least one port.");
        }

        if (parts.Length >= 3 && (string.IsNullOrWhiteSpace(parts[2]) || parts[2] == "null"))
        {
            warnings.Add("Image healthcheck is not configured.");
        }

        if (parts.Length >= 4 && string.IsNullOrWhiteSpace(parts[3]))
        {
            errors.Add("Image entrypoint is missing.");
        }

        stopwatch.Stop();
        return errors.Count == 0
            ? ValidationStageResult.Pass(StageName, stopwatch.Elapsed, warnings)
            : ValidationStageResult.Fail(StageName, stopwatch.Elapsed, errors, warnings);
    }
}

public sealed class ComposeValidationStage : IDeploymentValidationStage
{
    private readonly IValidationCommandRunner commandRunner;
    private readonly ITemporaryDeploymentWorkspaceFactory workspaceFactory;

    public ComposeValidationStage(
        IValidationCommandRunner commandRunner,
        ITemporaryDeploymentWorkspaceFactory workspaceFactory)
    {
        this.commandRunner = commandRunner;
        this.workspaceFactory = workspaceFactory;
    }

    public string StageName => "Docker Compose";

    public ValidationStageDescriptor Descriptor { get; } = new(
        ValidationCategory.Deployment,
        ValidationExecutionScope.Publishing);

    public async Task<ValidationStageResult> ValidateAsync(
        DeploymentValidationContext context,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var errors = new List<string>();
        var warnings = new List<string>();

        context.Workspace ??= await workspaceFactory.CreateAsync(context, cancellationToken);

        var composeContent = context.PackageContents?.ComposeContent ?? string.Empty;
        var manifest = context.PackageContents?.Manifest;
        if (string.IsNullOrWhiteSpace(composeContent))
        {
            return ValidationStageResult.Fail(StageName, stopwatch.Elapsed, ["docker-compose.yml is empty."]);
        }

        foreach (var token in new[] { "AOVIAS_CONFIG_ROOT", "volumes:", "restart:", "healthcheck:", "AOVIAS_DOCKER_NETWORK" })
        {
            if (!composeContent.Contains(token, StringComparison.OrdinalIgnoreCase))
            {
                errors.Add($"Compose file is missing required token '{token}'.");
            }
        }

        if (manifest?.Ports.Count > 0 && !composeContent.Contains("ports:", StringComparison.OrdinalIgnoreCase))
        {
            errors.Add("Compose file must expose HTTP ports for the runtime service.");
        }

        foreach (var hardcodedNetwork in EnvironmentNeutralRules.HardcodedNetworkNames)
        {
            if (composeContent.Contains(hardcodedNetwork, StringComparison.OrdinalIgnoreCase))
            {
                errors.Add($"Compose file contains hardcoded network name '{hardcodedNetwork}'.");
            }
        }

        if (!composeContent.Contains("${AOVIAS_DOCKER_NETWORK}", StringComparison.Ordinal))
        {
            errors.Add("Compose file must reference ${AOVIAS_DOCKER_NETWORK} instead of a hardcoded network.");
        }

        // Neutral templates declare ${AOVIAS_DOCKER_NETWORK}; resolved runtime .env
        // is validated separately once the workspace materializes profile values.
        var declaredNetwork = ProfileNeutralityPolicy.ReadDockerNetwork(context.PackageContents?.EnvTemplateContent);
        if (string.IsNullOrWhiteSpace(declaredNetwork))
        {
            errors.Add("Package .env.template is missing AOVIAS_DOCKER_NETWORK.");
        }
        else if (!IsEnvironmentReference(declaredNetwork)
                 && !string.Equals(declaredNetwork, context.ExpectedDockerNetwork, StringComparison.Ordinal))
        {
            errors.Add($"Package declares Docker network '{declaredNetwork}' but '{context.ExpectedDockerNetwork}' was expected.");
        }

        errors.AddRange(PackageArtifactScanner.ScanText(
            composeContent,
            DeployPackageLayout.ComposeFile,
            EnvironmentNeutralScanProfile.ComposeFile));

        var envContent = await File.ReadAllTextAsync(context.Workspace.EnvFilePath, cancellationToken);
        var networkName = envContent.Split('\n')
            .Select(line => line.Trim())
            .FirstOrDefault(line => line.StartsWith("AOVIAS_DOCKER_NETWORK=", StringComparison.OrdinalIgnoreCase))
            ?.Split('=', 2)[1];

        if (string.IsNullOrWhiteSpace(networkName))
        {
            errors.Add("Runtime .env is missing AOVIAS_DOCKER_NETWORK.");
        }
        else
        {
            var inspectNetwork = await commandRunner.RunAsync(
                context.DockerExecutable,
                $"network inspect {networkName}",
                context.Workspace.ExtractedPackagePath,
                context.TimeoutSeconds,
                context,
                cancellationToken);

            if (!inspectNetwork.IsSuccess)
            {
                errors.Add($"Configured Docker network '{networkName}' does not exist.");
            }
        }

        var config = await commandRunner.RunAsync(
            context.DockerExecutable,
            $"compose --env-file \"{context.Workspace.EnvFilePath}\" -f \"{context.Workspace.ComposeFilePath}\" config",
            context.Workspace.ExtractedPackagePath,
            context.TimeoutSeconds,
            context,
            cancellationToken);

        if (!config.IsSuccess)
        {
            errors.Add($"docker compose config failed: {config.StandardError}");
        }

        var contracts = context.DeploymentContracts ?? context.PackageContents?.DeploymentContracts;
        if (contracts is not null)
        {
            var mountValidation = ComposeMountValidator.Validate(composeContent, contracts.Volume, contracts.Filesystem);
            foreach (var check in mountValidation.Checks.Where(item => !string.Equals(item.Status, "PASS", StringComparison.OrdinalIgnoreCase)))
            {
                errors.Add($"{check.Label}: {check.Detail}");
            }

            foreach (var structuralError in mountValidation.Errors.Where(error =>
                         !mountValidation.Checks.Any(check => check.Detail == error)))
            {
                errors.Add(structuralError);
            }
        }
        else
        {
            errors.Add("Deployment contracts are required to validate compose bind mounts.");
        }

        stopwatch.Stop();
        return errors.Count == 0
            ? ValidationStageResult.Pass(StageName, stopwatch.Elapsed, warnings)
            : ValidationStageResult.Fail(StageName, stopwatch.Elapsed, errors, warnings);
    }

    private static bool IsEnvironmentReference(string value)
        => value.StartsWith("${", StringComparison.Ordinal) && value.Contains('}', StringComparison.Ordinal);
}

public sealed class BootstrapManifestValidationStage : IDeploymentValidationStage
{
    public string StageName => "Bootstrap Manifest";

    public ValidationStageDescriptor Descriptor { get; } = new(
        ValidationCategory.Deployment,
        ValidationExecutionScope.Publishing);

    public Task<ValidationStageResult> ValidateAsync(
        DeploymentValidationContext context,
        CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        var stopwatch = Stopwatch.StartNew();
        var errors = new List<string>();
        var warnings = new List<string>();
        var recommendations = new List<string>();
        var manifest = context.PackageContents?.Manifest;
        var composeContent = context.PackageContents?.ComposeContent ?? string.Empty;
        var envTemplate = context.PackageContents?.EnvTemplateContent ?? string.Empty;
        var isRuntimeAgent = context.ResolveDeploymentBundle()?.Feature.RequiresBootstrapDiagnostics == true;

        if (!isRuntimeAgent)
        {
            stopwatch.Stop();
            return Task.FromResult(ValidationStageResult.Pass(
                StageName,
                stopwatch.Elapsed,
                recommendations: ["Bootstrap manifest validation applies to runtime-agent packages only."]));
        }

        if (manifest is null)
        {
            return Task.FromResult(ValidationStageResult.Fail(StageName, stopwatch.Elapsed, ["Package manifest is unavailable."]));
        }

        if (string.IsNullOrWhiteSpace(manifest.EntryPoint))
        {
            errors.Add("Runtime Agent manifest must declare an entry point.");
        }

        if (string.IsNullOrWhiteSpace(manifest.ImageName) || string.IsNullOrWhiteSpace(manifest.ImageTag))
        {
            errors.Add("Runtime Agent manifest must declare image name and tag.");
        }

        foreach (var token in new[] { "AOVIAS_CONFIG_ROOT", "AOVIAS_ACTIVE_PROFILE", "healthcheck:", "volumes:" })
        {
            if (!composeContent.Contains(token, StringComparison.OrdinalIgnoreCase))
            {
                errors.Add($"Runtime Agent compose file is missing required token '{token}'.");
            }
        }

        foreach (var token in new[] { "AOVIAS_CONFIG_ROOT", "AOVIAS_ACTIVE_PROFILE", "AOVIAS_LOGS_ROOT", "AOVIAS_PACKAGES_ROOT" })
        {
            if (!envTemplate.Contains(token, StringComparison.OrdinalIgnoreCase))
            {
                errors.Add($"Runtime Agent .env.template is missing required token '{token}'.");
            }
        }

        errors.AddRange(PackageArtifactScanner.ScanText(
            composeContent,
            DeployPackageLayout.ComposeFile,
            EnvironmentNeutralScanProfile.ComposeFile));
        errors.AddRange(PackageArtifactScanner.ScanText(
            envTemplate,
            DeployPackageLayout.EnvTemplateFile,
            EnvironmentNeutralScanProfile.PackageArtifact));

        var contracts = context.DeploymentContracts ?? context.PackageContents?.DeploymentContracts;
        if (contracts is not null && !string.IsNullOrWhiteSpace(composeContent))
        {
            var mountValidation = ComposeMountValidator.Validate(composeContent, contracts.Volume, contracts.Filesystem);
            foreach (var check in mountValidation.Checks.Where(item => !string.Equals(item.Status, "PASS", StringComparison.OrdinalIgnoreCase)))
            {
                errors.Add($"{check.Label}: {check.Detail}");
            }
        }
        else
        {
            warnings.Add("Deployment contracts were unavailable; compose mount validation was limited.");
        }

        recommendations.Add("Runtime startup verification is deferred until deployment.");

        stopwatch.Stop();
        return Task.FromResult(errors.Count == 0
            ? ValidationStageResult.Pass(StageName, stopwatch.Elapsed, warnings, recommendations)
            : ValidationStageResult.Fail(StageName, stopwatch.Elapsed, errors, warnings));
    }
}

public sealed class RuntimeBootstrapValidationStage : IDeploymentValidationStage
{
    private readonly IValidationCommandRunner commandRunner;
    private readonly ITemporaryDeploymentWorkspaceFactory workspaceFactory;

    public RuntimeBootstrapValidationStage(
        IValidationCommandRunner commandRunner,
        ITemporaryDeploymentWorkspaceFactory workspaceFactory)
    {
        this.commandRunner = commandRunner;
        this.workspaceFactory = workspaceFactory;
    }

    public string StageName => "Runtime Bootstrap";

    public ValidationStageDescriptor Descriptor { get; } = new(
        ValidationCategory.Runtime,
        ValidationExecutionScope.Deployment);

    public async Task<ValidationStageResult> ValidateAsync(
        DeploymentValidationContext context,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var errors = new List<string>();
        var warnings = new List<string>();
        var image = context.LoadedImageReference;
        var manifest = context.PackageContents?.Manifest;
        if (string.IsNullOrWhiteSpace(image) && manifest is not null)
        {
            image = $"{manifest.ImageName}:{manifest.ImageTag}";
        }

        if (string.IsNullOrWhiteSpace(image))
        {
            return ValidationStageResult.Fail(StageName, stopwatch.Elapsed, ["Docker image reference was not available."]);
        }

        context.Workspace ??= await workspaceFactory.CreateAsync(context, cancellationToken);

        var isRuntimeAgent = context.ResolveDeploymentBundle()?.Feature.RequiresBootstrapDiagnostics == true;
        if (!isRuntimeAgent)
        {
            stopwatch.Stop();
            return ValidationStageResult.Pass(
                StageName,
                stopwatch.Elapsed,
                recommendations: ["Bootstrap diagnostics probe applies to runtime-agent packages only."]);
        }

        var entryPoint = manifest?.EntryPoint ?? "Aovias.Runtime.Agent.Api.dll";
        var dockerArguments = TemporaryDeploymentWorkspace.BuildBootstrapProbeDockerArguments(
            image,
            entryPoint,
            context.ValidationProfile,
            context.Workspace);
        var run = await commandRunner.RunAsync(
            context.DockerExecutable,
            dockerArguments,
            context.Workspace.ExtractedPackagePath,
            Math.Min(context.TimeoutSeconds, 45),
            context,
            cancellationToken);

        var output = $"{run.StandardOutput}\n{run.StandardError}";
        errors.AddRange(PackageArtifactScanner.ScanText(
            output,
            "bootstrap container output",
            EnvironmentNeutralScanProfile.BootstrapOutput));

        if (output.Contains("V:/DevelopmentRepo/V1.0/aovias-config", StringComparison.OrdinalIgnoreCase)
            || output.Contains("V:\\DevelopmentRepo\\V1.0\\aovias-config", StringComparison.OrdinalIgnoreCase))
        {
            errors.Add("Runtime Agent fell back to legacy V1.0 developer configuration paths.");
        }

        var bootstrap = RuntimeBootstrapReportParser.Parse(output);
        var recommendations = new List<string>();
        if (bootstrap.ReportFound)
        {
            if (bootstrap.FailedModules.Count > 0)
            {
                errors.Add($"Runtime Bootstrap Report has failed modules: {string.Join(", ", bootstrap.FailedModules)}.");
            }

            if (!bootstrap.StartupSucceeded)
            {
                errors.Add("Runtime Bootstrap Report did not reach Startup SUCCESS.");
            }
            else
            {
                recommendations.Add("Runtime Agent reached Startup SUCCESS.");
            }
        }
        else
        {
            warnings.Add("Bootstrap container did not emit a Runtime Bootstrap Report; startup success could not be confirmed.");
            if (!run.IsSuccess)
            {
                errors.Add("Bootstrap container exited non-zero and produced no Runtime Bootstrap Report.");
            }
        }

        stopwatch.Stop();
        return errors.Count == 0
            ? ValidationStageResult.Pass(StageName, stopwatch.Elapsed, warnings, recommendations)
            : ValidationStageResult.Fail(StageName, stopwatch.Elapsed, errors, warnings);
    }
}

public sealed class RuntimeConfigurationValidationStage : IDeploymentValidationStage
{
    public string StageName => "Configuration";

    public ValidationStageDescriptor Descriptor { get; } = new(
        ValidationCategory.Deployment,
        ValidationExecutionScope.Publishing);

    public Task<ValidationStageResult> ValidateAsync(
        DeploymentValidationContext context,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var errors = ScanArtifacts(context);
        stopwatch.Stop();
        return Task.FromResult(errors.Count == 0
            ? ValidationStageResult.Pass(StageName, stopwatch.Elapsed)
            : ValidationStageResult.Fail(StageName, stopwatch.Elapsed, errors));
    }

    internal static List<string> ScanArtifacts(DeploymentValidationContext context)
    {
        if (context.Workspace is null)
        {
            return [];
        }

        var allowWindowsHostPaths = ProfileNeutralityPolicy.AllowsWindowsHostPaths(context.PackageContents, context.ValidationConfigRoot);
        return PackageArtifactScanner.ScanDirectory(
            context.Workspace.ExtractedPackagePath,
            includeComposeHealthcheckAllowance: true,
            allowWindowsHostPaths: allowWindowsHostPaths).ToList();
    }
}

public sealed class ContainerStartupValidationStage : IDeploymentValidationStage
{
    private readonly IValidationCommandRunner commandRunner;
    private readonly ITemporaryDeploymentWorkspaceFactory workspaceFactory;

    public ContainerStartupValidationStage(
        IValidationCommandRunner commandRunner,
        ITemporaryDeploymentWorkspaceFactory workspaceFactory)
    {
        this.commandRunner = commandRunner;
        this.workspaceFactory = workspaceFactory;
    }

    public string StageName => "Runtime";

    public ValidationStageDescriptor Descriptor { get; } = new(
        ValidationCategory.Runtime,
        ValidationExecutionScope.Deployment);

    public async Task<ValidationStageResult> ValidateAsync(
        DeploymentValidationContext context,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var errors = new List<string>();

        context.Workspace ??= await workspaceFactory.CreateAsync(context, cancellationToken);

        if (context.LoadedImageReference is null)
        {
            var load = await context.Workspace.LoadImageAsync(context, cancellationToken);
            if (!load.IsSuccess)
            {
                return ValidationStageResult.Fail(StageName, stopwatch.Elapsed, [$"Docker load failed: {load.StandardError}"]);
            }
        }

        var up = await context.Workspace.ComposeUpAsync(context, cancellationToken);
        if (!up.IsSuccess)
        {
            return ValidationStageResult.Fail(StageName, stopwatch.Elapsed, [$"docker compose up failed: {up.StandardError}"]);
        }

        await context.Workspace.ResolvePublishedHostPortAsync(context, cancellationToken);

        var startupGrace = TimeSpan.FromSeconds(Math.Max(context.ResolveHealthContract().StabilizationSeconds, 15));
        await Task.Delay(startupGrace, cancellationToken);

        var containerName = context.PackageContents?.Manifest.Container;
        if (!string.IsNullOrWhiteSpace(containerName))
        {
            var inspect = await commandRunner.RunAsync(
                context.DockerExecutable,
                $"inspect {containerName} --format \"{{{{.State.Status}}}}|{{{{.State.Restarting}}}}|{{{{.State.ExitCode}}}}\"",
                context.Workspace.ExtractedPackagePath,
                context.TimeoutSeconds,
                context,
                cancellationToken);

            if (inspect.IsSuccess)
            {
                var parts = inspect.StandardOutput.Trim().Split('|', 3);
                var status = parts.Length > 0 ? parts[0] : string.Empty;
                var restarting = parts.Length > 1 && string.Equals(parts[1], "true", StringComparison.OrdinalIgnoreCase);
                if (!string.Equals(status, "running", StringComparison.OrdinalIgnoreCase) || restarting)
                {
                    var logs = await commandRunner.RunAsync(
                        context.DockerExecutable,
                        $"logs {containerName} --tail 40",
                        context.Workspace.ExtractedPackagePath,
                        context.TimeoutSeconds,
                        context,
                        cancellationToken);

                    return ValidationStageResult.Fail(
                        StageName,
                        stopwatch.Elapsed,
                        [$"Container '{containerName}' is not running (status={status}, restarting={restarting}).",
                         logs.StandardOutput,
                         logs.StandardError]);
                }
            }
        }

        stopwatch.Stop();
        return ValidationStageResult.Pass(StageName, stopwatch.Elapsed);
    }
}

public sealed class HealthValidationStage : IDeploymentValidationStage
{
    public string StageName => "Health";

    public ValidationStageDescriptor Descriptor { get; } = new(
        ValidationCategory.Runtime,
        ValidationExecutionScope.Deployment);

    public async Task<ValidationStageResult> ValidateAsync(
        DeploymentValidationContext context,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var errors = new List<string>();
        var warnings = new List<string>();

        if (context.Workspace is null)
        {
            return ValidationStageResult.Fail(StageName, stopwatch.Elapsed, ["Temporary deployment workspace was not initialized."]);
        }

        var health = context.ResolveHealthContract();
        var healthEndpoint = health.HealthUrl;
        var probes = await context.Workspace.ProbeRuntimeEndpointsAsync(context, cancellationToken);
        context.ApiProbes.Clear();
        context.ApiProbes.AddRange(probes);

        foreach (var probe in probes)
        {
            context.ValidationLogger?.WriteHttp(probe.Endpoint, probe.StatusCode, probe.Body);
            if (probe.Succeeded)
            {
                continue;
            }

            if (probe.Endpoint.Equals(healthEndpoint, StringComparison.OrdinalIgnoreCase))
            {
                var probePort = context.ResolveProbePort();
                var probeUrl = $"http://127.0.0.1:{probePort}{probe.Endpoint}";
                var detail = probe.StatusCode == 0 && !string.IsNullOrWhiteSpace(probe.Body)
                    ? $" Connection failed: {probe.Body}"
                    : string.Empty;
                errors.Add($"Health endpoint {probe.Endpoint} returned {probe.StatusCode} (probed {probeUrl}).{detail}");
            }
            else if (IsRequiredProbe(context, probe.Endpoint) || probe.StatusCode == 404)
            {
                errors.Add($"{probe.Method} {probe.Endpoint} returned {probe.StatusCode} (expected {probe.ExpectedStatus}).");
            }
            else
            {
                warnings.Add($"{probe.Method} {probe.Endpoint} returned {probe.StatusCode} (expected {probe.ExpectedStatus}).");
            }
        }

        if (IsRuntimeAgentPackage(context))
        {
            var capabilitiesProbe = probes.FirstOrDefault(result =>
                result.Endpoint.Equals("/api/runtime/capabilities", StringComparison.OrdinalIgnoreCase));
            if (capabilitiesProbe is { StatusCode: 200 })
            {
                foreach (var requiredCapability in new[]
                         {
                             "RemoteDeploymentOrchestration",
                             "PlatformCertification"
                         })
                {
                    if (!capabilitiesProbe.Body.Contains(requiredCapability, StringComparison.OrdinalIgnoreCase))
                    {
                        errors.Add($"Runtime capabilities response is missing required capability '{requiredCapability}'.");
                    }
                }
            }
        }

        stopwatch.Stop();
        return errors.Count == 0
            ? ValidationStageResult.Pass(StageName, stopwatch.Elapsed, warnings)
            : ValidationStageResult.Fail(StageName, stopwatch.Elapsed, errors, warnings);
    }

    private static bool IsRuntimeAgentPackage(DeploymentValidationContext context)
        => string.Equals(context.PackageContents?.Manifest.Service, "runtime-agent", StringComparison.OrdinalIgnoreCase);

    private static bool IsRequiredProbe(DeploymentValidationContext context, string endpoint)
    {
        var bundle = context.ResolveDeploymentBundle();
        return bundle?.Health.AdditionalProbes.Any(probe =>
            probe.Required
            && probe.Path.Equals(endpoint, StringComparison.OrdinalIgnoreCase)) == true;
    }
}

public sealed class RollbackValidationStage : IDeploymentValidationStage
{
    public string StageName => "Rollback";

    public ValidationStageDescriptor Descriptor { get; } = new(
        ValidationCategory.Package,
        ValidationExecutionScope.Publishing,
        IsOptional: true);

    public Task<ValidationStageResult> ValidateAsync(
        DeploymentValidationContext context,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var errors = new List<string>();
        var contents = context.PackageContents;
        if (contents is null)
        {
            return Task.FromResult(ValidationStageResult.Fail(StageName, stopwatch.Elapsed, ["Package contents are unavailable."]));
        }

        if (string.IsNullOrWhiteSpace(contents.Manifest.Version))
        {
            errors.Add("Manifest version is required for rollback support.");
        }

        if (string.IsNullOrWhiteSpace(contents.Manifest.Service))
        {
            errors.Add("Manifest service identity is required for rollback support.");
        }

        if (string.IsNullOrWhiteSpace(contents.Metadata.PackageVersion))
        {
            errors.Add("Metadata package version is required for rollback support.");
        }

        stopwatch.Stop();
        return Task.FromResult(errors.Count == 0
            ? ValidationStageResult.Pass(StageName, stopwatch.Elapsed, recommendations: ["Package contains version and service metadata required for rollback."])
            : ValidationStageResult.Fail(StageName, stopwatch.Elapsed, errors));
    }
}

public sealed class EnvironmentNeutralValidationStage : IDeploymentValidationStage
{
    public string StageName => "Environment Neutral";

    public ValidationStageDescriptor Descriptor { get; } = new(
        ValidationCategory.Build,
        ValidationExecutionScope.Publishing);

    public Task<ValidationStageResult> ValidateAsync(
        DeploymentValidationContext context,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var errors = new List<string>(RuntimeConfigurationValidationStage.ScanArtifacts(context));

        if (context.PackageContents is { } contents)
        {
            var allowWindowsHostPaths = ProfileNeutralityPolicy.AllowsWindowsHostPaths(contents, context.ValidationConfigRoot);

            errors.AddRange(PackageArtifactScanner.ScanText(
                JsonSerializer.Serialize(contents.Metadata),
                DeployPackageLayout.MetadataFile));
            errors.AddRange(PackageArtifactScanner.ScanText(
                JsonSerializer.Serialize(contents.Manifest),
                DeployPackageLayout.ManifestFile));
            errors.AddRange(PackageArtifactScanner.ScanText(
                contents.ConfigurationTemplateContent,
                DeployPackageLayout.ConfigTemplateFile));
            errors.AddRange(PackageArtifactScanner.ScanText(
                contents.EnvTemplateContent,
                DeployPackageLayout.EnvTemplateFile,
                EnvironmentNeutralScanProfile.PackageArtifact,
                allowWindowsHostPaths));
            errors.AddRange(EnvTemplateValidationRules.Validate(
                contents.EnvTemplateContent,
                DeployPackageLayout.EnvTemplateFile));
            errors.AddRange(PackageArtifactScanner.ScanText(
                contents.ComposeContent,
                DeployPackageLayout.ComposeFile,
                EnvironmentNeutralScanProfile.ComposeFile));
        }

        stopwatch.Stop();
        return Task.FromResult(errors.Count == 0
            ? ValidationStageResult.Pass(StageName, stopwatch.Elapsed)
            : ValidationStageResult.Fail(StageName, stopwatch.Elapsed, errors));
    }
}
