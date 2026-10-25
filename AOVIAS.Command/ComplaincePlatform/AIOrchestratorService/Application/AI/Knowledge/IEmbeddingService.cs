namespace AIOrchestratorService.Application.AI.Knowledge;

public interface IEmbeddingService
{
    string ModelName { get; }
    int Dimension { get; }
    Task<float[]> EmbedAsync(string text, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<float[]>> EmbedBatchAsync(IEnumerable<string> texts, CancellationToken cancellationToken = default);
}
