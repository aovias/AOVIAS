using ComplianceService.Domain;

namespace ComplianceService.Application.Journey;

/// <summary>
/// Cacheable static catalog slice used by journey workspace reads.
/// </summary>
public sealed class FrameworkCatalogSnapshot
{
    public required Guid FrameworkId { get; init; }

    public required IReadOnlyList<ComplianceChapter> Chapters { get; init; }

    public required IReadOnlyList<ComplianceStandard> Standards { get; init; }

    public required IReadOnlyList<ObjectiveElement> ObjectiveElements { get; init; }
}

