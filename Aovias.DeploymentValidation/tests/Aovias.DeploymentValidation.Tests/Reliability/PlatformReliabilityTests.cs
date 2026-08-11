using Aovias.Deployment.Contracts.Smoke;
using Aovias.Release.Contracts.RemoteDeployment;
using Xunit;

namespace Aovias.DeploymentValidation.Tests.Reliability;

public sealed class PlatformReliabilityTests
{
    [Fact]
    public void ServiceSmokeTestCatalog_declares_required_tests_for_gateway()
    {
        var suite = ServiceSmokeTestCatalog.GetSuite("gateway");

        Assert.Contains(suite.Tests, test => test.Id == "health" && test.Required);
    }

    [Fact]
    public void PlatformCertificationBuilder_marks_certified_when_all_gates_pass()
    {
        var manifest = new PlatformDeploymentManifest
        {
            Profile = "demo",
            Version = "1.0.0",
            Services = []
        };

        var report = PlatformCertificationBuilder.Build(
            "dep-1",
            manifest,
            new InfrastructureReadinessSnapshot
            {
                Infrastructure = "PASS",
                Contracts = "PASS",
                Containers = "PASS",
                Dependencies = "PASS",
                Health = "PASS",
                Runtime = "Healthy"
            },
            new SmokeTestSuiteResult
            {
                Passed = true,
                PassedCount = 1,
                FailedCount = 0,
                Results =
                [
                    new SmokeTestExecutionResult
                    {
                        ServiceId = "gateway",
                        TestId = "health",
                        TestName = "health",
                        Passed = true
                    }
                ]
            },
            []);

        Assert.Equal(CertificationGateStatus.Pass, report.Overall);
        Assert.True(report.IsCertified);
    }
}
