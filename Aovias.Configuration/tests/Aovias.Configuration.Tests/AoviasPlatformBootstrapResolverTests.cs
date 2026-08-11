using Aovias.Configuration.Loading;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Aovias.Configuration.Tests;

public sealed class AoviasPlatformBootstrapResolverTests : IDisposable
{
    private readonly List<(string Name, string? Value)> environmentRestore = [];

    [Fact]
    public void Resolve_prefers_environment_variables_over_bootstrap_configuration()
    {
        using var temp = CreateConfigRoot(profileName: "bootstrap-profile");
        SetEnvironment(AoviasPlatformBootstrapResolver.ConfigRootEnvironmentVariable, temp.ConfigRoot);
        SetEnvironment(AoviasPlatformBootstrapResolver.ActiveProfileEnvironmentVariable, "env-profile");
        SetEnvironment(AoviasPlatformBootstrapResolver.ProfilesDirectoryEnvironmentVariable, "custom-profiles");
        SetEnvironment(AoviasPlatformBootstrapResolver.SecretsDirectoryEnvironmentVariable, "custom-secrets");

        var bootstrap = BuildBootstrapConfiguration(new Dictionary<string, string?>
        {
            ["Aovias:ConfigRoot"] = @"V:\wrong\path",
            ["Aovias:ActiveProfile"] = "wrong-profile",
            ["Aovias:ProfilesDirectory"] = "wrong-profiles",
            ["Aovias:SecretsDirectory"] = "wrong-secrets"
        });

        var resolved = AoviasPlatformBootstrapResolver.Resolve(bootstrap);

        Assert.Equal(AoviasPlatformBootstrapResolver.NormalizeConfigRoot(temp.ConfigRoot), resolved.ConfigRoot);
        Assert.Equal("env-profile", resolved.ActiveProfile);
        Assert.Equal("custom-profiles", resolved.ProfilesDirectory);
        Assert.Equal("custom-secrets", resolved.SecretsDirectory);
    }

    [Fact]
    public void Resolve_uses_bootstrap_configuration_when_environment_variables_are_missing()
    {
        using var temp = CreateConfigRoot(profileName: "demo");

        var bootstrap = BuildBootstrapConfiguration(new Dictionary<string, string?>
        {
            ["Aovias:ConfigRoot"] = temp.ConfigRoot,
            ["Aovias:ActiveProfile"] = "demo",
            ["Aovias:ProfilesDirectory"] = "profiles",
            ["Aovias:SecretsDirectory"] = "secrets"
        });

        var resolved = AoviasPlatformBootstrapResolver.Resolve(bootstrap);

        Assert.Equal(AoviasPlatformBootstrapResolver.NormalizeConfigRoot(temp.ConfigRoot), resolved.ConfigRoot);
        Assert.Equal("demo", resolved.ActiveProfile);
        Assert.Equal("profiles", resolved.ProfilesDirectory);
        Assert.Equal("secrets", resolved.SecretsDirectory);
    }

    [Fact]
    public void Resolve_uses_safe_defaults_when_bootstrap_values_are_missing()
    {
        var bootstrap = BuildBootstrapConfiguration([]);

        var resolved = AoviasPlatformBootstrapResolver.Resolve(bootstrap);

        Assert.Equal(AoviasPlatformBootstrapResolver.DefaultConfigRoot, resolved.ConfigRoot);
        Assert.Equal(AoviasPlatformBootstrapResolver.DefaultActiveProfile, resolved.ActiveProfile);
        Assert.Equal(AoviasPlatformBootstrapResolver.DefaultProfilesDirectory, resolved.ProfilesDirectory);
        Assert.Equal(AoviasPlatformBootstrapResolver.DefaultSecretsDirectory, resolved.SecretsDirectory);
    }

    [Fact]
    public void Validate_throws_actionable_exception_when_active_profile_is_missing()
    {
        using var temp = CreateConfigRoot(profileName: "demo");
        var bootstrap = new AoviasPlatformBootstrapOptions(
            temp.ConfigRoot,
            "missing-profile",
            "profiles",
            "secrets");

        var exception = Assert.Throws<AoviasConfigurationBootstrapException>(() => AoviasPlatformBootstrapResolver.Validate(bootstrap));

        Assert.Contains("missing-profile", exception.Message, StringComparison.Ordinal);
        Assert.Contains(Path.Combine(temp.ConfigRoot, "profiles", "missing-profile"), exception.Message, StringComparison.Ordinal);
        Assert.Equal(bootstrap, exception.Bootstrap);
    }

    [Fact]
    public void NormalizeConfigRoot_preserves_linux_absolute_paths()
    {
        var normalized = AoviasPlatformBootstrapResolver.NormalizeConfigRoot("/opt/aovias/runtime/config");

        Assert.Equal("/opt/aovias/runtime/config", normalized);
    }

    [Fact]
    public void Resolve_normalizes_linux_config_root_from_environment_variable()
    {
        SetEnvironment(AoviasPlatformBootstrapResolver.ConfigRootEnvironmentVariable, "/opt/aovias/runtime/config/");

        var resolved = AoviasPlatformBootstrapResolver.Resolve(BuildBootstrapConfiguration([]));

        Assert.Equal("/opt/aovias/runtime/config", resolved.ConfigRoot);
    }

    public void Dispose()
    {
        foreach (var (name, value) in environmentRestore)
        {
            Environment.SetEnvironmentVariable(name, value);
        }
    }

    private void SetEnvironment(string name, string value)
    {
        environmentRestore.Add((name, Environment.GetEnvironmentVariable(name)));
        Environment.SetEnvironmentVariable(name, value);
    }

    private static IConfiguration BuildBootstrapConfiguration(IEnumerable<KeyValuePair<string, string?>> values)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(values.ToDictionary(static pair => pair.Key, static pair => pair.Value))
            .Build();
    }

    private static TempConfigRoot CreateConfigRoot(string profileName)
    {
        var root = Path.Combine(Path.GetTempPath(), "aovias-config-tests", Guid.NewGuid().ToString("N"));
        var profileDirectory = Path.Combine(root, "profiles", profileName);
        Directory.CreateDirectory(profileDirectory);
        File.WriteAllText(Path.Combine(profileDirectory, "settings.api.json"), """{ "enabled": true }""");
        return new TempConfigRoot(root);
    }

    private sealed class TempConfigRoot : IDisposable
    {
        public TempConfigRoot(string configRoot) => ConfigRoot = configRoot;

        public string ConfigRoot { get; }

        public void Dispose()
        {
            if (Directory.Exists(ConfigRoot))
            {
                Directory.Delete(ConfigRoot, recursive: true);
            }
        }
    }
}
