using System.IO.Compression;
using Aovias.DeploymentValidation.Models;
using Aovias.Publishing.Contracts;
using Microsoft.Extensions.Logging;

namespace Aovias.DeploymentValidation.Services;

/// <summary>
/// Maintains a single reusable validation workspace for sequential DRV.
/// </summary>
public sealed class ValidationWorkspaceManager
{
    private readonly IValidationCommandRunner commandRunner;
    private readonly ILogger<TemporaryDeploymentWorkspace> logger;
    private readonly SemaphoreSlim gate = new(1, 1);
    private bool configSeeded;
    private string? workspaceRoot;
    private string? configRoot;
    private string? networkName;

    public ValidationWorkspaceManager(
        IValidationCommandRunner commandRunner,
        ILogger<TemporaryDeploymentWorkspace> logger)
    {
        this.commandRunner = commandRunner;
        this.logger = logger;
    }

    public async Task<TemporaryDeploymentWorkspace> PrepareAsync(
        DeploymentValidationContext context,
        CancellationToken cancellationToken)
    {
        await gate.WaitAsync(cancellationToken);
        try
        {
            // This manager is a DI singleton holding one workspace shared across every project
            // in a "Publish Solution" run (class summary: "single reusable validation workspace
            // for sequential DRV"). context.WorkingDirectory is each project's own per-project,
            // ephemeral publishing session folder (PublishingWorkspaceManager.CreateSession),
            // torn down once that one project's own publish finishes. Seeding the shared
            // workspaceRoot from it means every project after the first references a directory
            // nested inside a session that no longer exists — the shared root must be anchored
            // to the stable OS temp directory instead, independent of any single project's
            // session lifecycle.
            workspaceRoot ??= Path.Combine(Path.GetTempPath(), "aovias-drv-validation-workspace");
            Directory.CreateDirectory(workspaceRoot);

            configRoot ??= Path.Combine(workspaceRoot, "config");
            if (!configSeeded)
            {
                await SeedConfigOnceAsync(configRoot, context, cancellationToken);
                configSeeded = true;
            }

            var packagePath = Path.Combine(workspaceRoot, "package");
            if (Directory.Exists(packagePath))
            {
                Directory.Delete(packagePath, recursive: true);
            }

            Directory.CreateDirectory(packagePath);
            await StagePackageArtifactsAsync(context, packagePath, cancellationToken);
            ValidationComposePortPatcher.PatchComposeFile(Path.Combine(packagePath, DeployPackageLayout.ComposeFile));

            var envPath = Path.Combine(workspaceRoot, ".env");
            networkName ??= "aovias-drv-" + Guid.NewGuid().ToString("N")[..12];
            var envTemplatePath = Path.Combine(packagePath, DeployPackageLayout.EnvTemplateFile);
            var envContent = File.Exists(envTemplatePath)
                ? await File.ReadAllTextAsync(envTemplatePath, cancellationToken)
                : string.Empty;
            envContent = TemporaryDeploymentWorkspace.BuildRuntimeEnvironmentFile(
                envContent,
                configRoot,
                workspaceRoot,
                context,
                networkName);
            await File.WriteAllTextAsync(envPath, envContent, cancellationToken);

            Directory.CreateDirectory(Path.Combine(workspaceRoot, "packages"));
            Directory.CreateDirectory(Path.Combine(workspaceRoot, "logs"));
            Directory.CreateDirectory(Path.Combine(workspaceRoot, "deployments"));
            Directory.CreateDirectory(Path.Combine(workspaceRoot, "evidence"));

            if (networkName is not null)
            {
                await TemporaryDeploymentWorkspace.EnsureDockerNetworkAsync(
                    context,
                    commandRunner,
                    networkName,
                    cancellationToken);
            }

            return TemporaryDeploymentWorkspace.OpenPrepared(
                commandRunner,
                logger,
                workspaceRoot,
                packagePath,
                Path.Combine(packagePath, DeployPackageLayout.ComposeFile),
                envPath,
                configRoot,
                networkName ?? "aovias-drv",
                context.ServiceImageTarPath);
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task<long> ResetAfterValidationAsync(CancellationToken cancellationToken)
    {
        await gate.WaitAsync(cancellationToken);
        try
        {
            if (string.IsNullOrWhiteSpace(workspaceRoot))
            {
                return 0;
            }

            long freedBytes = 0;
            var packagePath = Path.Combine(workspaceRoot, "package");
            if (Directory.Exists(packagePath))
            {
                freedBytes += DrvResourceMonitor.GetDirectorySizeBytes(packagePath);
                Directory.Delete(packagePath, recursive: true);
            }

            var envPath = Path.Combine(workspaceRoot, ".env");
            if (File.Exists(envPath))
            {
                freedBytes += DrvResourceMonitor.GetDirectorySizeBytes(envPath);
                File.Delete(envPath);
            }

            return freedBytes;
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task DisposeSessionAsync(CancellationToken cancellationToken)
    {
        await gate.WaitAsync(cancellationToken);
        try
        {
            if (string.IsNullOrWhiteSpace(workspaceRoot))
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(networkName))
            {
                try
                {
                    var context = new DeploymentValidationContext
                    {
                        PackagePath = string.Empty,
                        DockerExecutable = "docker",
                        WorkingDirectory = workspaceRoot
                    };
                    await commandRunner.RunAsync(
                        "docker",
                        $"network rm {networkName}",
                        workspaceRoot,
                        60,
                        context,
                        cancellationToken);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to remove shared validation network.");
                }
            }

            if (Directory.Exists(workspaceRoot))
            {
                Directory.Delete(workspaceRoot, recursive: true);
            }

            workspaceRoot = null;
            configRoot = null;
            configSeeded = false;
            networkName = null;
        }
        finally
        {
            gate.Release();
        }
    }

    private static async Task StagePackageArtifactsAsync(
        DeploymentValidationContext context,
        string packagePath,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(context.StagingDirectory) && Directory.Exists(context.StagingDirectory))
        {
            foreach (var file in Directory.EnumerateFiles(context.StagingDirectory))
            {
                var name = Path.GetFileName(file);
                if (string.Equals(name, DeployPackageLayout.ServiceImageTar, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                File.Copy(file, Path.Combine(packagePath, name), overwrite: true);
            }

            return;
        }

        if (string.IsNullOrWhiteSpace(context.PackagePath) || !File.Exists(context.PackagePath))
        {
            throw new InvalidOperationException("A package path or staging directory is required for validation.");
        }

        using var archive = ZipFile.OpenRead(context.PackagePath);
        foreach (var entry in archive.Entries)
        {
            if (string.IsNullOrWhiteSpace(entry.Name)
                || string.Equals(entry.Name, DeployPackageLayout.ServiceImageTar, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            entry.ExtractToFile(Path.Combine(packagePath, entry.Name), overwrite: true);
        }

        await Task.CompletedTask;
    }

    private static async Task SeedConfigOnceAsync(
        string configRoot,
        DeploymentValidationContext context,
        CancellationToken cancellationToken)
    {
        if (Directory.Exists(configRoot))
        {
            Directory.Delete(configRoot, recursive: true);
        }

        Directory.CreateDirectory(configRoot);
        if (!string.IsNullOrWhiteSpace(context.ValidationConfigRoot)
            && Directory.Exists(context.ValidationConfigRoot))
        {
            ValidationConfigSeeder.Seed(context.ValidationConfigRoot, configRoot, context.ValidationProfile);
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
    }
}
