using ComplianceService.Domain.Copilot;

namespace ComplianceService.Application.Copilot.Models;

/// <summary>
/// Output produced by <see cref="Contracts.IDraftContentGenerator"/>.
/// Contains all generated sections plus the Source Intelligence metadata.
/// </summary>
public sealed class DraftContent
{
    /// <summary>Human-readable draft title.</summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>Executive summary (1–3 sentences) synthesizing the draft's purpose.</summary>
    public string Summary { get; init; } = string.Empty;

    /// <summary>Full markdown content assembled from all sections.</summary>
    public string FullContent { get; init; } = string.Empty;

    /// <summary>Individual sections for structured display in the viewer.</summary>
    public IReadOnlyList<DraftSection> Sections { get; init; } = [];

    /// <summary>Source Intelligence — serialized to MetadataJson on the entity.</summary>
    public DraftSourceIntelligence SourceIntelligence { get; init; } = new();

    /// <summary>Confidence score [0.0–1.0] assigned by the generator.</summary>
    public decimal Confidence { get; init; } = 0.85m;

    /// <summary>Name of the generator that produced this content.</summary>
    public string GeneratorName { get; init; } = string.Empty;
}
