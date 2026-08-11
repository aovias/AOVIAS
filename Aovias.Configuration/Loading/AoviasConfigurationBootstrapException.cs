namespace Aovias.Configuration.Loading;

/// <summary>
/// Raised when AOVIAS platform bootstrap validation fails before profile configuration is loaded.
/// </summary>
public sealed class AoviasConfigurationBootstrapException : InvalidOperationException
{
    public AoviasConfigurationBootstrapException(
        string message,
        AoviasPlatformBootstrapOptions? bootstrap = null,
        string? missingPath = null)
        : base(message)
    {
        Bootstrap = bootstrap;
        MissingPath = missingPath;
    }

    /// <summary>
    /// Gets the bootstrap values that were resolved before validation failed.
    /// </summary>
    public AoviasPlatformBootstrapOptions? Bootstrap { get; }

    /// <summary>
    /// Gets the path that failed validation, when applicable.
    /// </summary>
    public string? MissingPath { get; }
}
