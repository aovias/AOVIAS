using System.Text.Json;
using AIOrchestratorService.Domain.Entities;
using AIOrchestratorService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AIOrchestratorService.Application.AI.Knowledge;

public sealed class SqlVectorIndex : IVectorIndex
{
    private readonly AIOrchestratorDbContext _db;
    private readonly IEmbeddingService _embeddings;

    public SqlVectorIndex(AIOrchestratorDbContext db, IEmbeddingService embeddings)
    {
        _db = db;
        _embeddings = embeddings;
    }

    public async Task UpsertAsync(VectorUpsertRequest request, CancellationToken cancellationToken = default)
    {
        var embedding = await _embeddings.EmbedAsync(request.Content, cancellationToken);
        var existing = await _db.KnowledgeEmbeddingChunks
            .FirstOrDefaultAsync(
                x => x.HospitalId == request.HospitalId
                     && x.SourceType == request.SourceType
                     && x.SourceId == request.SourceId,
                cancellationToken);

        if (existing == null)
        {
            existing = new KnowledgeEmbeddingChunk
            {
                Id = Guid.NewGuid(),
                HospitalId = request.HospitalId,
                SourceType = request.SourceType,
                SourceId = request.SourceId
            };
            _db.KnowledgeEmbeddingChunks.Add(existing);
        }

        existing.FrameworkId = request.FrameworkId;
        existing.DocumentId = request.DocumentId;
        existing.Version = request.Version;
        existing.Title = request.Title;
        existing.Content = request.Content.Length > 4000 ? request.Content[..4000] : request.Content;
        existing.EmbeddingJson = JsonSerializer.Serialize(embedding);
        existing.EmbeddingModel = _embeddings.ModelName;
        existing.Dimension = embedding.Length;
        existing.IndexedAtUtc = DateTime.UtcNow;
        existing.SourceUpdatedAtUtc = request.SourceUpdatedAtUtc;

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<VectorSearchHit>> SearchAsync(
        Guid hospitalId,
        string query,
        Guid? frameworkId,
        int topK,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query) || hospitalId == Guid.Empty)
        {
            return Array.Empty<VectorSearchHit>();
        }

        var queryVector = await _embeddings.EmbedAsync(query, cancellationToken);
        var q = _db.KnowledgeEmbeddingChunks.AsNoTracking().Where(x => x.HospitalId == hospitalId);
        if (frameworkId.HasValue)
        {
            q = q.Where(x => x.FrameworkId == null || x.FrameworkId == frameworkId);
        }

        // Keyword prefilter then cosine rank (keeps SQL load bounded).
        var keyword = query.Trim();
        var candidates = await q
            .Where(x => x.Content.Contains(keyword) || x.Title.Contains(keyword) || keyword.Length < 3)
            .OrderByDescending(x => x.IndexedAtUtc)
            .Take(Math.Max(topK * 8, 40))
            .ToListAsync(cancellationToken);

        if (candidates.Count == 0)
        {
            candidates = await q
                .OrderByDescending(x => x.IndexedAtUtc)
                .Take(Math.Max(topK * 4, 20))
                .ToListAsync(cancellationToken);
        }

        return candidates
            .Select(c =>
            {
                var vec = Deserialize(c.EmbeddingJson);
                var cosine = Cosine(queryVector, vec);
                var keywordBoost = (!string.IsNullOrWhiteSpace(c.Content) &&
                                    c.Content.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                    ? 0.15f
                    : 0f;
                return new VectorSearchHit
                {
                    ChunkId = c.Id,
                    SourceType = c.SourceType,
                    SourceId = c.SourceId,
                    DocumentId = c.DocumentId,
                    Version = c.Version,
                    Title = c.Title,
                    Excerpt = c.Content.Length > 280 ? c.Content[..280] : c.Content,
                    Score = (decimal)Math.Clamp(cosine + keywordBoost, 0f, 1f)
                };
            })
            .OrderByDescending(x => x.Score)
            .Take(Math.Max(1, topK))
            .ToList();
    }

    public Task<int> KeywordSearchCountAsync(Guid hospitalId, string query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return Task.FromResult(0);
        }

        return _db.KnowledgeEmbeddingChunks.AsNoTracking()
            .CountAsync(
                x => x.HospitalId == hospitalId
                     && (x.Content.Contains(query) || x.Title.Contains(query)),
                cancellationToken);
    }

    private static float[] Deserialize(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<float[]>(json) ?? Array.Empty<float>();
        }
        catch
        {
            return Array.Empty<float>();
        }
    }

    private static float Cosine(float[] a, float[] b)
    {
        if (a.Length == 0 || b.Length == 0 || a.Length != b.Length)
        {
            return 0f;
        }

        float dot = 0, na = 0, nb = 0;
        for (var i = 0; i < a.Length; i++)
        {
            dot += a[i] * b[i];
            na += a[i] * a[i];
            nb += b[i] * b[i];
        }

        var denom = MathF.Sqrt(na) * MathF.Sqrt(nb);
        return denom <= 0 ? 0f : dot / denom;
    }
}
