namespace Aovias.Configuration.Layout;

/// <summary>
/// Supported <c>repository-layout.json</c> schema versions for production consumption.
/// </summary>
public static class RepositoryLayoutSchema
{
    public const string Version1_0_0 = "1.0.0";

    public static IReadOnlyList<string> SupportedSchemaVersions { get; } =
        [Version1_0_0];

    public static bool IsSupported(string? schemaVersion)
        => !string.IsNullOrWhiteSpace(schemaVersion)
           && SupportedSchemaVersions.Contains(schemaVersion, StringComparer.Ordinal);
}
