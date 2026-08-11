using AIOrchestratorService.Application.AI.Governance;
using AIOrchestratorService.Application.AI.Models;
using AIOrchestratorService.Application.AI.Orchestration;
using AIOrchestratorService.Application.AI.Retrieval;

namespace AIOrchestratorService.Application.AI.Advisor;

public interface IComplianceAdvisorService
{
    Task<CapaSuggestResult> SuggestCapaAsync(CapaSuggestRequest request, CancellationToken cancellationToken = default);
}

/// <summary>
/// CAPA / corrective / preventive proposal advisor. Suggestions are proposals only;
/// HumanApprovalRequired is always true.
/// </summary>
public sealed class ComplianceAdvisorService : IComplianceAdvisorService
{
    private readonly IRetrievalService _retrievalService;
    private readonly IAIOrchestrationService _orchestrationService;
    private readonly IAIAuditService _auditService;
    private readonly ILogger<ComplianceAdvisorService> _logger;

    public ComplianceAdvisorService(
        IRetrievalService retrievalService,
        IAIOrchestrationService orchestrationService,
        IAIAuditService auditService,
        ILogger<ComplianceAdvisorService> logger)
    {
        _retrievalService = retrievalService;
        _orchestrationService = orchestrationService;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task<CapaSuggestResult> SuggestCapaAsync(CapaSuggestRequest request, CancellationToken cancellationToken = default)
    {
        var traceId = Guid.NewGuid().ToString("N");
        var deficiencyText = string.IsNullOrWhiteSpace(request.DeficiencySummary)
            ? "Compliance deficiency requiring CAPA"
            : request.DeficiencySummary.Trim();

        var retrieval = await _retrievalService.BuildContextAsync(new AIRequest
        {
            Prompt = $"CAPA suggestions for: {deficiencyText}",
            HospitalId = request.HospitalId,
            FrameworkId = request.FrameworkId,
            OrganizationId = request.OrganizationId,
            ObjectiveElementId = request.ObjectiveElementId,
            UseCase = AIUseCase.TaskRecommendation,
            PromptTemplateId = "TaskRecommendation",
            PromptTemplateVersion = "1.0",
            ContextVersionId = $"{request.HospitalId:N}:capa-suggest"
        }, cancellationToken);

        var suggestions = BuildHeuristicSuggestions(request, deficiencyText, retrieval.Sources);

        GroundedAnswer? grounded = null;
        if (request.UseLlmAssist)
        {
            try
            {
                var assist = await _orchestrationService.RunAssistiveAsync(new AIRequest
                {
                    Prompt =
                        $"Propose corrective and preventive CAPA actions (proposal only, do not execute) for: {deficiencyText}. " +
                        $"Root cause hint: {request.RootCauseHint ?? "not provided"}.",
                    HospitalId = request.HospitalId,
                    FrameworkId = request.FrameworkId,
                    OrganizationId = request.OrganizationId,
                    ObjectiveElementId = request.ObjectiveElementId,
                    UseCase = AIUseCase.TaskRecommendation,
                    PromptTemplateId = "TaskRecommendation",
                    PromptTemplateVersion = "1.0",
                    ContextVersionId = retrieval.ContextVersionId
                }, cancellationToken);

                grounded = assist.Grounded;
                if (!string.IsNullOrWhiteSpace(assist.ComplianceHint))
                {
                    suggestions.Insert(0, new CapaSuggestionItem
                    {
                        Kind = "Corrective",
                        Title = "AI-assisted corrective proposal",
                        Proposal = assist.ComplianceHint,
                        ExpectedOutcome = "Addresses the stated deficiency pending human review and approval.",
                        ConfidenceScore = Math.Clamp(assist.CompositeConfidenceScore, 0.2m, 0.75m),
                        Citations = assist.SourceReferences
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Optional LLM assist path failed for CAPA suggest; returning heuristics only.");
            }
        }

        var result = new CapaSuggestResult
        {
            Suggestions = suggestions.Take(6).ToList(),
            Summary = $"{suggestions.Count} CAPA proposal(s) generated. Human approval required before any CAPA creation or status change.",
            HumanApprovalRequired = true,
            TraceId = traceId,
            Grounded = grounded ?? new GroundedAnswer
            {
                Answer = suggestions.FirstOrDefault()?.Proposal ?? "No CAPA proposals generated.",
                Citations = retrieval.Sources,
                ConfidenceScore = suggestions.Count > 0 ? suggestions.Average(s => s.ConfidenceScore) : 0.3m,
                TraceId = traceId,
                HumanApprovalRequired = true,
                Retrieval = retrieval
            }
        };

        await _auditService.LogAsync(
            new AIRequest
            {
                HospitalId = request.HospitalId,
                FrameworkId = request.FrameworkId,
                OrganizationId = request.OrganizationId,
                ObjectiveElementId = request.ObjectiveElementId,
                Prompt = deficiencyText,
                PromptTemplateId = "TaskRecommendation",
                PromptTemplateVersion = "1.0",
                UseCase = AIUseCase.TaskRecommendation,
                ContextVersionId = retrieval.ContextVersionId
            },
            new AIResponse
            {
                ComplianceHint = result.Summary,
                ConfidenceScore = result.Grounded?.ConfidenceScore ?? 0.4m,
                CompositeConfidenceScore = result.Grounded?.ConfidenceScore ?? 0.4m,
                HumanApprovalRequired = true,
                ValidationOutcome = AIValidationOutcome.Accept,
                SourceReferences = retrieval.Sources,
                PromptTemplateId = "TaskRecommendation",
                PromptTemplateVersion = "1.0",
                ContextVersionId = retrieval.ContextVersionId,
                TraceId = traceId,
                Grounded = result.Grounded
            },
            "advisor.capa-suggest",
            cancellationToken);

        return result;
    }

    private static List<CapaSuggestionItem> BuildHeuristicSuggestions(
        CapaSuggestRequest request,
        string deficiencyText,
        List<AISourceReference> sources)
    {
        var root = string.IsNullOrWhiteSpace(request.RootCauseHint)
            ? "process control gap"
            : request.RootCauseHint.Trim();

        return
        [
            new CapaSuggestionItem
            {
                Kind = "Corrective",
                Title = "Immediate corrective containment",
                Proposal =
                    $"Contain and correct the issue described as '{Truncate(deficiencyText, 160)}' by assigning an owner, documenting the fix, and attaching verification evidence against the related objective element.",
                ExpectedOutcome = "Deficiency is remediated with auditable proof of correction.",
                ConfidenceScore = 0.62m,
                Citations = sources.Take(3).ToList()
            },
            new CapaSuggestionItem
            {
                Kind = "Corrective",
                Title = "Root-cause aligned corrective action",
                Proposal =
                    $"Address root cause ({root}) with a documented procedure update or training reinforcement, then verify effectiveness within 30–60 days.",
                ExpectedOutcome = "Root cause is mitigated and recurrence risk declines.",
                ConfidenceScore = 0.58m,
                Citations = sources.Take(3).ToList()
            },
            new CapaSuggestionItem
            {
                Kind = "Preventive",
                Title = "Preventive monitoring control",
                Proposal =
                    "Add a preventive control: periodic checklist / KPI watch for the same failure mode, with escalation when thresholds breach.",
                ExpectedOutcome = "Early detection prevents recurrence before audit impact.",
                ConfidenceScore = 0.55m,
                Citations = sources.Take(2).ToList()
            },
            new CapaSuggestionItem
            {
                Kind = "Preventive",
                Title = "Competence & awareness reinforcement",
                Proposal =
                    "Schedule targeted staff awareness or competency verification for the department owning this deficiency; retain attendance and assessment records as evidence.",
                ExpectedOutcome = "People-dependent failure modes are reduced through demonstrated competence.",
                ConfidenceScore = 0.52m,
                Citations = sources.Take(2).ToList()
            }
        ];
    }

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..(max - 3)] + "...";
}
