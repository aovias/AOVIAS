namespace AIOrchestratorService.Domain;

public class AIAuditEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;
    public Guid HospitalId { get; set; }
    public string UseCase { get; set; } = string.Empty;
    public string PromptTemplateId { get; set; } = string.Empty;
    public string PromptTemplateVersion { get; set; } = string.Empty;
    public string ContextVersionId { get; set; } = string.Empty;
    public decimal ConfidenceScore { get; set; }
    public decimal CompositeConfidenceScore { get; set; }
    public string ValidationOutcome { get; set; } = string.Empty;
    public string DecisionPath { get; set; } = string.Empty;
    public string SourceReferencesJson { get; set; } = "[]";

    /// <summary>Phase 5.10 — model / prompt template version used for the audited turn.</summary>
    public string ModelVersion { get; set; } = string.Empty;
}
