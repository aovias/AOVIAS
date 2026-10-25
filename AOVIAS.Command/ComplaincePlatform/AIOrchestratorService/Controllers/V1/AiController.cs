using AIOrchestratorService.Application.AI.Ace;
using AIOrchestratorService.Application.AI.Actions;
using AIOrchestratorService.Application.AI.Advisor;
using AIOrchestratorService.Application.AI.Autonomy;
using AIOrchestratorService.Application.AI.Evidence;
using AIOrchestratorService.Application.AI.Governance;
using AIOrchestratorService.Application.AI.Knowledge;
using AIOrchestratorService.Application.AI.Models;
using AIOrchestratorService.Application.AI.Orchestration;
using AIOrchestratorService.Application.AI.Predictive;
using AIOrchestratorService.Application.AI.Proactive;
using AIOrchestratorService.Application.AI.Reliability;
using AIOrchestratorService.Application.AI.Reporting;
using AIOrchestratorService.Application.AI.Retrieval;
using AIOrchestratorService.Application.AI.Risk;
using AIOrchestratorService.Application.AI.Workflow;
using AIOrchestratorService.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIOrchestratorService.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/ai")]
[Authorize(Policy = "AiScope")]
public class AiController : ControllerBase
{
    private readonly IAIFeaturePolicy _featurePolicy;
    private readonly IAIOrchestrationService _orchestrationService;
    private readonly IToolPreExecutionValidator _preExecutionValidator;
    private readonly IAIToolExecutor _toolExecutor;
    private readonly IAIAuditService _aiAuditService;
    private readonly IProactiveSuggestionStore _proactiveStore;
    private readonly IAcceptanceGateService _acceptanceGateService;
    private readonly IAgentOrchestrator _agentOrchestrator;
    private readonly IMetricsTracker _metricsTracker;
    private readonly IRetrievalService _retrievalService;
    private readonly IKnowledgeSearchService _knowledgeSearchService;
    private readonly IRiskIntelligenceService _riskIntelligence;
    private readonly IPredictiveAnalyticsService _predictiveAnalytics;
    private readonly IAiReportingEngine _reportingEngine;
    private readonly IAiWorkflowHookService _workflowHooks;
    private readonly IAceEngine _aceEngine;
    private readonly IEvidenceReviewerService _evidenceReviewer;
    private readonly IComplianceAdvisorService _complianceAdvisor;
    private readonly ILogger<AiController> _logger;

    public AiController(
        IAIFeaturePolicy featurePolicy,
        IAIOrchestrationService orchestrationService,
        IToolPreExecutionValidator preExecutionValidator,
        IAIToolExecutor toolExecutor,
        IAIAuditService aiAuditService,
        IProactiveSuggestionStore proactiveStore,
        IAcceptanceGateService acceptanceGateService,
        IAgentOrchestrator agentOrchestrator,
        IMetricsTracker metricsTracker,
        IRetrievalService retrievalService,
        IKnowledgeSearchService knowledgeSearchService,
        IRiskIntelligenceService riskIntelligence,
        IPredictiveAnalyticsService predictiveAnalytics,
        IAiReportingEngine reportingEngine,
        IAiWorkflowHookService workflowHooks,
        IAceEngine aceEngine,
        IEvidenceReviewerService evidenceReviewer,
        IComplianceAdvisorService complianceAdvisor,
        ILogger<AiController> logger)
    {
        _featurePolicy = featurePolicy;
        _orchestrationService = orchestrationService;
        _preExecutionValidator = preExecutionValidator;
        _toolExecutor = toolExecutor;
        _aiAuditService = aiAuditService;
        _proactiveStore = proactiveStore;
        _acceptanceGateService = acceptanceGateService;
        _agentOrchestrator = agentOrchestrator;
        _metricsTracker = metricsTracker;
        _retrievalService = retrievalService;
        _knowledgeSearchService = knowledgeSearchService;
        _riskIntelligence = riskIntelligence;
        _predictiveAnalytics = predictiveAnalytics;
        _reportingEngine = reportingEngine;
        _workflowHooks = workflowHooks;
        _aceEngine = aceEngine;
        _evidenceReviewer = evidenceReviewer;
        _complianceAdvisor = complianceAdvisor;
        _logger = logger;
    }

