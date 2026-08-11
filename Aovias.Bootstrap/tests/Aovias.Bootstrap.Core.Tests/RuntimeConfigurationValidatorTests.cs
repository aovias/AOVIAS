using Aovias.Bootstrap.Contracts;
using Aovias.Configuration.Settings;
using Aovias.Release.Contracts.RemoteDeployment;
using Xunit;

namespace Aovias.Bootstrap.Core.Tests;

/// <summary>
/// Bootstrap Configuration Validation &amp; Runtime Startup Diagnostics defect fix. Covers the
/// "missing configuration" and "empty configuration values" verification scenarios directly against
/// <see cref="RuntimeConfigurationValidator"/> and <see cref="RuntimeConfigurationScaffoldBuilder"/> —
/// the two new pieces this defect fix adds — independent of whether the current generator can be
/// coaxed into producing a bad value through a live orchestrator run (by design, after this fix, it
/// shouldn't be able to; that is the point).
/// </summary>
public sealed class RuntimeConfigurationValidatorTests
{
    [Fact]
    public void Validate_reports_no_failures_for_a_freshly_generated_target()
    {
        var target = BuildTarget();
        var settingsByFile = RuntimeConfigurationScaffoldBuilder.BuildRequiredProfileSettings(target);

        var failures = RuntimeConfigurationValidator.Validate(settingsByFile);

        Assert.Empty(failures);
    }

    [Fact]
    public void Validate_generates_every_file_the_demo_profile_feature_set_requires()
    {
        var settingsByFile = RuntimeConfigurationScaffoldBuilder.BuildRequiredProfileSettings(BuildTarget());

        // The full set the exploration confirmed as required for Runtime + ReleaseManagement +
        // DockerProvider + SignalR — the four features Bootstrap's own phases already exercise.
        Assert.Equal(17, settingsByFile.Count);
        Assert.Contains("settings.logging.json", settingsByFile.Keys);
        Assert.Contains("settings.runtime.json", settingsByFile.Keys);
        Assert.Contains("settings.deployment-host.json", settingsByFile.Keys);
        Assert.Contains("settings.deployment-storage.json", settingsByFile.Keys);
        Assert.Contains("settings.network.json", settingsByFile.Keys);
        Assert.Contains("settings.environment-registration.json", settingsByFile.Keys);
    }

    [Fact]
    public void Validate_reports_the_exact_real_world_defect_a_missing_logRoot()
    {
        // Reproduces the actual reported crash verbatim: "Logging configuration is invalid. The log
        // root setting is missing or empty. Expected setting: logRoot."
        var settingsByFile = new Dictionary<string, object>
        {
            ["settings.logging.json"] = new LoggingSettings { LogRoot = string.Empty, MinimumLevel = "Information", RetentionDays = 14, RetainedFileCountLimit = 14 }
        };

        var failures = RuntimeConfigurationValidator.Validate(settingsByFile);

        Assert.Single(failures);
        var failure = failures[0];
        Assert.Equal("settings.logging.json", failure.File);
        Assert.Equal(nameof(LoggingSettings.LogRoot), failure.Property);
        Assert.Equal("<empty>", failure.Actual);
        Assert.Contains("Regenerate Runtime Configuration", failure.SuggestedFix, StringComparison.Ordinal);
    }

    [Fact]
    public void Validate_reports_every_missing_property_across_every_file_not_just_the_first()
    {
        var settingsByFile = new Dictionary<string, object>
        {
            ["settings.runtime.json"] = new RuntimeSettings(), // every [Required] property left empty
            ["settings.storage.json"] = new StorageSettings()  // every [Required] property left empty
        };

        var failures = RuntimeConfigurationValidator.Validate(settingsByFile);

        Assert.True(failures.Count >= 10); // 7 on RuntimeSettings + 3 on StorageSettings
        Assert.Contains(failures, f => f.File == "settings.runtime.json" && f.Property == nameof(RuntimeSettings.Hostname));
        Assert.Contains(failures, f => f.File == "settings.storage.json" && f.Property == nameof(StorageSettings.DataRoot));
    }

    [Fact]
    public void Validate_catches_deploymentStorage_root_even_though_the_class_has_no_Required_attribute()
    {
        var settingsByFile = new Dictionary<string, object>
        {
            ["settings.deployment-storage.json"] = new DeploymentStorageSettings { Root = string.Empty }
        };

        var failures = RuntimeConfigurationValidator.Validate(settingsByFile);

        Assert.Single(failures);
        Assert.Equal(nameof(DeploymentStorageSettings.Root), failures[0].Property);
    }

    [Fact]
    public void FormatFailures_includes_file_property_expected_actual_and_fix_for_every_failure()
    {
        var failures = new List<RuntimeConfigurationValidationFailure>
        {
            new()
            {
                File = "settings.logging.json",
                Property = "LogRoot",
                Expected = "a non-empty value",
                Actual = "<empty>",
                SuggestedFix = "Regenerate Runtime Configuration — 'LogRoot' in 'settings.logging.json' must be set."
            }
        };

        var formatted = RuntimeConfigurationValidator.FormatFailures(failures);

        Assert.Contains("settings.logging.json", formatted, StringComparison.Ordinal);
        Assert.Contains("LogRoot", formatted, StringComparison.Ordinal);
        Assert.Contains("<empty>", formatted, StringComparison.Ordinal);
        Assert.Contains("Regenerate Runtime Configuration", formatted, StringComparison.Ordinal);
    }

    private static BootstrapTarget BuildTarget() => new()
    {
        Host = "vps-demo.example.test",
        Username = "deploy",
        Password = "unused-in-tests",
        ProfileName = "demo",
        EnvironmentName = "Demo VPS",
        EnvironmentType = EnvironmentType.Demo,
        RegistrationToken = "demo-vps-registration-token",
        PublicUrl = "http://vps-demo.example.test:7080",
        CommandCenterBaseUrl = "https://command-center.example.test",
        RuntimeAgentArtifactPath = "/local/path/to/agent-publish-output",
        RemoteConfigRoot = "/opt/aovias/config",
        DockerNetworkName = "aovias-net"
    };
}
