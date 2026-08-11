using Aovias.Release.Contracts.Enums;

namespace Aovias.Release.Contracts.Releases;

/// <summary>
/// Search criteria for deployment history views.
/// </summary>
public sealed class ReleaseHistorySearchCriteria
{
    public string? Runtime { get; init; }

    public ReleaseState? State { get; init; }

    public string? Operator { get; init; }

    public DateTimeOffset? From { get; init; }

    public DateTimeOffset? To { get; init; }
}
