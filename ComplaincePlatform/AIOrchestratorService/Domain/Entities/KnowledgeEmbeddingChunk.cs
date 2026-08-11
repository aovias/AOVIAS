namespace AIOrchestratorService.Domain.Entities;

/// <summary>
/// SQL-backed embedding chunk for hybrid knowledge retrieval (Phase 5.1).
/// Vector stored as JSON float array for portability without Azure AI Search.
/// </summary>
public sealed class KnowledgeEmbeddingChunk
{
    public Guid Id { get; set; }
    public Guid HospitalId { get; set; }
    public Guid? FrameworkId { get; set; }
    public string SourceType { get; set; } = string.Empty;
    public string SourceId { get; set; } = string.Empty;
    public string? DocumentId { get; set; }
    public string? Version { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string EmbeddingJson { get; set; } = "[]";
    public string EmbeddingModel { get; set; } = "hash-v1";
    public int Dimension { get; set; }
    public DateTime IndexedAtUtc { get; set; }
    public DateTime? SourceUpdatedAtUtc { get; set; }
}