    /// <summary>Phase 5.1 — hybrid/semantic/keyword knowledge search with version-aware citations.</summary>
    [HttpPost("knowledge/search")]
    public async Task<IActionResult> KnowledgeSearch([FromBody] KnowledgeQuery query, CancellationToken cancellationToken)
    {
        if (!_featurePolicy.IsReadEnabled())
        {
            return Forbid();
        }

        if (query.HospitalId == Guid.Empty)
        {
            var hospitalHeader = Request.Headers["X-Hospital-Id"].FirstOrDefault();
            if (Guid.TryParse(hospitalHeader, out var hospitalId))
            {
                query.HospitalId = hospitalId;
            }
        }

        var result = await _retrievalService.SearchKnowledgeAsync(query, cancellationToken);
        return Ok(result);
    }

    [HttpPost("knowledge/reindex-seed")]
    public async Task<IActionResult> ReindexSeed([FromBody] KnowledgeQuery query, CancellationToken cancellationToken)
    {
        if (!_featurePolicy.IsAssistEnabled())
        {
            return Forbid();
        }

        if (query.HospitalId == Guid.Empty)
        {
            return BadRequest(new { error = "HospitalId required" });
        }

        await _knowledgeSearchService.SeedDemoIndexAsync(query.HospitalId, query.FrameworkId, cancellationToken);
        return Ok(new { seeded = true, query.HospitalId });
    }

    /// <summary>Phase 5.5 — risk theme clustering + emerging risks (advisory).</summary>
    [HttpGet("risk-intelligence/analyze")]
    [HttpPost("risk-intelligence/analyze")]
    public async Task<IActionResult> AnalyzeRiskIntelligence(
        [FromBody] RiskIntelligenceAnalyzeRequest? body,
        [FromQuery] Guid? hospitalId,
        [FromQuery] Guid? frameworkId,
        CancellationToken cancellationToken)
    {
        if (!_featurePolicy.IsReadEnabled())
        {
            return Forbid();
        }

        var request = body ?? new RiskIntelligenceAnalyzeRequest();
        if (request.HospitalId == Guid.Empty)
        {
            request.HospitalId = hospitalId ?? Guid.Empty;
        }

        if (request.HospitalId == Guid.Empty &&
            Guid.TryParse(Request.Headers["X-Hospital-Id"], out var headerHospital))
        {
            request.HospitalId = headerHospital;
        }

        if (request.FrameworkId == null)
        {
            request.FrameworkId = frameworkId;
        }

        if (request.HospitalId == Guid.Empty)
        {
            return BadRequest(new { error = "HospitalId required" });
        }

        var result = await _riskIntelligence.AnalyzeAsync(request, cancellationToken);
        return Ok(result);
    }

    /// <summary>Phase 5.7 — readiness forecast.</summary>
    [HttpGet("predict/readiness")]
    public async Task<IActionResult> PredictReadiness(
        [FromQuery] Guid? hospitalId,
        [FromQuery] Guid? frameworkId,
        [FromQuery] string horizon = "monthly",
        CancellationToken cancellationToken = default)
    {
        if (!_featurePolicy.IsReadEnabled())
        {
            return Forbid();
        }

        if (!TryResolveHospitalId(hospitalId, out var resolved))
        {
            return BadRequest(new { error = "HospitalId required" });
        }

        var result = await _predictiveAnalytics.ForecastReadinessAsync(new PredictiveRequest
        {
            HospitalId = resolved,
            FrameworkId = frameworkId,
            Horizon = horizon
        }, cancellationToken);
        return Ok(result);
    }

