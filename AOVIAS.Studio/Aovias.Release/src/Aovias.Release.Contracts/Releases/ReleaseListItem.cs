using Aovias.Release.Contracts.Enums;

namespace Aovias.Release.Contracts.Releases;

/// <summary>
/// Lightweight release list item.
/// </summary>
public sealed class ReleaseListItem
{
    public required string ReleaseId { get; init; }

    public required string PackageId { get; init; }

    public required string PackageVersion { get; init; }

    public required string ApplicationName { get; init; }

    public required ReleaseState State { get; init; }

    public required DateTimeOffset UpdatedAt { get; init; }
}
