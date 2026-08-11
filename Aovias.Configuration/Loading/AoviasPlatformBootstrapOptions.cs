namespace Aovias.Configuration.Loading;

/// <summary>
/// Resolved AOVIAS platform bootstrap settings used before profile configuration is loaded.
/// </summary>
public sealed record AoviasPlatformBootstrapOptions(
    string ConfigRoot,
    string ActiveProfile,
    string ProfilesDirectory,
    string SecretsDirectory)
{
    /// <summary>
    /// Gets the absolute profiles directory path.
    /// </summary>
    public string ProfilesPath => Path.Combine(ConfigRoot, ProfilesDirectory);

    /// <summary>
    /// Gets the absolute active profile directory path.
    /// </summary>
    public string ActiveProfilePath => Path.Combine(ProfilesPath, ActiveProfile);

    /// <summary>
    /// Gets the absolute secrets directory path for the active profile.
    /// </summary>
    public string SecretsPath => Path.Combine(ConfigRoot, SecretsDirectory, ActiveProfile);
}