    /// <summary>Phase 5.7 — delay / expiry forecasts.</summary>
    [HttpGet("predict/delays")]
    public async Task<IActionResult> PredictDelays(
        [FromQuery] Guid? hospitalId,
        [FromQuery] Guid? frameworkId,
        [FromQuery] string horizon = "monthly",
        CancellationToken cancellationToken = default)
    {
        if (!_featurePolicy.IsReadEnabled())
        {
            return Forbid();
        }

        if (!TryResolveHospitalId(hospitalId, out var resolved))
        {
            return BadRequest(new { error = "HospitalId required" });
        }

        var result = await _predictiveAnalytics.ForecastDelaysAsync(new PredictiveRequest
        {
            HospitalId = resolved,
            FrameworkId = frameworkId,
            Horizon = horizon
        }, cancellationToken);
        return Ok(result);
    }

    /// <summary>Phase 5.7 — predictive overview (readiness + delays + departments).</summary>
    [HttpGet("predict/overview")]
    public async Task<IActionResult> PredictOverview(
        [FromQuery] Guid? hospitalId,
        [FromQuery] Guid? frameworkId,
        [FromQuery] string horizon = "monthly",
        CancellationToken cancellationToken = default)
    {
        if (!_featurePolicy.IsReadEnabled())
        {
            return Forbid();
        }

        if (!TryResolveHospitalId(hospitalId, out var resolved))
        {
            return BadRequest(new { error = "HospitalId required" });
        }

        var result = await _predictiveAnalytics.GetOverviewAsync(new PredictiveRequest
        {
            HospitalId = resolved,
            FrameworkId = frameworkId,
            Horizon = horizon
        }, cancellationToken);
        return Ok(result);
    }

    /// <summary>Phase 5.6 — markdown AI report generation.</summary>
    [HttpPost("reports/generate")]
    public async Task<IActionResult> GenerateReport([FromBody] AiReportGenerateRequest request, CancellationToken cancellationToken)
    {
        if (!_featurePolicy.IsReadEnabled())
        {
            return Forbid();
        }

        if (request.HospitalId == Guid.Empty &&
            Guid.TryParse(Request.Headers["X-Hospital-Id"], out var hospitalId))
        {
            request.HospitalId = hospitalId;
        }

        if (request.HospitalId == Guid.Empty)
        {
            return BadRequest(new { error = "HospitalId required" });
        }

        var result = await _reportingEngine.GenerateAsync(request, cancellationToken);
        return Ok(result);
    }

    /// <summary>Phase 5 ACE — list advisory insights (works when Read enabled; Proactive flag not required).</summary>
    [HttpGet("ace/insights")]
    public async Task<IActionResult> ListAceInsights(
        [FromQuery] Guid? hospitalId,
        [FromQuery] string? status,
        [FromQuery] int take = 20,
        CancellationToken cancellationToken = default)
    {
        if (!_featurePolicy.IsReadEnabled())
        {
            return Forbid();
        }

        if (!TryResolveHospitalId(hospitalId, out var resolved))
        {
            return BadRequest(new { error = "HospitalId required" });
        }

        var items = await _aceEngine.ListInsightsAsync(resolved, status, take, cancellationToken);
        return Ok(items);
    }

    [HttpPost("ace/insights/{insightId:guid}/acknowledge")]
    public async Task<IActionResult> AcknowledgeAceInsight(
        Guid insightId,
        [FromBody] AceAcknowledgeRequest? body,
        CancellationToken cancellationToken)
    {
        if (!_featurePolicy.IsReadEnabled())
        {
            return Forbid();
        }

        Guid? userId = body?.UserId;
        if (userId == null && Guid.TryParse(User.FindFirst("sub")?.Value, out var sub))
        {
            userId = sub;
        }

        var updated = await _aceEngine.AcknowledgeAsync(insightId, userId, cancellationToken);
        return updated == null ? NotFound() : Ok(updated);
    }

