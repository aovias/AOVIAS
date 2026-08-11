namespace Aovias.Configuration.Settings;

/// <summary>
/// Validated logging settings resolved for runtime bootstrap.
/// </summary>
public sealed class ResolvedLoggingSettings
{
    public required string LogDirectory { get; init; }

    public required string FileNamePrefix { get; init; }

    public required int RetainedFileCountLimit { get; init; }

    public required string MinimumLevel { get; init; }

    public string? ConfigurationFilePath { get; init; }
}
