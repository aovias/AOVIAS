using System.Text.Json;
using Aovias.DeploymentValidation.Rules;
using Aovias.DeploymentValidation.Services;
using Aovias.Publishing.Contracts;
using Xunit;

namespace Aovias.DeploymentValidation.Tests;

public sealed class BootstrapValidationTests
{
    [Fact]
    public void PatchValidationRuntimePaths_rewrites_host_specific_deployment_settings()
    {
        var configRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var profileDirectory = Path.Combine(configRoot, "profiles", "local");
        Directory.CreateDirectory(profileDirectory);

        File.WriteAllText(
            Path.Combine(profileDirectory, "settings.deployment-host.json"),
            """
              {
                "hostConfigRoot": "C:/Aovias/runtime/config",
                "hostLogsRoot": "C:/Aovias/runtime/logs"
              }
              """);
        File.WriteAllText(
            Path.Combine(profileDirectory, "settings.deployment-storage.json"),
            """
              {
                "root": "V:/DevelopmentRepo/V1.0/aovias-config/storage/local/deployments"
              }
              """);

        try
        {
            TemporaryDeploymentWorkspace.PatchValidationRuntimePaths(configRoot, "local");

            using var hostDocument = JsonDocument.Parse(
                File.ReadAllText(Path.Combine(profileDirectory, "settings.deployment-host.json")));
            Assert.Equal(
                "/opt/aovias/runtime/config",
                hostDocument.RootElement.GetProperty("hostConfigRoot").GetString());
            Assert.Equal(
                "/opt/aovias/runtime/logs",
                hostDocument.RootElement.GetProperty("hostLogsRoot").GetString());

            using var storageDocument = JsonDocument.Parse(
                File.ReadAllText(Path.Combine(profileDirectory, "settings.deployment-storage.json")));
            Assert.Equal(
                "/opt/aovias/runtime/deployments",
                storageDocument.RootElement.GetProperty("root").GetString());
        }
        finally
        {
            Directory.Delete(configRoot, recursive: true);
        }
    }

    [Fact]
    public void BuildBootstrapProbeDockerArguments_mounts_validation_workspace_paths()
    {
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var workspace = TemporaryDeploymentWorkspace.OpenPrepared(
            commandRunner: null!,
            logger: null!,
            rootPath: root,
            extractedPath: Path.Combine(root, "package"),
            composePath: Path.Combine(root, "docker-compose.yml"),
            envPath: Path.Combine(root, ".env"),
            configRoot: Path.Combine(root, "config"),
            networkName: "aovias-drv-test",
            serviceImageTarPath: null);

        var arguments = TemporaryDeploymentWorkspace.BuildBootstrapProbeDockerArguments(
            "aovias/runtime-agent:1.0.2",
            "Aovias.Runtime.Agent.Api.dll",
            "local",
            workspace);

        Assert.Contains("-v \"", arguments, StringComparison.Ordinal);
        Assert.Contains(":/opt/aovias/runtime/config:ro", arguments, StringComparison.Ordinal);
        Assert.Contains(":/opt/aovias/runtime/logs:rw", arguments, StringComparison.Ordinal);
        Assert.Contains(":/opt/aovias/runtime/packages:rw", arguments, StringComparison.Ordinal);
        Assert.Contains(":/opt/aovias/runtime/deployments:rw", arguments, StringComparison.Ordinal);
        Assert.Contains(Path.Combine(root, "logs"), arguments, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void PublishingReady_treats_deferred_runtime_stages_as_satisfied()
    {
        var report = DeploymentReadinessReport.PublishingReady(
        [
            ValidationStageResult.Pass("Package", TimeSpan.Zero),
            ValidationStageResult.Pass("Bootstrap Manifest", TimeSpan.Zero),
            ValidationStageResult.Defer("Runtime Verification", TimeSpan.Zero, "Runs during deployment.")
        ],
        TimeSpan.FromSeconds(1));

        Assert.True(report.IsReady);
        Assert.Equal("PUBLISHING READY", report.StatusText);
        Assert.Equal(100, report.OverallScore);
    }

    [Fact]
    public void Bootstrap_manifest_compose_scan_allows_container_healthcheck_loopback()
    {
        var compose = """
                      services:
                        runtime-agent:
                          healthcheck:
                            test: ["CMD", "wget", "-qO-", "http://127.0.0.1:7080/health"]
                      """;

        var violations = PackageArtifactScanner.ScanText(
            compose,
            DeployPackageLayout.ComposeFile,
            EnvironmentNeutralScanProfile.ComposeFile);

        Assert.Empty(violations);
    }

    [Fact]
    public void RuntimeBootstrapReportParser_detects_logging_and_startup_failures()
    {
        var output = """
                     Runtime Bootstrap Report

                     Logging
                     FAILED

                     Startup
                     FAILED
                     """;

        var result = RuntimeBootstrapReportParser.Parse(output);

        Assert.True(result.ReportFound);
        Assert.False(result.StartupSucceeded);
        Assert.Equal(["Logging", "Startup"], result.FailedModules);
    }
}
