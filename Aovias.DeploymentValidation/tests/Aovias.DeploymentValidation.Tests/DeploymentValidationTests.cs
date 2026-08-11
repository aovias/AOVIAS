using System.Diagnostics;
using Aovias.DeploymentValidation.Rules;
using Aovias.Publishing.Contracts;
using Xunit;

namespace Aovias.DeploymentValidation.Tests;

public sealed class EnvironmentNeutralRulesTests
{
    private const string DeveloperConfigRoot = "V:/DevelopmentRepo/V1.0/aovias-config";

    [Fact]
    public void FindViolations_detects_developer_paths()
    {
        var content = $$"""
                      {
                        "Aovias": {
                          "ConfigRoot": "{{DeveloperConfigRoot}}",
                          "ActiveProfile": "local"
                        }
                      }
                      """;

        var violations = EnvironmentNeutralRules.FindViolations(content, "appsettings.json", EnvironmentNeutralScanProfile.PackageArtifact);

        Assert.NotEmpty(violations);
        Assert.Contains(violations, violation => violation.Contains("DevelopmentRepo", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void FindViolations_allows_compose_healthcheck_urls()
    {
        var content = """
                      ASPNETCORE_URLS: "http://+:7080"
                      test: ["CMD", "wget", "-qO-", "http://127.0.0.1:7080/health"]
                      """;

        var violations = EnvironmentNeutralRules.FindViolations(
            content,
            DeployPackageLayout.ComposeFile,
            EnvironmentNeutralScanProfile.ComposeFile);

        Assert.Empty(violations);
    }

    [Fact]
    public void FindViolations_detects_windows_drive_paths()
    {
        var content = "ConfigRoot: V:/DevelopmentRepo/V1.0/aovias-config";

        var violations = EnvironmentNeutralRules.FindViolations(content, "config.json");

        Assert.Contains(violations, violation => violation.Contains("V:/", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void FindViolations_allows_neutral_container_bootstrap_output()
    {
        var content = """
                      Bootstrapping Runtime Agent...

                      Configuration Root:
                      /opt/aovias/runtime/config

                      Active Profile:
                      local
                      """;

        var violations = EnvironmentNeutralRules.FindViolations(
            content,
            "bootstrap container output",
            EnvironmentNeutralScanProfile.BootstrapOutput);

        Assert.Empty(violations);
    }

    [Fact]
    public void ValidationStageResult_pass_and_fail_helpers_work()
    {
        var pass = ValidationStageResult.Pass("Package", TimeSpan.FromSeconds(1));
        var fail = ValidationStageResult.Fail("Compose", TimeSpan.FromSeconds(2), ["invalid compose"]);

        Assert.True(pass.Passed);
        Assert.False(fail.Passed);
        Assert.Equal("Package", pass.StageName);
    }
}

public sealed class DeploymentReadinessReportTests
{
    [Fact]
    public void Ready_report_has_full_score()
    {
        var report = DeploymentReadinessReport.Ready(
            [ValidationStageResult.Pass("Package", TimeSpan.FromMilliseconds(100))],
            TimeSpan.FromSeconds(1));

        Assert.True(report.IsReady);
        Assert.Equal(100, report.OverallScore);
        Assert.Equal("READY FOR DEPLOYMENT", report.StatusText);
    }

    [Fact]
    public void NotReady_report_calculates_partial_score()
    {
        var report = DeploymentReadinessReport.NotReady(
            [
                ValidationStageResult.Pass("Package", TimeSpan.FromMilliseconds(100)),
                ValidationStageResult.Fail("Compose", TimeSpan.FromMilliseconds(100), ["syntax error"])
            ],
            TimeSpan.FromSeconds(2),
            ["Compose: syntax error"]);

        Assert.False(report.IsReady);
        Assert.Equal(50, report.OverallScore);
    }
}
