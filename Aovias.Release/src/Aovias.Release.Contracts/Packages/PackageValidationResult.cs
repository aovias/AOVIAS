namespace Aovias.Release.Contracts.Packages;

/// <summary>
/// Result of validating an AOVIAS package.
/// </summary>
public sealed class PackageValidationResult
{
    public required bool IsValid { get; init; }

    public required IReadOnlyList<string> Errors { get; init; }

    public required IReadOnlyList<string> Warnings { get; init; }

    public PackageContents? Contents { get; init; }
}
