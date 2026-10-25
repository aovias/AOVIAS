using System.Reflection;
using Aovias.Publishing.Contracts;
using Aovias.Publishing.Core.Common;
using Aovias.Publishing.Core.Pipeline;
using Aovias.Publishing.Core.Reporting;
using Aovias.Publishing.Core.Workspace;
using Xunit;

namespace Aovias.Publishing.Core.Tests;

/// <summary>
/// Regression guards for Sprint M1.1 technical debt cleanup.
/// </summary>
public sealed class PublishingTechnicalDebtRegressionTests
{
    [Fact]
    public void PublishingPipeline_has_no_legacy_execution_path()
    {
        var legacyMethod = typeof(PublishingPipeline).GetMethod(
            "ExecuteLegacyAsync",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.Null(legacyMethod);
    }

    [Fact]
    public void PublishingPipelineOrchestrator_has_no_skip_stage_hook()
    {
        var skipMethod = typeof(PublishingPipelineOrchestrator).GetMethod(
            "ShouldSkipStage",
            BindingFlags.Static | BindingFlags.NonPublic);
        Assert.Null(skipMethod);
    }

    [Fact]
    public void WorkspaceManager_has_no_stage_artifact_reuse_api()
    {
        var workspaceType = typeof(PublishingWorkspaceManager);
        Assert.Null(workspaceType.GetMethod("TryLoadStageArtifacts"));
        Assert.Null(workspaceType.GetMethod("SaveStageArtifacts"));
        Assert.Null(workspaceType.GetMethod("GetStageArtifactPath"));
    }

    [Fact]
    public void ExecutionOptions_has_no_artifact_reuse_flag()
    {
        Assert.Null(typeof(PublishingExecutionOptions).GetProperty("ReuseSuccessfulArtifacts"));
        Assert.Null(typeof(PublishingExecutionOptions).GetProperty("ReplaySessionId"));
    }

    [Fact]
    public void CanonicalOrder_is_unchanged_after_debt_cleanup()
    {
        var order = PublishingStageExecutor.CanonicalOrder;
        Assert.Equal(13, order.Count);
        Assert.Equal(PublishingPipelineStageId.Discovery, order[0]);
        Assert.Equal(PublishingPipelineStageId.Preflight, order[1]);
        Assert.Equal(PublishingPipelineStageId.DeploymentCertification, order[^1]);
    }

    [Fact]
    public void SolutionMembershipResolver_reads_demo_membership_from_deployable_project()
    {
        var demoProject = new DeployableProject
        {
            Name = "Gateway",
            ServiceId = "gateway",
            ProjectPath = "gateway.csproj",
            DiscoveryPath = "gateway.csproj",
            Type = PublishingProjectType.AspNetCore,
            OutputPath = ".",
            Version = "1.0.0",
            IncludeInDemoBundle = true
        };

        var fullProject = new DeployableProject
        {
            Name = "Knowledge Base",
            ServiceId = "knowledge-base",
            ProjectPath = "knowledge-base.csproj",
            DiscoveryPath = "knowledge-base.csproj",
            Type = PublishingProjectType.AspNetCore,
            OutputPath = ".",
            Version = "1.0.0",
            IncludeInDemoBundle = false
        };

        Assert.True(SolutionMembershipResolver.IsDemoBundleMember(demoProject));
        Assert.False(SolutionMembershipResolver.IsDemoBundleMember(fullProject));
    }

    [Fact]
    public void PublishingVersionPolicy_centralizes_defaults()
    {
        Assert.Equal("1.0.0", PublishingVersionPolicy.DefaultPackageVersion);
        Assert.Equal(PublishingVersionPolicy.DefaultPackageVersion, PublishingVersionPolicy.EngineVersion);
        Assert.Equal(PublishingVersionPolicy.EngineVersion, PublishingStageExecutor.EngineVersion);
    }

    [Fact]
    public void PublishingReportEmission_writes_sibling_report()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), "aovias-m11-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(outputDirectory);
        try
        {
            var packagePath = Path.Combine(outputDirectory, "runtime-agent.aovdeploy");
            File.WriteAllText(packagePath, "package");

            var context = new PublishingContext
            {
                Request = new PublishingRequest
                {
                    ProjectPath = "runtime-agent.csproj",
                    ServiceId = "runtime-agent",
                    Version = PublishingVersionPolicy.DefaultPackageVersion,
                    Profile = "demo"
                },
                PackagePath = packagePath,
                Manifest = new DeployManifest
                {
                    Service = "runtime-agent",
                    Version = PublishingVersionPolicy.DefaultPackageVersion,
                    Type = "AspNetCore",
                    Container = "runtime-agent",
                    Health = "/health",
                    EntryPoint = "Aovias.Runtime.Agent.Api.dll",
                    Ports = [8080],
                    Dependencies = [],
                    Configuration = DeployPackageLayout.ConfigTemplateFile,
                    ImageName = "aovias/runtime-agent",
                    ImageTag = PublishingVersionPolicy.DefaultPackageVersion
                },
                Metadata = new DeployPackageMetadata
                {
                    Publisher = "test",
                    PublishingEngineVersion = PublishingVersionPolicy.EngineVersion,
                    PackageVersion = PublishingVersionPolicy.DefaultPackageVersion,
                    PackageType = DeployPackageLayout.Extension,
                    CreatedUtc = DateTimeOffset.UtcNow
                }
            };

            PublishingReportEmission.EmitSibling(context);

            var siblingPath = Path.Combine(outputDirectory, $"runtime-agent.{DeployPackageLayout.PublishingReportFile}");
            Assert.True(File.Exists(siblingPath));
            Assert.Contains(siblingPath, context.GeneratedFiles);
        }
        finally
        {
            if (Directory.Exists(outputDirectory))
            {
                Directory.Delete(outputDirectory, recursive: true);
            }
        }
    }
}
