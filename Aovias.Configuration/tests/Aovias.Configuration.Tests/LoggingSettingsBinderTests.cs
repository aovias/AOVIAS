using Aovias.Configuration.Loading;
using Aovias.Configuration.Settings;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Aovias.Configuration.Tests;

public sealed class LoggingSettingsBinderTests
{
    [Fact]
    public void BindAndPrepare_uses_demo_logRoot_schema()
    {
        using var tempRoot = new TempDirectory();
        var logRoot = Path.Combine(tempRoot.Path, "demo-logs");
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Logging:logRoot"] = logRoot,
            ["Logging:minimumLevel"] = "Information",
            ["Logging:retentionDays"] = "30"
        });

        var resolved = LoggingSettingsBinder.BindAndPrepare(
            configuration,
            CreateBootstrap(tempRoot.Path, "demo"));

        Assert.Equal(logRoot, resolved.LogDirectory);
        Assert.Equal("runtime-agent-", resolved.FileNamePrefix);
        Assert.Equal(30, resolved.RetainedFileCountLimit);
        Assert.Equal("Information", resolved.MinimumLevel);
        Assert.True(Directory.Exists(logRoot));
    }

    [Fact]
    public void BindAndPrepare_supports_legacy_logDirectory_schema()
    {
        using var tempRoot = new TempDirectory();
        var logDirectory = Path.Combine(tempRoot.Path, "legacy-logs");
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Logging:logDirectory"] = logDirectory,
            ["Logging:fileNamePrefix"] = "runtime-agent-",
            ["Logging:retainedFileCountLimit"] = "14",
            ["Logging:minimumLevel"] = "Warning"
        });

        var resolved = LoggingSettingsBinder.BindAndPrepare(configuration);

        Assert.Equal(logDirectory, resolved.LogDirectory);
        Assert.Equal("runtime-agent-", resolved.FileNamePrefix);
        Assert.Equal(14, resolved.RetainedFileCountLimit);
        Assert.Equal("Warning", resolved.MinimumLevel);
        Assert.True(Directory.Exists(logDirectory));
    }

    [Fact]
    public void BindAndPrepare_throws_when_logRoot_is_missing()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Logging:minimumLevel"] = "Information"
        });

        var exception = Assert.Throws<AoviasConfigurationBootstrapException>(() =>
            LoggingSettingsBinder.BindAndPrepare(
                configuration,
                CreateBootstrap(Path.GetTempPath(), "demo")));

        Assert.Contains("Logging configuration is invalid.", exception.Message, StringComparison.Ordinal);
        Assert.Contains("Expected setting:", exception.Message, StringComparison.Ordinal);
        Assert.Contains("logRoot", exception.Message, StringComparison.Ordinal);
        Assert.Contains("Resolved value:", exception.Message, StringComparison.Ordinal);
        Assert.Contains("<empty>", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void BindAndPrepare_throws_when_logRoot_is_empty()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Logging:logRoot"] = "   "
        });

        var exception = Assert.Throws<AoviasConfigurationBootstrapException>(() =>
            LoggingSettingsBinder.BindAndPrepare(configuration));

        Assert.Contains("<empty>", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void BindAndPrepare_throws_when_logRoot_is_invalid()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Logging:logRoot"] = "|invalid-path"
        });

        var exception = Assert.Throws<AoviasConfigurationBootstrapException>(() =>
            LoggingSettingsBinder.BindAndPrepare(configuration));

        Assert.Contains("Logging configuration is invalid.", exception.Message, StringComparison.Ordinal);
        Assert.Contains("|invalid-path", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void BindAndPrepare_creates_missing_log_directory()
    {
        using var tempRoot = new TempDirectory();
        var logRoot = Path.Combine(tempRoot.Path, "nested", "runtime-agent");
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Logging:logRoot"] = logRoot
        });

        Assert.False(Directory.Exists(logRoot));

        var resolved = LoggingSettingsBinder.BindAndPrepare(configuration);

        Assert.True(Directory.Exists(resolved.LogDirectory));
    }

    [Fact]
    public void BindAndPrepare_loads_repository_demo_profile_schema()
    {
        var configRoot = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..", "..",
            "AOVIAS.Infrastructure",
            "aovias-config"));
        var demoSettingsPath = Path.Combine(configRoot, "profiles", "demo", "settings.logging.json");
        if (!File.Exists(demoSettingsPath))
        {
            return;
        }

        var bootstrap = CreateBootstrap(configRoot, "demo");
        using var tempRoot = new TempDirectory();
        var configuration = new ConfigurationBuilder()
            .AddAoviasProfileConfiguration(bootstrap)
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Logging:logRoot"] = Path.Combine(tempRoot.Path, "demo-runtime")
            })
            .Build();

        var resolved = LoggingSettingsBinder.BindAndPrepare(configuration, bootstrap);

        Assert.Equal(Path.Combine(tempRoot.Path, "demo-runtime"), resolved.LogDirectory);
        Assert.Equal(30, resolved.RetainedFileCountLimit);
        Assert.True(Directory.Exists(resolved.LogDirectory));
    }

    private static IConfiguration BuildConfiguration(IDictionary<string, string?> values)
        => new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();

    private static AoviasPlatformBootstrapOptions CreateBootstrap(string configRoot, string profile)
        => new(
            configRoot,
            profile,
            AoviasPlatformBootstrapResolver.DefaultProfilesDirectory,
            AoviasPlatformBootstrapResolver.DefaultSecretsDirectory);

    private sealed class TempDirectory : IDisposable
    {
        public TempDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "aovias-logging-tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
    }
}
