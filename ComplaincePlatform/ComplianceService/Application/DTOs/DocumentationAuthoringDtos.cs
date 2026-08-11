using ComplianceService.Domain.Enums;
using System.Text.Json;

namespace ComplianceService.Application.DTOs;

public record AuthoringBlockDto
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");
    public string Type { get; init; } = "paragraph";
    public JsonElement Data { get; init; }
}

public record AuthoringSectionDto
{
    public Guid Id { get; init; }
    public string SectionKey { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public int SortOrder { get; init; }
    public bool IsRequired { get; init; }
    public bool IsCollapsed { get; init; }
    public IReadOnlyList<AuthoringBlockDto> Blocks { get; init; } = Array.Empty<AuthoringBlockDto>();
    public DateTime LastEditedAt { get; init; }
}

public record DocumentHealthScoreDto
{
    public int CompletenessPercent { get; init; }
    public int ReadabilityScore { get; init; }
    public int ApprovalReadinessPercent { get; init; }
    public int OverallHealthScore { get; init; }
    public IReadOnlyList<string> MissingSections { get; init; } = Array.Empty<string>();
    public int MappedObjectiveElements { get; init; }
    public int UnresolvedComments { get; init; }
    public int LinkedEvidenceCount { get; init; }
    public int LinkedCapaCount { get; init; }
    public int LinkedFindingCount { get; init; }
}

public record ComplianceMappingSuggestionDto
{
    public string Framework { get; init; } = string.Empty;
    public string Chapter { get; init; } = string.Empty;
    public string Standard { get; init; } = string.Empty;
    public string ObjectiveElement { get; init; } = string.Empty;
    public Guid? ObjectiveElementId { get; init; }
    public double Confidence { get; init; }
    public string Reason { get; init; } = string.Empty;
}

public record AuthoringCommentDto
{
    public Guid Id { get; init; }
    public Guid? SectionId { get; init; }
    public string? BlockId { get; init; }
    public DocumentAuthoringCommentType CommentType { get; init; }
    public string Content { get; init; } = string.Empty;
    public string? AuthorName { get; init; }
    public string? MentionedUserName { get; init; }
    public bool IsResolved { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record AuthoringCheckpointDto
{
    public Guid Id { get; init; }
    public string? Label { get; init; }
    public string? CreatedByName { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record CrossReferenceDto
{
    public Guid Id { get; init; }
    public Guid? SectionId { get; init; }
    public string? BlockId { get; init; }
    public DocumentCrossReferenceType ReferenceType { get; init; }
    public Guid TargetId { get; init; }
    public string TargetLabel { get; init; } = string.Empty;
    public string? TargetRoute { get; init; }
}

public record AuthoringSessionDto
{
    public Guid DocumentId { get; init; }
    public Guid VersionId { get; init; }
    public string DocumentNumber { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public ControlledDocumentType DocumentType { get; init; }
    public ControlledDocumentStatus Status { get; init; }
    public string AuthoringSchemaKey { get; init; } = "sop";
    public string? Department { get; init; }
    public string? MappedFramework { get; init; }
    public string? MappedChapter { get; init; }
    public string? MappedStandard { get; init; }
    public string? MappedObjectiveElement { get; init; }
    public Guid? MappedObjectiveElementId { get; init; }
    public IReadOnlyList<AuthoringSectionDto> Sections { get; init; } = Array.Empty<AuthoringSectionDto>();
    public DocumentHealthScoreDto Health { get; init; } = new();
    public IReadOnlyList<ComplianceMappingSuggestionDto> MappingSuggestions { get; init; } = Array.Empty<ComplianceMappingSuggestionDto>();
    public IReadOnlyList<AuthoringCommentDto> Comments { get; init; } = Array.Empty<AuthoringCommentDto>();
    public IReadOnlyList<CrossReferenceDto> CrossReferences { get; init; } = Array.Empty<CrossReferenceDto>();
    public IReadOnlyList<AuthoringCheckpointDto> Checkpoints { get; init; } = Array.Empty<AuthoringCheckpointDto>();
    public DateTime LastSavedAt { get; init; }
}

public record CreateAuthoringRequest
{
    public string Title { get; init; } = string.Empty;
    public ControlledDocumentType DocumentType { get; init; } = ControlledDocumentType.Sop;
    public Guid? TemplateId { get; init; }
    public string? AuthoringSchemaKey { get; init; }
    public string? Department { get; init; }
}

public record PatchAuthoringRequest
{
    public string? Title { get; init; }
    public string? Department { get; init; }
    public IReadOnlyList<AuthoringSectionPatchDto>? Sections { get; init; }
    public bool CreateCheckpoint { get; init; }
    public string? CheckpointLabel { get; init; }
    public Guid? AcceptMappingObjectiveElementId { get; init; }
    public ComplianceMappingSuggestionDto? ManualMapping { get; init; }
}

public record AuthoringSectionPatchDto
{
    public Guid Id { get; init; }
    public bool? IsCollapsed { get; init; }
    public IReadOnlyList<AuthoringBlockDto>? Blocks { get; init; }
}

public record StructuredCompareRequest
{
    public Guid DocumentId { get; init; }
    public Guid? VersionId { get; init; }
}

public record StructuredCompareResultDto
{
    public Guid CurrentVersionId { get; init; }
    public Guid? PreviousVersionId { get; init; }
    public IReadOnlyList<SectionDiffDto> SectionDiffs { get; init; } = Array.Empty<SectionDiffDto>();
    public string AiSummary { get; init; } = string.Empty;
}

public record SectionDiffDto
{
    public string SectionKey { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string ChangeType { get; init; } = string.Empty;
    public IReadOnlyList<BlockDiffDto> BlockDiffs { get; init; } = Array.Empty<BlockDiffDto>();
}

public record BlockDiffDto
{
    public string BlockId { get; init; } = string.Empty;
    public string ChangeType { get; init; } = string.Empty;
    public string? BeforeText { get; init; }
    public string? AfterText { get; init; }
}

public record AuthoringAiDraftRequest
{
    public Guid DocumentId { get; init; }
    public Guid SectionId { get; init; }
    public string Action { get; init; } = "draft-section";
    public string? Instructions { get; init; }
}

public record AuthoringAiDraftResponse
{
    public IReadOnlyList<AuthoringBlockDto> SuggestedBlocks { get; init; } = Array.Empty<AuthoringBlockDto>();
    public string Explanation { get; init; } = string.Empty;
    public bool IsAdvisoryOnly { get; init; } = true;
}

public record CreateAuthoringCommentRequest
{
    public Guid DocumentId { get; init; }
    public Guid? SectionId { get; init; }
    public string? BlockId { get; init; }
    public DocumentAuthoringCommentType CommentType { get; init; } = DocumentAuthoringCommentType.Inline;
    public string Content { get; init; } = string.Empty;
    public string? MentionedUserName { get; init; }
}

public record AddCrossReferenceRequest
{
    public Guid DocumentId { get; init; }
    public Guid? SectionId { get; init; }
    public string? BlockId { get; init; }
    public DocumentCrossReferenceType ReferenceType { get; init; }
    public Guid TargetId { get; init; }
    public string TargetLabel { get; init; } = string.Empty;
    public string? TargetRoute { get; init; }
}

public record DocumentExportRequest
{
    public Guid DocumentId { get; init; }
    public DocumentExportFormat Format { get; init; } = DocumentExportFormat.Pdf;
}

public record DocumentExportResultDto
{
    public string FileName { get; init; } = string.Empty;
    public string ContentType { get; init; } = string.Empty;
    public string Base64Content { get; init; } = string.Empty;
}
