using System.Security.Cryptography;
using System.Text;

namespace AIOrchestratorService.Application.AI.Knowledge;

/// <summary>
/// Deterministic local embedding (hashing trick) so hybrid RAG works without external embedding APIs.
/// Production can swap for Azure OpenAI embeddings behind the same interface.
/// </summary>
public sealed class HashingEmbeddingService : IEmbeddingService
{
    public string ModelName => "hash-v1";
    public int Dimension => 256;

    public Task<float[]> EmbedAsync(string text, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(Embed(text));
    }

    public async Task<IReadOnlyList<float[]>> EmbedBatchAsync(IEnumerable<string> texts, CancellationToken cancellationToken = default)
    {
        var list = new List<float[]>();
        foreach (var text in texts)
        {
            list.Add(await EmbedAsync(text, cancellationToken));
        }

        return list;
    }

    /// <summary>Cosine similarity for normalized (or raw) embedding vectors.</summary>
    public static float CosineSimilarity(float[] a, float[] b)
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

    private float[] Embed(string text)
    {
        var vector = new float[Dimension];
        if (string.IsNullOrWhiteSpace(text))
        {
            return vector;
        }

        var tokens = Tokenize(text);
        foreach (var token in tokens)
        {
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(token));
            var index = BitConverter.ToUInt16(hash, 0) % Dimension;
            var sign = (hash[2] & 1) == 0 ? 1f : -1f;
            vector[index] += sign;
        }

        // L2 normalize
        var norm = MathF.Sqrt(vector.Sum(v => v * v));
        if (norm > 0)
        {
            for (var i = 0; i < vector.Length; i++)
            {
                vector[i] /= norm;
            }
        }

        return vector;
    }

    private static IEnumerable<string> Tokenize(string text)
    {
        var sb = new StringBuilder();
        foreach (var ch in text.ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(ch))
            {
                sb.Append(ch);
            }
            else if (sb.Length > 0)
            {
                yield return sb.ToString();
                sb.Clear();
            }
        }

        if (sb.Length > 0)
        {
            yield return sb.ToString();
        }
    }
}
