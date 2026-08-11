using AIOrchestratorService.Application.AI.Classification;
using AIOrchestratorService.Application.AI.Models;

namespace AIOrchestratorService.Application.AI.Inference;

public sealed class AIModelRequest
{
    public string TenantKey { get; set; } = string.Empty;
    public string Prompt { get; set; } = string.Empty;
    public string SystemPrompt { get; set; } = string.Empty;
    public string PromptTemplateVersion { get; set; } = "1.0";
    public AIUseCase UseCase { get; set; } = AIUseCase.ExplainCompliance;
    public AIRetrievalContext Context { get; set; } = new();
    public AssistantAnswerMode AnswerMode { get; set; } = AssistantAnswerMode.GeneralKnowledge;
}

public sealed class AIModelResponse
{
    public string Hint { get; set; } = string.Empty;
    public decimal Confidence { get; set; }
    public List<string> ReasoningBullets { get; set; } = new();

    // Optional Executive Compliance Advisor structured fields. Populated when the model emits
    // the extended JSON schema; absent for legacy/unstructured providers.
    public string? ExecutiveSummary { get; set; }
    public string? ExpectedOutcome { get; set; }
    public ComplianceInsightType? InsightType { get; set; }
    public List<ComplianceFinding> Findings { get; set; } = new();
    public List<ComplianceRisk> Risks { get; set; } = new();
    public List<RecommendedAction> Recommendations { get; set; } = new();

    /// <summary>True when the model returned any structured card content.</summary>
    public bool HasStructuredContent =>
        !string.IsNullOrWhiteSpace(ExecutiveSummary)
        || Findings.Count > 0
        || Risks.Count > 0
        || Recommendations.Count > 0;
}

public interface IAIModelAdapter
{
    Task<AIModelResponse> GenerateAsync(AIModelRequest request, CancellationToken cancellationToken);
}
