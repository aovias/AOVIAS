using Aovias.Configuration.Loading;
using Aovias.Configuration.Settings;
using Microsoft.Extensions.Options;

namespace Aovias.Configuration.Profiles;

/// <summary>
/// Manages runtime profile discovery and activation.
/// </summary>
public interface IProfileManager
{
    /// <summary>
    /// Gets the active profile name.
    /// </summary>
    string ActiveProfile { get; }

    /// <summary>
    /// Gets available profile names.
    /// </summary>
    IReadOnlyList<string> AvailableProfiles { get; }

    /// <summary>
    /// Activates a profile by updating persisted bootstrap configuration.
    /// </summary>
    /// <param name="profileName">The profile name.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>A task representing the operation.</returns>
    Task ActivateProfileAsync(string profileName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates that the active profile exists and is loadable.
    /// </summary>
    bool ValidateActiveProfile();
}

/// <summary>
/// Default profile manager backed by the AOVIAS configuration root.
/// </summary>
public sealed class ProfileManager : IProfileManager
{
    private readonly IOptionsMonitor<AoviasPlatformOptions> platformOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProfileManager"/> class.
    /// </summary>
    /// <param name="platformOptions">Platform options monitor.</param>
    public ProfileManager(IOptionsMonitor<AoviasPlatformOptions> platformOptions)
    {
        this.platformOptions = platformOptions;
    }

    /// <inheritdoc />
    public string ActiveProfile => platformOptions.CurrentValue.ActiveProfile;

    /// <inheritdoc />
    public IReadOnlyList<string> AvailableProfiles =>
        ProfileConfigurationLoader.ListProfiles(
            platformOptions.CurrentValue.ConfigRoot,
            platformOptions.CurrentValue.ProfilesDirectory);

    /// <inheritdoc />
    public Task ActivateProfileAsync(string profileName, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(profileName);

        var platform = platformOptions.CurrentValue;
        var profilePath = Path.Combine(platform.ConfigRoot, platform.ProfilesDirectory, profileName);
        if (!Directory.Exists(profilePath))
        {
            throw new DirectoryNotFoundException($"Profile directory was not found: {profilePath}");
        }

        var bootstrapPath = Path.Combine(platform.ConfigRoot, "aovias.config.json");
        var bootstrap = $$"""
                          {
                            "Aovias": {
                              "ConfigRoot": "{{platform.ConfigRoot.Replace("\\", "\\\\")}}",
                              "ProfilesDirectory": "{{platform.ProfilesDirectory}}",
                              "SecretsDirectory": "{{platform.SecretsDirectory}}",
                              "ActiveProfile": "{{profileName}}"
                            }
                          }
                          """;

        File.WriteAllText(bootstrapPath, bootstrap);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public bool ValidateActiveProfile()
    {
        var platform = platformOptions.CurrentValue;
        var profilePath = Path.Combine(platform.ConfigRoot, platform.ProfilesDirectory, ActiveProfile);
        return Directory.Exists(profilePath);
    }
}
