using KnowledgeBase.Application.Contracts;

namespace KnowledgeBase.Application.Services;

public sealed class MockAIService : IAIService
{
    public string? LastPrompt { get; private set; }

    public Task<AICompletionResult> CompleteAsync(string prompt, CancellationToken cancellationToken = default)
    {
        LastPrompt = prompt;
        var text = "MOCK: Review the injected context and follow hospital policy. [mock-response]";
        return Task.FromResult(new AICompletionResult(text, "mock-model", Math.Max(1, prompt.Length / 4)));
    }
}