    /// <summary>Phase 5.8 — workflow assistant hooks (advisory suggestions only).</summary>
    [HttpPost("workflow/hooks/{eventType}")]
    public async Task<IActionResult> WorkflowHook(
        string eventType,
        [FromBody] AiWorkflowHookRequest request,
        CancellationToken cancellationToken)
    {
        if (!_featurePolicy.IsReadEnabled())
        {
            return Forbid();
        }

        if (request.HospitalId == Guid.Empty &&
            Guid.TryParse(Request.Headers["X-Hospital-Id"], out var hospitalId))
        {
            request.HospitalId = hospitalId;
        }

        if (request.HospitalId == Guid.Empty)
        {
            return BadRequest(new { error = "HospitalId required" });
        }

        AiWorkflowHookResult result = eventType.Trim().ToLowerInvariant() switch
        {
            "evidenceuploaded" or "evidence-uploaded" =>
                await _workflowHooks.OnEvidenceUploadedAsync(request, cancellationToken),
            "auditclosed" or "audit-closed" =>
                await _workflowHooks.OnAuditClosedAsync(request, cancellationToken),
            "incidentclosed" or "incident-closed" =>
                await _workflowHooks.OnIncidentClosedAsync(request, cancellationToken),
            "trainingcompleted" or "training-completed" =>
                await _workflowHooks.OnTrainingCompletedAsync(request, cancellationToken),
            _ => new AiWorkflowHookResult
            {
                EventType = eventType,
                SuggestionCreated = false,
                Message = "Unknown event type. Supported: EvidenceUploaded, AuditClosed, IncidentClosed, TrainingCompleted."
            }
        };

        return result.SuggestionCreated ? Ok(result) : BadRequest(result);
    }

    [HttpPost("read/explain")]
    public async Task<IActionResult> Explain([FromBody] AIRequest request, CancellationToken cancellationToken)
    {
        if (!_featurePolicy.IsReadEnabled())
        {
            return Forbid();
        }

        NormalizeRequest(request);
        request.UseCase = AIUseCase.ExplainCompliance;

        var response = await _orchestrationService.RunReadOnlyAsync(request, cancellationToken);
        await _aiAuditService.LogAsync(request, response, "read.explain", cancellationToken);
        return Ok(response);
    }

    [HttpPost("read/query")]
    public async Task<IActionResult> Query([FromBody] AIRequest request, CancellationToken cancellationToken)
    {
        if (!_featurePolicy.IsReadEnabled())
        {
            return Forbid();
        }

        NormalizeRequest(request);
        request.UseCase = AIUseCase.QueryCompliance;
        var response = await _orchestrationService.RunReadOnlyAsync(request, cancellationToken);
        await _aiAuditService.LogAsync(request, response, "read.query", cancellationToken);
        return Ok(response);
    }

    [HttpPost("assist/suggest")]
    public async Task<IActionResult> Suggest([FromBody] AIRequest request, CancellationToken cancellationToken)
    {
        if (!_featurePolicy.IsAssistEnabled())
        {
            return Forbid();
        }

        NormalizeRequest(request);
        var response = await _orchestrationService.RunAssistiveAsync(request, cancellationToken);
        response.HumanApprovalRequired = true;
        await _aiAuditService.LogAsync(request, response, "assist.suggest", cancellationToken);
        return Ok(response);
    }

    /// <summary>Phase 5.3 — advisory evidence review (never auto-approves).</summary>
    [HttpPost("evidence/review")]
    public async Task<IActionResult> ReviewEvidence([FromBody] EvidenceReviewRequest request, CancellationToken cancellationToken)
    {
        if (!_featurePolicy.IsAssistEnabled())
        {
            return Forbid();
        }

        if (request.HospitalId == Guid.Empty &&
            Guid.TryParse(Request.Headers["X-Hospital-Id"], out var hospitalId))
        {
            request.HospitalId = hospitalId;
        }

        var result = await _evidenceReviewer.ReviewAsync(request, cancellationToken);
        result.HumanApprovalRequired = true;
        return Ok(result);
    }

