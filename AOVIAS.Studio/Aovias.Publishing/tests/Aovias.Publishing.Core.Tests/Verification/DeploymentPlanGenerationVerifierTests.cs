using Aovias.Publishing.Contracts;
using Aovias.Publishing.Contracts.Verification;
using Aovias.Publishing.Core.Discovery;
using Aovias.Publishing.Core.Generation;
using Aovias.Publishing.Core.Verification;
using Xunit;

namespace Aovias.Publishing.Core.Tests.Verification;

/// <summary>
/// Sprint M3.3 regression tests for the deployment-plan-generation smoke check. Builds a real
/// <see cref="Aovias.Deployment.Contracts.ServiceDeploymentBundle"/> the same way the publishing
/// pipeline does (via <see cref="ServiceContractGenerator"/> against a real
/// <c>deployment.metadata.json</c> fixture already used by <c>ContractDrivenPublishingTests</c>),
/// rather than hand-authoring the bundle's dozen sub-contracts.
/// </summary>
public sealed class DeploymentPlanGenerationVerifierTests
{
    private static readonly string PlatformRoot = Path.GetFullPath(Path.Combine(
        AppContext.BaseDirectory,
        "..", "..", "..", "..", "..", ".."));

    [Fact]
    public void Verify_passes_for_a_real_service_bundle_under_permissive_capabilities()
    {
        var contents = BuildPackageContentsForRealService(
            "AOVIAS.Command/ComplaincePlatform/AIOrchestratorService/deployment.metadata.json",
            "AOVIAS.Command/ComplaincePlatform/AIOrchestratorService/AIOrchestratorService.csproj");

        var status = DeploymentPlanGenerationVerifier.Verify(contents);

        Assert.Equal(VerificationOutcome.Passed, status.Outcome);
        Assert.Equal(VerificationEvidenceTier.RuntimeVerification, status.EvidenceTier);
    }

    [Fact]
    public void Verify_skips_when_package_carries_no_deployment_contracts()
    {
        var contents = new DeployPackageContents
        {
            PackagePath = "/out/legacy.aovdeploy",
            Manifest = new DeployManifest
            {
                Service = "legacy",
                Version = "1.0.0",
                Type = "AspNetCore",
                Container = "legacy",
                Health = "/health",
                EntryPoint = "legacy.dll",
                Configuration = "config.json",
                ImageName = "legacy",
                ImageTag = "1.0.0"
            },
            Health = new DeployHealthDefinition { Endpoint = "/health" },
            Metadata = new DeployPackageMetadata
            {
                Publisher = "test",
                PublishingEngineVersion = "1.0.0",
                PackageVersion = "1.0.0",
                PackageType = "Service",
                CreatedUtc = DateTimeOffset.UtcNow
            },
            Checksums = new Dictionary<string, string> { ["a"] = "b" },
            ConfigurationTemplate = new Dictionary<string, string>(),
            DeploymentContracts = null
        };

        var status = DeploymentPlanGenerationVerifier.Verify(contents);

        Assert.Equal(VerificationOutcome.Skipped, status.Outcome);
    }

    private static DeployPackageContents BuildPackageContentsForRealService(string metadataRelativePath, string projectRelativePath)
    {
        var metadataPath = Path.Combine(PlatformRoot, metadataRelativePath);
        var projectPath = Path.Combine(PlatformRoot, projectRelativePath);
        var metadata = DeploymentMetadataReader.TryLoad(metadataPath, Path.GetDirectoryName(metadataPath)!)
            ?? throw new InvalidOperationException($"Fixture missing: {metadataPath}");

        var context = new PublishingContext
        {
            Request = new PublishingRequest
            {
                ProjectPath = projectPath,
                ServiceId = metadata.ServiceId,
                Version = "1.0.0"
            },
            Discovery = new ProjectDiscoveryResult
            {
                ProjectType = PublishingProjectType.AspNetCore,
                ProjectPath = projectPath,
                ProjectRoot = Path.GetDirectoryName(projectPath)!,
                ServiceId = metadata.ServiceId,
                ServiceName = metadata.ServiceId,
                ProviderName = "DotNet",
                Confidence = 100,
                EntryPoint = $"{metadata.ServiceId}.dll"
            },
            DeploymentMetadata = metadata
        };

        context.Manifest = new DeployManifestGenerator().Generate(context);
        context.Health = new HealthDefinitionGenerator().Generate(context);
        context.DeploymentContracts = ServiceContractGenerator.Generate(context);

        return new DeployPackageContents
        {
            PackagePath = $"/out/{metadata.ServiceId}-1.0.0.aovdeploy",
            Manifest = context.Manifest,
            Health = context.Health,
            Metadata = new DeployPackageMetadata
            {
                Publisher = "test",
                PublishingEngineVersion = "1.0.0",
                PackageVersion = "1.0.0",
                PackageType = "Service",
                CreatedUtc = DateTimeOffset.UtcNow
            },
            Checksums = new Dictionary<string, string> { ["manifest.json"] = "sha256:test" },
            ConfigurationTemplate = new Dictionary<string, string>(),
            DeploymentContracts = context.DeploymentContracts
        };
    }
}
