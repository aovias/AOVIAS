using Aovias.Deployment.Contracts.Smoke;

namespace Aovias.Release.Contracts.RemoteDeployment;

/// <summary>
/// Builds platform certification reports from deployment validation evidence.
/// </summary>
public static class PlatformCertificationBuilder
{
    public static PlatformCertificationReport Build(
        string deploymentId,
        PlatformDeploymentManifest manifest,
        InfrastructureReadinessSnapshot infrastructure,
        SmokeTestSuiteResult smokeTests,
        IReadOnlyList<string> additionalFindings)
    {
        var infraPass = IsPass(infrastructure.Infrastructure) && IsPass(infrastructure.Contracts);
        var deployPass = IsPass(infrastructure.Containers) && IsPass(infrastructure.Dependencies);
        var smokePass = smokeTests.Passed;
        var apiPass = smokeTests.Results.Any(result =>
            result.TestId is "health" or "index" && result.Passed);

        var findings = additionalFindings.ToList();
        if (!smokePass)
        {
            findings.AddRange(smokeTests.Results
                .Where(result => !result.Passed)
                .Select(result => $"{result.ServiceId}/{result.TestId}: {result.Message}"));
        }

        var overallPass = infraPass && deployPass && smokePass && apiPass
            && IsPass(infrastructure.Dependencies)
            && IsPass(infrastructure.Contracts)
            && deployPass;

        var evidence = PlatformCertificationChecklistBuilder.FromInfrastructureAndSmoke(infrastructure, smokeTests);

        return new PlatformCertificationReport
        {
            DeploymentId = deploymentId,
            PlatformVersion = manifest.Version,
            RuntimeVersion = manifest.Profile,
            GeneratedUtc = DateTimeOffset.UtcNow,
            Infrastructure = ToStatus(infraPass),
            Deployment = ToStatus(deployPass),
            SmokeTests = ToStatus(smokePass),
            ApiReachability = ToStatus(apiPass),
            DatabaseConnectivity = ToStatus(IsPass(infrastructure.Dependencies)),
            CertificateValidity = ToStatus(IsPass(infrastructure.Contracts)),
            SecretResolution = ToStatus(IsPass(infrastructure.Contracts)),
            DependencyGraph = ToStatus(deployPass),
            Overall = overallPass ? CertificationGateStatus.Pass : CertificationGateStatus.Fail,
            Findings = findings,
            SmokeTestSummary = new SmokeTestSuiteSummary
            {
                Passed = smokeTests.PassedCount,
                Failed = smokeTests.FailedCount,
                Total = smokeTests.Results.Count,
                FailedTests = smokeTests.Results
                    .Where(result => !result.Passed)
                    .Select(result => $"{result.ServiceId}/{result.TestId}")
                    .ToList()
            },
            Checklist = PlatformCertificationChecklistBuilder.Build(evidence, null)
        };
    }

    private static bool IsPass(string value)
        => string.Equals(value, "PASS", StringComparison.OrdinalIgnoreCase)
           || string.Equals(value, "Healthy", StringComparison.OrdinalIgnoreCase);

    private static CertificationGateStatus ToStatus(bool passed)
        => passed ? CertificationGateStatus.Pass : CertificationGateStatus.Fail;
}