    /// <summary>Phase 5.4 — CAPA corrective/preventive proposals (human approval required).</summary>
    [HttpPost("advisor/capa-suggest")]
    public async Task<IActionResult> SuggestCapa([FromBody] CapaSuggestRequest request, CancellationToken cancellationToken)
    {
        if (!_featurePolicy.IsAssistEnabled())
        {
            return Forbid();
        }

        if (request.HospitalId == Guid.Empty &&
            Guid.TryParse(Request.Headers["X-Hospital-Id"], out var hospitalId))
        {
            request.HospitalId = hospitalId;
        }

        var result = await _complianceAdvisor.SuggestCapaAsync(request, cancellationToken);
        result.HumanApprovalRequired = true;
        return Ok(result);
    }

    [HttpPost("actions/preview")]
    public async Task<IActionResult> PreviewAction([FromBody] AIToolActionRequest request, CancellationToken cancellationToken)
    {
        if (!_featurePolicy.IsActionEnabled())
        {
            return Forbid();
        }

        NormalizeRequest(request.Request);
        var preview = await _preExecutionValidator.ValidateAsync(
            request.Request,
            request.Action,
            approved: request.Approved,
            cancellationToken);

        return Ok(preview);
    }

    [HttpPost("actions/execute")]
    public async Task<IActionResult> ExecuteAction([FromBody] AIToolActionRequest request, CancellationToken cancellationToken)
    {
        if (!_featurePolicy.IsActionEnabled())
        {
            return Forbid();
        }

        NormalizeRequest(request.Request);
        var preview = await _preExecutionValidator.ValidateAsync(
            request.Request,
            request.Action,
            approved: request.Approved,
            cancellationToken);

        if (!preview.Allowed)
        {
            return BadRequest(preview);
        }

        var execution = await _toolExecutor.ExecuteAsync(request.Request, request.Action, cancellationToken);
        var actionResponse = new AIResponse
        {
            ComplianceHint = "Guarded action executed through policy-approved tool pipeline.",
            ConfidenceScore = 0.8m,
            CompositeConfidenceScore = 0.8m,
            ValidationOutcome = AIValidationOutcome.Accept,
            ContextVersionId = request.Request.ContextVersionId,
            PromptTemplateId = request.Request.PromptTemplateId,
            PromptTemplateVersion = request.Request.PromptTemplateVersion,
            HumanApprovalRequired = false,
            SuggestedAction = request.Action,
            Reasoning = new List<AIReasoningStep>
            {
                new() { Code = "ACTION_EXECUTION", Message = $"Tool {request.Action.ToolName} execution completed." }
            },
            SourceReferences = new List<AISourceReference>
            {
                new() { SourceType = "ComplianceService", SourceId = "command-api", RelevanceScore = 1m }
            }
        };

        await _aiAuditService.LogAsync(request.Request, actionResponse, "action.execute", cancellationToken);
        return Ok(new { preview, execution, response = actionResponse });
    }

    [HttpGet("proactive/suggestions/{entityType}/{entityId}")]
    public IActionResult GetProactiveSuggestions(string entityType, string entityId)
    {
        if (!_featurePolicy.IsProactiveEnabled())
        {
            return Forbid();
        }

        var items = _proactiveStore.GetForEntity(entityType, entityId);
        return Ok(items);
    }

    [HttpGet("governance/acceptance-gates")]
    public async Task<IActionResult> GetAcceptanceGates(CancellationToken cancellationToken)
    {
        var report = await _acceptanceGateService.BuildReportAsync(cancellationToken);
        return Ok(report);
    }

