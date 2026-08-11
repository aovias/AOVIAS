namespace AIOrchestratorService.Application.AI.Knowledge;

public sealed class VectorSearchHit
{
    public Guid ChunkId { get; set; }
    public string SourceType { get; set; } = string.Empty;
    public string SourceId { get; set; } = string.Empty;
    public string? DocumentId { get; set; }
    public string? Version { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Excerpt { get; set; } = string.Empty;
    public decimal Score { get; set; }
}

public sealed class VectorUpsertRequest
{
    public Guid HospitalId { get; set; }
    public Guid? FrameworkId { get; set; }
    public string SourceType { get; set; } = string.Empty;
    public string SourceId { get; set; } = string.Empty;
    public string? DocumentId { get; set; }
    public string? Version { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime? SourceUpdatedAtUtc { get; set; }
}

public interface IVectorIndex
{
    Task UpsertAsync(VectorUpsertRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<VectorSearchHit>> SearchAsync(
        Guid hospitalId,
        string query,
        Guid? frameworkId,
        int topK,
        CancellationToken cancellationToken = default);
    Task<int> KeywordSearchCountAsync(Guid hospitalId, string query, CancellationToken cancellationToken = default);
}
