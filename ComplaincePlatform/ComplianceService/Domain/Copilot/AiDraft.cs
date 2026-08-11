using ComplianceService.Domain.Copilot.Enums;

namespace ComplianceService.Domain.Copilot;

/// <summary>
/// Persists a Copilot-generated compliance draft (e.g., CAPA draft, evidence narrative, policy text).
/// Supports versioning, Source Intelligence traceability, and future approval workflows (Sprint 13D).
/// </summary>
public class AiDraft : AggregateRoot
{
    // ── Identity ────────────────────────────────────────────────────────────
    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }

    /// <summary>
    /// Groups all versions of the same logical draft.
    /// On first creation GroupId == Id. Regenerations share GroupId with the original.
    /// </summary>
    public Guid GroupId { get; private set; }

    // ── Classification ───────────────────────────────────────────────────────
    public DraftType DraftType { get; private set; } = DraftType.CapaDraft;
    public DraftSourceType SourceType { get; private set; } = DraftSourceType.Manual;
    public Guid? SourceEntityId { get; private set; }
    public Guid? RecommendationId { get; private set; }
    public string Persona { get; private set; } = string.Empty;

    // ── Content ──────────────────────────────────────────────────────────────
    public string Title { get; private set; } = string.Empty;
    public string Summary { get; private set; } = string.Empty;
    public string Content { get; private set; } = string.Empty;

    // ── Generation Provenance ────────────────────────────────────────────────
    public string GeneratedBy { get; private set; } = string.Empty;
    public decimal Confidence { get; private set; } = 1.0m;

    /// <summary>
    /// Serialized <see cref="DraftSourceIntelligence"/> JSON.
    /// Contains recommendation IDs, KPI values, deficiency refs, CAPA refs,
    /// obligation refs, analytics metrics, and rationale objects used during generation.
    /// </summary>
    public string? MetadataJson { get; private set; }

    public DateTime GeneratedAtUtc { get; private set; }

    // ── Lifecycle ────────────────────────────────────────────────────────────
    public DraftStatus Status { get; private set; } = DraftStatus.Draft;
    public int Version { get; private set; } = 1;
    public DateTime CreatedUtc { get; private set; }
    public DateTime UpdatedUtc { get; private set; }

    // ── EF Core constructor ──────────────────────────────────────────────────
    private AiDraft() { }

    // ── Factory – Legacy (Sprint 13A compatibility) ──────────────────────────

    /// <summary>Creates a minimal draft; generation engine will use CreateFromEngine instead.</summary>
    public static AiDraft Create(
        Guid organizationId,
        string draftType,
        string persona,
        string title,
        string content,
        string? sourceContext = null)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title is required.", nameof(title));

        if (!Enum.TryParse<DraftType>(draftType, ignoreCase: true, out var typedDraft))
            typedDraft = DraftType.CapaDraft;

        var id  = Guid.NewGuid();
        var now = DateTime.UtcNow;
        return new AiDraft
        {
            Id              = id,
            GroupId         = id,
            OrganizationId  = organizationId,
            DraftType       = typedDraft,
            SourceType      = DraftSourceType.Manual,
            Persona         = persona,
            Title           = title,
            Summary         = string.Empty,
            Content         = content,
            GeneratedBy     = "Manual",
            Confidence      = 1.0m,
            MetadataJson    = sourceContext,
            Status          = DraftStatus.Draft,
            Version         = 1,
            GeneratedAtUtc  = now,
            CreatedUtc      = now,
            UpdatedUtc      = now,
        };
    }

    // ── Factory – Engine (Sprint 13C) ────────────────────────────────────────

    /// <summary>
    /// Creates a fully-attributed draft from the generation engine.
    /// </summary>
    public static AiDraft CreateFromEngine(
        Guid organizationId,
        Guid groupId,
        int version,
        DraftType draftType,
        DraftSourceType sourceType,
        string persona,
        string title,
        string summary,
        string content,
        string generatedBy,
        decimal confidence,
        string? metadataJson,
        Guid? sourceEntityId = null,
        Guid? recommendationId = null)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title is required.", nameof(title));

        var now = DateTime.UtcNow;
        return new AiDraft
        {
            Id              = Guid.NewGuid(),
            GroupId         = groupId,
            OrganizationId  = organizationId,
            DraftType       = draftType,
            SourceType      = sourceType,
            SourceEntityId  = sourceEntityId,
            RecommendationId = recommendationId,
            Persona         = persona,
            Title           = title,
            Summary         = summary,
            Content         = content,
            GeneratedBy     = generatedBy,
            Confidence      = confidence,
            MetadataJson    = metadataJson,
            Status          = DraftStatus.Draft,
            Version         = version,
            GeneratedAtUtc  = now,
            CreatedUtc      = now,
            UpdatedUtc      = now,
        };
    }

    // ── Mutations ────────────────────────────────────────────────────────────

    /// <summary>Updates content; used for human edits. Increments version counter.</summary>
    public void UpdateContent(string newContent)
    {
        if (Status == DraftStatus.Archived)
            throw new InvalidOperationException("Archived drafts cannot be updated.");

        Content    = newContent;
        Version   += 1;
        UpdatedUtc = DateTime.UtcNow;
    }

    /// <summary>Updates the summary text without creating a new version.</summary>
    public void UpdateSummary(string newSummary)
    {
        if (Status == DraftStatus.Archived)
            throw new InvalidOperationException("Archived drafts cannot be updated.");

        Summary    = newSummary;
        UpdatedUtc = DateTime.UtcNow;
    }

    /// <summary>Marks draft as reviewed by a human.</summary>
    public void MarkReviewed()
    {
        if (Status != DraftStatus.Draft)
            throw new InvalidOperationException(
                $"Only Draft-status items can be marked reviewed. Current: {Status}.");

        Status     = DraftStatus.Reviewed;
        UpdatedUtc = DateTime.UtcNow;
    }

    /// <summary>Approves the draft (Sprint 13D approval card target).</summary>
    public void Approve()
    {
        if (Status != DraftStatus.Reviewed)
            throw new InvalidOperationException(
                $"Only Reviewed drafts can be approved. Current: {Status}.");

        Status     = DraftStatus.Approved;
        UpdatedUtc = DateTime.UtcNow;
    }

    /// <summary>Archives the draft, preventing further edits.</summary>
    public void Archive()
    {
        if (Status == DraftStatus.Archived)
            return;

        Status     = DraftStatus.Archived;
        UpdatedUtc = DateTime.UtcNow;
    }
}