    /// <summary>Phase 5.10 — in-process AI governance counters from <see cref="IMetricsTracker"/>.</summary>
    [HttpGet("governance/metrics")]
    public IActionResult GetGovernanceMetrics()
    {
        var snapshot = _metricsTracker.GetSnapshot();
        return Ok(new
        {
            capturedAtUtc = DateTime.UtcNow,
            counters = snapshot,
            totals = new
            {
                validatorOutcomes = snapshot.Where(x => x.Key.StartsWith("validator.outcome.", StringComparison.OrdinalIgnoreCase)).Sum(x => x.Value),
                timeouts = snapshot.Where(x => x.Key.StartsWith("timeouts.", StringComparison.OrdinalIgnoreCase)).Sum(x => x.Value),
                hallucinationFlags = snapshot.TryGetValue("governance.hallucination.flagged", out var h) ? h : 0,
                userAccepted = snapshot.TryGetValue("user.accepted", out var a) ? a : 0,
                userRejected = snapshot.TryGetValue("user.rejected", out var r) ? r : 0
            }
        });
    }

    [HttpPost("workflows/start")]
    public async Task<IActionResult> StartWorkflow([FromBody] StartWorkflowRequest request, CancellationToken cancellationToken)
    {
        if (!_featurePolicy.IsAutonomyEnabled())
        {
            return Forbid();
        }

        NormalizeRequest(request.AIRequest);
        var execution = await _agentOrchestrator.StartAsync(request, cancellationToken);
        return Ok(execution);
    }

    [HttpPost("workflows/{executionId:guid}/approve-step")]
    public async Task<IActionResult> ApproveStep(Guid executionId, [FromBody] ApproveStepRequest request, CancellationToken cancellationToken)
    {
        if (!_featurePolicy.IsActionEnabled())
        {
            return Forbid();
        }

        if (!Guid.TryParse(User.FindFirst("sub")?.Value, out var approvedBy))
        {
            approvedBy = Guid.Empty;
        }

        var result = await _agentOrchestrator.ApproveAndResumeAsync(executionId, approvedBy, request.Reason, cancellationToken);
        if (result)
        {
            _metricsTracker.TrackUserAcceptance("WorkflowApproval", GetTenantKey());
        }
        return result ? Ok(new { approved = true }) : NotFound();
    }

    [HttpPost("workflows/{executionId:guid}/resume")]
    public async Task<IActionResult> ResumeWorkflow(Guid executionId, CancellationToken cancellationToken)
    {
        var execution = await _agentOrchestrator.GetAsync(executionId, cancellationToken);
        if (execution == null)
        {
            return NotFound();
        }

        var result = await _agentOrchestrator.ApproveAndResumeAsync(executionId, execution.ApprovedBy ?? Guid.Empty, execution.ApprovalReason ?? "resume", cancellationToken);
        return result ? Ok(new { resumed = true }) : NotFound();
    }

    [HttpPost("workflows/{executionId:guid}/reject-step")]
    public async Task<IActionResult> RejectStep(Guid executionId, [FromBody] RejectStepRequest request, CancellationToken cancellationToken)
    {
        if (!_featurePolicy.IsActionEnabled())
        {
            return Forbid();
        }

        if (!Guid.TryParse(User.FindFirst("sub")?.Value, out var rejectedBy))
        {
            rejectedBy = Guid.Empty;
        }

        var result = await _agentOrchestrator.RejectAsync(executionId, rejectedBy, request.Reason, cancellationToken);
        if (result)
        {
            var hallucinationRelated = request.Reason.Contains("hallucination", StringComparison.OrdinalIgnoreCase);
            _metricsTracker.TrackUserRejection("WorkflowApproval", GetTenantKey(), hallucinationRelated);
        }
        return result ? Ok(new { rejected = true }) : NotFound();
    }

    [HttpPost("workflows/{executionId:guid}/cancel")]
    public async Task<IActionResult> CancelWorkflow(Guid executionId, [FromBody] CancelWorkflowRequest request, CancellationToken cancellationToken)
    {
        if (!_featurePolicy.IsActionEnabled())
        {
            return Forbid();
        }

        var result = await _agentOrchestrator.CancelAsync(executionId, request.Reason, cancellationToken);
        return result ? Ok(new { cancelled = true }) : NotFound();
    }

