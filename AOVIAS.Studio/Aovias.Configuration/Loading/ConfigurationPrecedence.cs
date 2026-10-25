namespace Aovias.Configuration.Loading;

/// <summary>
/// Defines configuration source precedence from lowest to highest priority.
/// </summary>
public enum ConfigurationPrecedence
{
    /// <summary>
    /// Profile JSON settings files for the active runtime profile.
    /// </summary>
    ProfileSettingsFiles = 100,

    /// <summary>
    /// Profile secrets file for the active runtime profile.
    /// </summary>
    ProfileSecretsFile = 200,

    /// <summary>
    /// Application appsettings.json.
    /// </summary>
    AppSettings = 300,

    /// <summary>
    /// Environment-specific appsettings file.
    /// </summary>
    AppSettingsEnvironment = 400,

    /// <summary>
    /// Environment variables with AOVIAS_ prefix.
    /// </summary>
    EnvironmentVariables = 500,

    /// <summary>
    /// Reserved for future database provider.
    /// </summary>
    DatabaseProvider = 600,

    /// <summary>
    /// Reserved for future Azure Key Vault provider.
    /// </summary>
    AzureKeyVault = 700,

    /// <summary>
    /// Reserved for future HashiCorp Vault provider.
    /// </summary>
    HashiCorpVault = 800
}
