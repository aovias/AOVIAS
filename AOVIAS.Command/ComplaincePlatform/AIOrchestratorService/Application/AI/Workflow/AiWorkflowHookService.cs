using AIOrchestratorService.Application.AI.Ace;
using AIOrchestratorService.Application.AI.Models;
using AIOrchestratorService.Application.AI.Proactive;
using AIOrchestratorService.Domain.Entities;

namespace AIOrchestratorService.Application.AI.Workflow;

public sealed class AiWorkflowHookRequest
{
    public Guid HospitalId { get; set; }
    public Guid? FrameworkId { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string? Title { get; set; }
}

public sealed class AiWorkflowHookResult
{
    public string EventType { get; set; } = string.Empty;
    public bool SuggestionCreated { get; set; }
    public Guid? AceInsightId { get; set; }
    public string Message { get; set; } = string.Empty;
}

public interface IAiWorkflowHookService
{
    Task<AiWorkflowHookResult> OnEvidenceUploadedAsync(AiWorkflowHookRequest request, CancellationToken cancellationToken);
    Task<AiWorkflowHookResult> OnAuditClosedAsync(AiWorkflowHookRequest request, CancellationToken cancellationToken);
    Task<AiWorkflowHookResult> OnIncidentClosedAsync(AiWorkflowHookRequest request, CancellationToken cancellationToken);
    Task<AiWorkflowHookResult> OnTrainingCompletedAsync(AiWorkflowHookRequest request, CancellationToken cancellationToken);
}

/// <summary>
/// Workflow assistant hooks — create advisory suggestions only; never mutate regulatory state.
/// </summary>
public sealed class AiWorkflowHookService : IAiWorkflowHookService
{
    private readonly IAceInsightStore _aceStore;
    private readonly IProactiveSuggestionStore _proactiveStore;
    private readonly IAceNotificationBridge _notificationBridge;
    private readonly ILogger<AiWorkflowHookService> _logger;

    public AiWorkflowHookService(
        IAceInsightStore aceStore,
        IProactiveSuggestionStore proactiveStore,
        IAceNotificationBridge notificationBridge,
        ILogger<AiWorkflowHookService> logger)
    {
        _aceStore = aceStore;
        _proactiveStore = proactiveStore;
        _notificationBridge = notificationBridge;
        _logger = logger;
    }

    public Task<AiWorkflowHookResult> OnEvidenceUploadedAsync(AiWorkflowHookRequest request, CancellationToken cancellationToken)
        => CreateAdvisoryAsync(
            "EvidenceUploaded",
            request,
            "Evidence",
            "Review newly uploaded evidence",
            "Evidence upload detected. Consider verifying completeness and mapping to the objective element before submission lock.",
            ["Verify evidence quality", "Confirm OE/task linkage"],
            cancellationToken);

    public Task<AiWorkflowHookResult> OnAuditClosedAsync(AiWorkflowHookRequest request, CancellationToken cancellationToken)
        => CreateAdvisoryAsync(
            "AuditClosed",
            request,
            "Audit",
            "Close-out follow-ups after audit",
            "Audit closed. Review residual findings and ensure CAPAs or effectiveness checks are scheduled.",
            ["Review open findings from closed audit", "Schedule management review note"],
            cancellationToken);

    public Task<AiWorkflowHookResult> OnIncidentClosedAsync(AiWorkflowHookRequest request, CancellationToken cancellationToken)
        => CreateAdvisoryAsync(
            "IncidentClosed",
            request,
            "Incident",
            "Incident closure learning loop",
            "Incident closed. Capture lessons learned and check whether a related risk register update is warranted.",
            ["Update related risk if needed", "Share lessons with quality forum"],
            cancellationToken);

    public Task<AiWorkflowHookResult> OnTrainingCompletedAsync(AiWorkflowHookRequest request, CancellationToken cancellationToken)
        => CreateAdvisoryAsync(
            "TrainingCompleted",
            request,
            "Training",
            "Training completion — competency evidence",
            "Training completed. Ensure competency records and document acknowledgements are current for audit readiness.",
            ["Attach training evidence to relevant OEs", "Refresh department training heatmap"],
            cancellationToken);

    private async Task<AiWorkflowHookResult> CreateAdvisoryAsync(
        string eventType,
        AiWorkflowHookRequest request,
        string category,
        string title,
        string summary,
        IReadOnlyList<string> actions,
        CancellationToken cancellationToken)
    {
        var entityLabel = string.IsNullOrWhiteSpace(request.Title) ? request.EntityId : request.Title;
        var fullTitle = $"{title}: {entityLabel}";
        var citations = new List<AISourceReference>
        {
            new()
            {
                SourceType = request.EntityType,
                SourceId = request.EntityId,
                Summary = $"{eventType} advisory for {entityLabel}",
                RelevanceScore = 0.8m
            }
        };

        var insight = new AceInsight
        {
            Id = Guid.NewGuid(),
            HospitalId = request.HospitalId,
            FrameworkId = request.FrameworkId,
            Category = category,
            Title = fullTitle,
            Summary = summary,
            Reasoning = $"Workflow hook {eventType} produced an advisory suggestion. No regulatory mutation performed.",
            Confidence = 0.75m,
            Priority = 3,
            Status = "Open",
            CitationsJson = AceInsightStore.SerializeCitations(citations),
            SuggestedActionsJson = AceInsightStore.SerializeActions(actions),
            CreatedAtUtc = DateTime.UtcNow
        };

        var dto = await _aceStore.AddAsync(insight, cancellationToken);

        _proactiveStore.Add(new ProactiveSuggestionItem
        {
            EntityType = request.EntityType,
            EntityId = request.EntityId,
            EventType = eventType,
            CreatedAtUtc = DateTime.UtcNow,
            Suggestion = new AIResponse
            {
                ComplianceHint = summary,
                ConfidenceScore = 0.75m,
                CompositeConfidenceScore = 0.75m,
                ValidationOutcome = AIValidationOutcome.Accept,
                HumanApprovalRequired = true,
                Reasoning =
                [
                    new AIReasoningStep { Code = "WORKFLOW_HOOK", Message = $"{eventType} advisory created." },
                    new AIReasoningStep { Code = "NO_STATE_MUTATION", Message = "Regulatory state unchanged." }
                ],
                SourceReferences = citations,
                Grounded = new GroundedAnswer
                {
                    Answer = summary,
                    Citations = citations,
                    ConfidenceScore = 0.75m,
                    ModelVersion = "workflow-hook-v1",
                    TraceId = Guid.NewGuid().ToString("N"),
                    HumanApprovalRequired = true
                }
            }
        });

        await _notificationBridge.NotifyInsightCreatedAsync(dto, cancellationToken);
        _logger.LogInformation(
            "Workflow hook {EventType} created advisory insight {InsightId} for {EntityType}/{EntityId}",
            eventType,
            dto.Id,
            request.EntityType,
            request.EntityId);

        return new AiWorkflowHookResult
        {
            EventType = eventType,
            SuggestionCreated = true,
            AceInsightId = dto.Id,
            Message = "Advisory suggestion stored. Regulatory state was not modified."
        };
    }
}