    [HttpGet("workflows/{executionId:guid}")]
    public async Task<IActionResult> GetWorkflow(Guid executionId, CancellationToken cancellationToken)
    {
        var execution = await _agentOrchestrator.GetAsync(executionId, cancellationToken);
        return execution == null ? NotFound() : Ok(execution);
    }

    [HttpGet("workflows")]
    public async Task<IActionResult> ListWorkflows([FromQuery] AgentExecutionStatus? status, CancellationToken cancellationToken)
    {
        var list = await _agentOrchestrator.ListAsync(status, cancellationToken);
        return Ok(list);
    }

    [HttpPost("workflows/trigger-from-event")]
    public async Task<IActionResult> TriggerFromEvent([FromBody] TriggerWorkflowFromEventRequest request, CancellationToken cancellationToken)
    {
        if (!_featurePolicy.IsEventDrivenAutonomyEnabled())
        {
            return Forbid();
        }

        var start = new StartWorkflowRequest
        {
            Goal = $"{request.EventType}:{request.EntityType}:{request.EntityId}",
            AIRequest = new AIRequest
            {
                HospitalId = request.TenantId,
                Prompt = $"Auto-trigger from event {request.EventType}",
                ContextVersionId = request.ContextVersionId ?? $"{request.TenantId:N}:{DateTime.UtcNow:yyyyMMddHHmm}",
                PromptTemplateId = "TaskRecommendation",
                PromptTemplateVersion = "1.0",
                UseCase = AIUseCase.TaskRecommendation
            }
        };
        var execution = await _agentOrchestrator.StartAsync(start, cancellationToken);
        return Ok(execution);
    }

    private void NormalizeRequest(AIRequest request)
    {
        if (request.HospitalId == Guid.Empty &&
            Guid.TryParse(Request.Headers["X-Hospital-Id"], out var hospitalId))
        {
            request.HospitalId = hospitalId;
        }

        request.ContextVersionId = string.IsNullOrWhiteSpace(request.ContextVersionId)
            ? $"{request.HospitalId:N}:{DateTime.UtcNow:yyyyMMddHHmm}"
            : request.ContextVersionId;

        request.PromptTemplateId = string.IsNullOrWhiteSpace(request.PromptTemplateId)
            ? "ExplainCompliance"
            : request.PromptTemplateId;

        request.PromptTemplateVersion = string.IsNullOrWhiteSpace(request.PromptTemplateVersion)
            ? "1.0"
            : request.PromptTemplateVersion;
    }

    private bool TryResolveHospitalId(Guid? queryHospitalId, out Guid hospitalId)
    {
        hospitalId = queryHospitalId ?? Guid.Empty;
        if (hospitalId == Guid.Empty &&
            Guid.TryParse(Request.Headers["X-Hospital-Id"], out var headerHospital))
        {
            hospitalId = headerHospital;
        }

        return hospitalId != Guid.Empty;
    }

    private string GetTenantKey()
    {
        if (Guid.TryParse(Request.Headers["X-Hospital-Id"], out var hospitalId))
        {
            return hospitalId.ToString("N");
        }

        return "unknown-tenant";
    }
}

public sealed class AIToolActionRequest
{
    public AIRequest Request { get; set; } = new();
    public AISuggestedAction Action { get; set; } = new();
    public bool Approved { get; set; }
}

public sealed class ApproveStepRequest
{
    public string Reason { get; set; } = "approved";
}

public sealed class CancelWorkflowRequest
{
    public string Reason { get; set; } = "cancelled";
}

public sealed class RejectStepRequest
{
    public string Reason { get; set; } = "rejected";
}

public sealed class TriggerWorkflowFromEventRequest
{
    public string EventType { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public Guid TenantId { get; set; }
    public string? ContextVersionId { get; set; }
}
