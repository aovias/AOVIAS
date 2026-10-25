using Aovias.Deployment.Contracts.Building;
using Aovias.Publishing.Contracts;
using Aovias.Publishing.Core.Generation;
using Aovias.Publishing.Core.Reporting;
using Xunit;

namespace Aovias.Publishing.Core.Tests;

/// <summary>
/// Permanent regression tests (D12) for the publishing engine core: deterministic
/// generation, environment comparison and quality-gate evaluation.
/// </summary>
public sealed class RegressionTests
{
    private static string ConfigRoot => Path.GetFullPath(Path.Combine(
        AppContext.BaseDirectory, "..", "..", "..", "..", "..", "..", "AOVIAS.Infrastructure", "aovias-config"));

    [Fact]
    public void EnvironmentComparison_server_profiles_only_differ_in_expected_values()
    {
        var report = EnvironmentComparer.Compare(ConfigRoot, ["demo", "uat", "production"]);

        Assert.True(report.IsConsistent, string.Join("; ", report.Violations.Select(v => v.Key)));

        var network = report.Variables.Single(variable => variable.Key == "AOVIAS_DOCKER_NETWORK");
        Assert.False(network.Differs);

        var hostConfig = report.Variables.Single(variable => variable.Key == "AOVIAS_HOST_CONFIG_ROOT");
        Assert.True(hostConfig.ExpectedToDiffer);
    }

    [Fact]
    public async Task ComposeGenerator_is_deterministic_and_drops_version_field()
    {
        var generator = new ComposeGenerator();

        var first = await generator.GenerateAsync(BuildComposeContext(), CancellationToken.None);
        var second = await generator.GenerateAsync(BuildComposeContext(), CancellationToken.None);

        Assert.Equal(first, second);
        Assert.DoesNotContain("version:", first, StringComparison.Ordinal);
        Assert.Contains("${AOVIAS_DOCKER_NETWORK}", first, StringComparison.Ordinal);
    }

    [Fact]
    public void QualityGates_pass_when_all_stages_and_verification_succeed()
    {
        var context = BuildGateContext(composePassed: true);
        var gates = QualityGateEvaluator.Evaluate(context);

        Assert.True(gates.AllPassed);
        Assert.Equal(QualityGateStatus.Passed, GateNamed(gates, "Compose").Status);
        Assert.Equal(QualityGateStatus.Passed, GateNamed(gates, "Package").Status);
        Assert.Equal(QualityGateStatus.Passed, GateNamed(gates, "Checksums").Status);
        Assert.Equal(QualityGateStatus.Skipped, GateNamed(gates, "API").Status);
    }

    [Fact]
    public void QualityGates_block_when_a_stage_fails()
    {
        var context = BuildGateContext(composePassed: false);
        var gates = QualityGateEvaluator.Evaluate(context);

        Assert.False(gates.AllPassed);
        Assert.Equal(QualityGateStatus.Failed, GateNamed(gates, "Compose").Status);
    }

    private static QualityGate GateNamed(QualityGateReport report, string name)
        => report.Gates.Single(gate => gate.Name == name);

    private static PublishingContext BuildGateContext(bool composePassed)
    {
        var context = new PublishingContext
        {
            Request = new PublishingRequest { ProjectPath = ".", ServiceId = "runtime-agent", Version = "1.0.0" }
        };

        context.StageTimings.Add(new PublishingStageTiming("Build", TimeSpan.FromMilliseconds(5), true));
        context.StageTimings.Add(new PublishingStageTiming("Build Docker Image", TimeSpan.FromMilliseconds(5), true));
        context.PackageVerification = DeployPackageVerificationResult.Success(["manifest.json", "checksums.json"], 6, hasCertificate: true);

        var composeStage = composePassed
            ? ValidationStageResult.Pass("Docker Compose", TimeSpan.Zero)
            : ValidationStageResult.Fail("Docker Compose", TimeSpan.Zero, ["compose config failed"]);

        context.ReadinessReport = DeploymentReadinessReport.Ready(
            [
                ValidationStageResult.Pass("Package", TimeSpan.Zero),
                composeStage,
                ValidationStageResult.Pass("Environment Neutral", TimeSpan.Zero),
                ValidationStageResult.Pass("Runtime", TimeSpan.Zero),
                ValidationStageResult.Pass("Health", TimeSpan.Zero),
                ValidationStageResult.Pass("Bootstrap Manifest", TimeSpan.Zero)
            ],
            TimeSpan.Zero);

        return context;
    }

    private static PublishingContext BuildComposeContext() => new()
    {
        Request = new PublishingRequest
        {
            ProjectPath = "AIOrchestratorService.csproj",
            ServiceId = "AIOrchestratorService",
            Version = "1.0.0"
        },
        Discovery = new ProjectDiscoveryResult
        {
            ProjectType = PublishingProjectType.AspNetCore,
            ProjectPath = "AIOrchestratorService.csproj",
            ProjectRoot = "AIOrchestratorService",
            ServiceId = "AIOrchestratorService",
            ServiceName = "AI Orchestrator",
            ProviderName = "DotNet",
            ContainerPort = 5088,
            HealthEndpoint = "/health",
            EntryPoint = "AIOrchestratorService.dll"
        },
        Manifest = new DeployManifest
        {
            Service = "AIOrchestratorService",
            Version = "1.0.0",
            Type = "AspNetCore",
            Container = "aiorchestratorservice",
            Health = "/health",
            EntryPoint = "AIOrchestratorService.dll",
            Ports = [5088],
            Dependencies = [],
            Configuration = DeployPackageLayout.ConfigTemplateFile,
            ImageName = "aiorchestratorservice",
            ImageTag = "1.0.0"
        },
        DeploymentContracts = ServiceContractBuilder.Build(new ServiceContractBuildRequest
        {
            ServiceId = "AIOrchestratorService",
            DisplayName = "AI Orchestrator",
            Version = "1.0.0",
            ImageReference = "aiorchestratorservice:1.0.0",
            ContainerName = "aiorchestratorservice",
            EntryPoint = "AIOrchestratorService.dll",
            HealthEndpoint = "/health",
            Ports = [5088]
        }),
        DeploymentMetadata = new DeploymentMetadata
        {
            ServiceId = "ai",
            Ports = new DeploymentPortMetadata { ContainerPort = 5088 },
            Health = new DeploymentHealthMetadata(),
            Dependencies =
            [
                new DeploymentDependencyMetadata { Service = "gateway" },
                new DeploymentDependencyMetadata { Service = "compliance-service" }
            ]
        }
    };
}
