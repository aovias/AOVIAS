using Aovias.Publishing.Contracts;
using Aovias.Publishing.Contracts.Verification;
using Aovias.Publishing.Core.Discovery;
using Aovias.Publishing.Core.Generation;
using Aovias.Publishing.Core.Verification;
using Xunit;

namespace Aovias.Publishing.Core.Tests.Verification;

/// <summary>
/// Sprint M3.4 regression tests for deploy-time lifecycle readiness. Builds a real
/// <see cref="Aovias.Deployment.Contracts.ServiceDeploymentBundle"/> the same way
/// <c>DeploymentPlanGenerationVerifierTests</c> (Sprint M3.3) does, then feeds it through the
/// existing M2.4 <see cref="Aovias.Deployment.Contracts.Planning.DeploymentPlanBuilder"/> to get a
/// real <see cref="Aovias.Deployment.Contracts.Planning.DeploymentPlan"/> â€” no hand-authored plan
/// fixtures. The wiring checks run against the actual checked-out repository, so they are genuine
/// regression tests: if the reconciliation hosted service or the health/certification
/// provisioners were ever unregistered, these tests would fail for real.
/// </summary>
public sealed class DeploymentLifecycleReadinessVerifierTests
{
    private static readonly string PlatformRoot = Path.GetFullPath(Path.Combine(
        AppContext.BaseDirectory,
        "..", "..", "..", "..", "..", ".."));

    [Fact]
    public void BuildRepresentativePlan_produces_a_structurally_complete_plan_for_a_real_bundle()
    {
        var bundle = BuildRealBundle(
            "ComplaincePlatform/AIOrchestratorService/deployment.metadata.json",
            "ComplaincePlatform/AIOrchestratorService/AIOrchestratorService.csproj");

        var plan = DeploymentLifecycleReadinessVerifier.BuildRepresentativePlan(bundle, out var failureReason);

        Assert.NotNull(plan);
        Assert.Null(failureReason);
        Assert.Equal(8, plan!.ExecutionPhases.Count);
    }

    [Fact]
    public void VerifyHealthGateReadiness_passes_when_plan_declares_health_gates()
    {
        var bundle = BuildRealBundle(
            "ComplaincePlatform/AIOrchestratorService/deployment.metadata.json",
            "ComplaincePlatform/AIOrchestratorService/AIOrchestratorService.csproj");
        var plan = DeploymentLifecycleReadinessVerifier.BuildRepresentativePlan(bundle, out var failureReason);

        var status = DeploymentLifecycleReadinessVerifier.VerifyHealthGateReadiness(plan, failureReason);

        Assert.True(status.IsPassed);
        Assert.Equal(VerificationEvidenceTier.RuntimeVerification, status.EvidenceTier);
    }

    [Fact]
    public void VerifyDeploymentCertificationReadiness_passes_when_plan_declares_certification_gates()
    {
        var bundle = BuildRealBundle(
            "ComplaincePlatform/AIOrchestratorService/deployment.metadata.json",
            "ComplaincePlatform/AIOrchestratorService/AIOrchestratorService.csproj");
        var plan = DeploymentLifecycleReadinessVerifier.BuildRepresentativePlan(bundle, out var failureReason);

        var status = DeploymentLifecycleReadinessVerifier.VerifyDeploymentCertificationReadiness(plan, failureReason);

        Assert.True(status.IsPassed);
    }

    [Fact]
    public void VerifyRollbackReadiness_passes_when_plan_declares_rollback_checkpoints()
    {
        var bundle = BuildRealBundle(
            "ComplaincePlatform/AIOrchestratorService/deployment.metadata.json",
            "ComplaincePlatform/AIOrchestratorService/AIOrchestratorService.csproj");
        var plan = DeploymentLifecycleReadinessVerifier.BuildRepresentativePlan(bundle, out var failureReason);

        var status = DeploymentLifecycleReadinessVerifier.VerifyRollbackReadiness(plan, failureReason);

        Assert.True(status.IsPassed);
    }

    [Fact]
    public void VerifyHealthGateReadiness_fails_with_root_cause_when_plan_could_not_be_built()
    {
        var status = DeploymentLifecycleReadinessVerifier.VerifyHealthGateReadiness(plan: null, buildFailureReason: "boom");

        Assert.Equal(VerificationOutcome.Failed, status.Outcome);
        Assert.Equal("boom", status.RootCause);
    }

    [Fact]
    public void VerifyReconciliationInitializationWiring_passes_against_the_real_repository()
    {
        var status = DeploymentLifecycleReadinessVerifier.VerifyReconciliationInitializationWiring(PlatformRoot);

        Assert.True(status.IsPassed);
        Assert.Equal(VerificationEvidenceTier.StaticVerification, status.EvidenceTier);
    }

    [Fact]
    public void VerifyRuntimeStartupWiring_passes_against_the_real_repository()
    {
        var status = DeploymentLifecycleReadinessVerifier.VerifyRuntimeStartupWiring(PlatformRoot);

        Assert.True(status.IsPassed);
    }

    [Fact]
    public void VerifyReconciliationInitializationWiring_fails_with_root_cause_when_di_file_is_missing()
    {
        var emptyRoot = Path.Combine(Path.GetTempPath(), "m3.4-empty-root", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(emptyRoot);

        var status = DeploymentLifecycleReadinessVerifier.VerifyReconciliationInitializationWiring(emptyRoot);

        Assert.Equal(VerificationOutcome.Failed, status.Outcome);
        Assert.NotNull(status.RootCause);
    }

    [Fact]
    public void VerifyRuntimeStartupWiring_fails_with_root_cause_when_program_and_provider_are_missing()
    {
        var emptyRoot = Path.Combine(Path.GetTempPath(), "m3.4-empty-root", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(emptyRoot);

        var status = DeploymentLifecycleReadinessVerifier.VerifyRuntimeStartupWiring(emptyRoot);

        Assert.Equal(VerificationOutcome.Failed, status.Outcome);
        Assert.NotNull(status.RootCause);
    }

    private static Aovias.Deployment.Contracts.ServiceDeploymentBundle BuildRealBundle(
        string metadataRelativePath,
        string projectRelativePath)
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
        return ServiceContractGenerator.Generate(context);
    }
}
