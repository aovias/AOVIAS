namespace KnowledgeBase.Application.Contracts;

public interface IAIService
{
    Task<AICompletionResult> CompleteAsync(string prompt, CancellationToken cancellationToken = default);
}

public sealed record AICompletionResult(string Text, string ModelUsed, int TokensUsed);
