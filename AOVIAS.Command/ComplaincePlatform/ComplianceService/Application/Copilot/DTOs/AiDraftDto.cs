namespace ComplianceService.Application.Copilot.DTOs;

/// <summary>Read model returned by the Copilot drafts API.</summary>
public sealed class AiDraftDto
{
    public Guid Id { get; init; }

    /// <summary>Groups all versions of the same logical draft.</summary>
    public Guid GroupId { get; init; }

    public Guid OrganizationId { get; init; }

    // ── Classification ────────────────────────────────────────────────────────
    public string DraftType { get; init; } = string.Empty;
    public int DraftTypeEnum { get; init; }
    public string SourceType { get; init; } = string.Empty;
    public Guid? SourceEntityId { get; init; }
    public Guid? RecommendationId { get; init; }
    public string Persona { get; init; } = string.Empty;

    // ── Content ───────────────────────────────────────────────────────────────
    public string Title { get; init; } = string.Empty;
    public string Summary { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;

    // ── Generation Provenance ─────────────────────────────────────────────────
    public string GeneratedBy { get; init; } = string.Empty;
    public decimal Confidence { get; init; }

    /// <summary>Serialized Source Intelligence JSON for auditing and regeneration.</summary>
    public string? MetadataJson { get; init; }

    // ── Lifecycle ─────────────────────────────────────────────────────────────
    public string Status { get; init; } = string.Empty;
    public int Version { get; init; }
    public DateTime GeneratedAtUtc { get; init; }
    public DateTime CreatedUtc { get; init; }
    public DateTime UpdatedUtc { get; init; }
}
