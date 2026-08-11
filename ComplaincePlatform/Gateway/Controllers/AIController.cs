using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Gateway.Services;
using Shared.Authorization;
using Shared.Infrastructure.Api;

namespace Gateway.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/ai")]
[Authorize]
[EnableRateLimiting("AIPolicy")]
public class AIController : ControllerBase
{
    private readonly IProxyService _proxyService;
    private readonly IAIFeaturePolicy _featurePolicy;
    private readonly ILogger<AIController> _logger;

    public AIController(
        IProxyService proxyService,
        IAIFeaturePolicy featurePolicy,
        ILogger<AIController> logger)
    {
        _proxyService = proxyService;
        _featurePolicy = featurePolicy;
        _logger = logger;
    }

    [HttpPost("explain-task")]
    public async Task<IActionResult> ExplainTask(
        [FromBody] object requestBody,
        CancellationToken cancellationToken)
    {
        var requestId = HttpContext.TraceIdentifier;

        // Extract hospital_id from JWT token
        var hospitalIdClaim = User.FindFirst("hospital_id")?.Value;
        if (string.IsNullOrEmpty(hospitalIdClaim) || !Guid.TryParse(hospitalIdClaim, out var hospitalId))
        {
            _logger.LogWarning("Missing or invalid hospital_id claim in JWT token");

            return ApiResponseFactory.FailureResult<object>(ApiResponseCode.HOSPITAL_CLAIM_INVALID, requestId);
        }

        if (!_featurePolicy.CanUseRead())
        {
            return ApiResponseFactory.FailureResult<object>(ApiResponseCode.FORBIDDEN, requestId);
        }

        var additionalHeaders = BuildAiHeaders(hospitalId);
        return await _proxyService.ProxyToAIServiceAsync(
            endpoint: "/api/v1.0/ai/read/explain",
            httpMethod: HttpMethod.Post,
            requestBody: requestBody,
            additionalHeaders: additionalHeaders,
            cancellationToken: cancellationToken);
    }

    [HttpPost("query")]
    [RequirePermission("AI.Query.Read")]
    public async Task<IActionResult> Query(
        [FromBody] object requestBody,
        CancellationToken cancellationToken)
    {
        var requestId = HttpContext.TraceIdentifier;
        if (!_featurePolicy.CanUseRead())
        {
            return ApiResponseFactory.FailureResult<object>(ApiResponseCode.FORBIDDEN, requestId);
        }

        if (!TryGetHospitalId(out var hospitalId))
        {
            return ApiResponseFactory.FailureResult<object>(ApiResponseCode.HOSPITAL_CLAIM_INVALID, requestId);
        }

        return await _proxyService.ProxyToAIServiceAsync(
            endpoint: "/api/v1.0/ai/read/query",
            httpMethod: HttpMethod.Post,
            requestBody: requestBody,
            additionalHeaders: BuildAiHeaders(hospitalId),
            cancellationToken: cancellationToken);
    }

    [HttpPost("assist")]
    public async Task<IActionResult> Assist(
        [FromBody] object requestBody,
        CancellationToken cancellationToken)
    {
        var requestId = HttpContext.TraceIdentifier;
        if (!_featurePolicy.CanUseAssist())
        {
            return ApiResponseFactory.FailureResult<object>(ApiResponseCode.FORBIDDEN, requestId);
        }

        if (!TryGetHospitalId(out var hospitalId))
        {
            return ApiResponseFactory.FailureResult<object>(ApiResponseCode.HOSPITAL_CLAIM_INVALID, requestId);
        }

        return await _proxyService.ProxyToAIServiceAsync(
            endpoint: "/api/v1.0/ai/assist/suggest",
            httpMethod: HttpMethod.Post,
            requestBody: requestBody,
            additionalHeaders: BuildAiHeaders(hospitalId),
            cancellationToken: cancellationToken);
    }

    [HttpPost("actions/preview")]
    public async Task<IActionResult> PreviewAction(
        [FromBody] object requestBody,
        CancellationToken cancellationToken)
    {
        var requestId = HttpContext.TraceIdentifier;
        if (!_featurePolicy.CanUseAction())
        {
            return ApiResponseFactory.FailureResult<object>(ApiResponseCode.FORBIDDEN, requestId);
        }

        if (!TryGetHospitalId(out var hospitalId))
        {
            return ApiResponseFactory.FailureResult<object>(ApiResponseCode.HOSPITAL_CLAIM_INVALID, requestId);
        }

        return await _proxyService.ProxyToAIServiceAsync(
            endpoint: "/api/v1.0/ai/actions/preview",
            httpMethod: HttpMethod.Post,
            requestBody: requestBody,
            additionalHeaders: BuildAiHeaders(hospitalId),
            cancellationToken: cancellationToken);
    }

    [HttpPost("actions/execute")]
    public async Task<IActionResult> ExecuteAction(
        [FromBody] object requestBody,
        CancellationToken cancellationToken)
    {
        var requestId = HttpContext.TraceIdentifier;
        if (!_featurePolicy.CanUseAction())
        {
            return ApiResponseFactory.FailureResult<object>(ApiResponseCode.FORBIDDEN, requestId);
        }

        if (!TryGetHospitalId(out var hospitalId))
        {
            return ApiResponseFactory.FailureResult<object>(ApiResponseCode.HOSPITAL_CLAIM_INVALID, requestId);
        }

        return await _proxyService.ProxyToAIServiceAsync(
            endpoint: "/api/v1.0/ai/actions/execute",
            httpMethod: HttpMethod.Post,
            requestBody: requestBody,
            additionalHeaders: BuildAiHeaders(hospitalId),
            cancellationToken: cancellationToken);
    }

    [HttpPost("workflows/start")]
    public async Task<IActionResult> StartWorkflow(
        [FromBody] object requestBody,
        CancellationToken cancellationToken)
    {
        var requestId = HttpContext.TraceIdentifier;
        if (!_featurePolicy.CanUseAutonomousWorkflows())
        {
            return ApiResponseFactory.FailureResult<object>(ApiResponseCode.FORBIDDEN, requestId);
        }
        if (!TryGetHospitalId(out var hospitalId))
        {
            return ApiResponseFactory.FailureResult<object>(ApiResponseCode.HOSPITAL_CLAIM_INVALID, requestId);
        }

        return await _proxyService.ProxyToAIServiceAsync(
            endpoint: "/api/v1.0/ai/workflows/start",
            httpMethod: HttpMethod.Post,
            requestBody: requestBody,
            additionalHeaders: BuildAiHeaders(hospitalId),
            cancellationToken: cancellationToken);
    }

    [HttpPost("workflows/{executionId}/approve-step")]
    public async Task<IActionResult> ApproveWorkflowStep(
        string executionId,
        [FromBody] object requestBody,
        CancellationToken cancellationToken)
    {
        var requestId = HttpContext.TraceIdentifier;
        if (!_featurePolicy.CanUseAction())
        {
            return ApiResponseFactory.FailureResult<object>(ApiResponseCode.FORBIDDEN, requestId);
        }
        if (!TryGetHospitalId(out var hospitalId))
        {
            return ApiResponseFactory.FailureResult<object>(ApiResponseCode.HOSPITAL_CLAIM_INVALID, requestId);
        }

        return await _proxyService.ProxyToAIServiceAsync(
            endpoint: $"/api/v1.0/ai/workflows/{executionId}/approve-step",
            httpMethod: HttpMethod.Post,
            requestBody: requestBody,
            additionalHeaders: BuildAiHeaders(hospitalId),
            cancellationToken: cancellationToken);
    }

    [HttpPost("workflows/{executionId}/resume")]
    public async Task<IActionResult> ResumeWorkflow(string executionId, CancellationToken cancellationToken)
    {
        var requestId = HttpContext.TraceIdentifier;
        if (!_featurePolicy.CanUseAction())
        {
            return ApiResponseFactory.FailureResult<object>(ApiResponseCode.FORBIDDEN, requestId);
        }
        if (!TryGetHospitalId(out var hospitalId))
        {
            return ApiResponseFactory.FailureResult<object>(ApiResponseCode.HOSPITAL_CLAIM_INVALID, requestId);
        }

        return await _proxyService.ProxyToAIServiceAsync(
            endpoint: $"/api/v1.0/ai/workflows/{executionId}/resume",
            httpMethod: HttpMethod.Post,
            requestBody: null,
            additionalHeaders: BuildAiHeaders(hospitalId),
            cancellationToken: cancellationToken);
    }

    [HttpPost("workflows/{executionId}/reject-step")]
    public async Task<IActionResult> RejectWorkflowStep(
        string executionId,
        [FromBody] object requestBody,
        CancellationToken cancellationToken)
    {
        var requestId = HttpContext.TraceIdentifier;
        if (!_featurePolicy.CanUseAction())
        {
            return ApiResponseFactory.FailureResult<object>(ApiResponseCode.FORBIDDEN, requestId);
        }
        if (!TryGetHospitalId(out var hospitalId))
        {
            return ApiResponseFactory.FailureResult<object>(ApiResponseCode.HOSPITAL_CLAIM_INVALID, requestId);
        }

        return await _proxyService.ProxyToAIServiceAsync(
            endpoint: $"/api/v1.0/ai/workflows/{executionId}/reject-step",
            httpMethod: HttpMethod.Post,
            requestBody: requestBody,
            additionalHeaders: BuildAiHeaders(hospitalId),
            cancellationToken: cancellationToken);
    }

    [HttpPost("workflows/{executionId}/cancel")]
    public async Task<IActionResult> CancelWorkflow(
        string executionId,
        [FromBody] object requestBody,
        CancellationToken cancellationToken)
    {
        var requestId = HttpContext.TraceIdentifier;
        if (!_featurePolicy.CanUseAction())
        {
            return ApiResponseFactory.FailureResult<object>(ApiResponseCode.FORBIDDEN, requestId);
        }
        if (!TryGetHospitalId(out var hospitalId))
        {
            return ApiResponseFactory.FailureResult<object>(ApiResponseCode.HOSPITAL_CLAIM_INVALID, requestId);
        }

        return await _proxyService.ProxyToAIServiceAsync(
            endpoint: $"/api/v1.0/ai/workflows/{executionId}/cancel",
            httpMethod: HttpMethod.Post,
            requestBody: requestBody,
            additionalHeaders: BuildAiHeaders(hospitalId),
            cancellationToken: cancellationToken);
    }

    [HttpGet("workflows/{executionId}")]
    public async Task<IActionResult> GetWorkflow(string executionId, CancellationToken cancellationToken)
    {
        var requestId = HttpContext.TraceIdentifier;
        if (!_featurePolicy.CanUseAutonomousWorkflows())
        {
            return ApiResponseFactory.FailureResult<object>(ApiResponseCode.FORBIDDEN, requestId);
        }
        if (!TryGetHospitalId(out var hospitalId))
        {
            return ApiResponseFactory.FailureResult<object>(ApiResponseCode.HOSPITAL_CLAIM_INVALID, requestId);
        }

        return await _proxyService.ProxyToAIServiceAsync(
            endpoint: $"/api/v1.0/ai/workflows/{executionId}",
            httpMethod: HttpMethod.Get,
            requestBody: null,
            additionalHeaders: BuildAiHeaders(hospitalId),
            cancellationToken: cancellationToken);
    }

    [HttpGet("workflows")]
    public async Task<IActionResult> ListWorkflows([FromQuery] string? status, CancellationToken cancellationToken)
    {
        var requestId = HttpContext.TraceIdentifier;
        if (!_featurePolicy.CanUseAutonomousWorkflows())
        {
            return ApiResponseFactory.FailureResult<object>(ApiResponseCode.FORBIDDEN, requestId);
        }
        if (!TryGetHospitalId(out var hospitalId))
        {
            return ApiResponseFactory.FailureResult<object>(ApiResponseCode.HOSPITAL_CLAIM_INVALID, requestId);
        }

        var endpoint = string.IsNullOrWhiteSpace(status)
            ? "/api/v1.0/ai/workflows"
            : $"/api/v1.0/ai/workflows?status={Uri.EscapeDataString(status)}";

        return await _proxyService.ProxyToAIServiceAsync(
            endpoint: endpoint,
            httpMethod: HttpMethod.Get,
            requestBody: null,
            additionalHeaders: BuildAiHeaders(hospitalId),
            cancellationToken: cancellationToken);
    }

    [HttpPost("workflows/trigger-from-event")]
    public async Task<IActionResult> TriggerFromEvent(
        [FromBody] object requestBody,
        CancellationToken cancellationToken)
    {
        var requestId = HttpContext.TraceIdentifier;
        if (!_featurePolicy.CanUseEventDrivenAutonomy())
        {
            return ApiResponseFactory.FailureResult<object>(ApiResponseCode.FORBIDDEN, requestId);
        }
        if (!TryGetHospitalId(out var hospitalId))
        {
            return ApiResponseFactory.FailureResult<object>(ApiResponseCode.HOSPITAL_CLAIM_INVALID, requestId);
        }

        return await _proxyService.ProxyToAIServiceAsync(
            endpoint: "/api/v1.0/ai/workflows/trigger-from-event",
            httpMethod: HttpMethod.Post,
            requestBody: requestBody,
            additionalHeaders: BuildAiHeaders(hospitalId),
            cancellationToken: cancellationToken);
    }

    /// <summary>Phase 5 — AIL knowledge search (keyword | semantic | hybrid).</summary>
    [HttpPost("knowledge/search")]
    public async Task<IActionResult> KnowledgeSearch(
        [FromBody] object requestBody,
        CancellationToken cancellationToken)
    {
        var requestId = HttpContext.TraceIdentifier;
        if (!_featurePolicy.CanUseRead())
        {
            return ApiResponseFactory.FailureResult<object>(ApiResponseCode.FORBIDDEN, requestId);
        }

        if (!TryGetHospitalId(out var hospitalId))
        {
            return ApiResponseFactory.FailureResult<object>(ApiResponseCode.HOSPITAL_CLAIM_INVALID, requestId);
        }

        return await _proxyService.ProxyToAIServiceAsync(
            endpoint: "/api/v1.0/ai/knowledge/search",
            httpMethod: HttpMethod.Post,
            requestBody: requestBody,
            additionalHeaders: BuildAiHeaders(hospitalId),
            cancellationToken: cancellationToken);
    }

    /// <summary>Phase 5.3 — evidence reviewer (advisory; never auto-approves).</summary>
    [HttpPost("evidence/review")]
    public async Task<IActionResult> ReviewEvidence(
        [FromBody] object requestBody,
        CancellationToken cancellationToken)
    {
        var requestId = HttpContext.TraceIdentifier;
        if (!_featurePolicy.CanUseAssist())
        {
            return ApiResponseFactory.FailureResult<object>(ApiResponseCode.FORBIDDEN, requestId);
        }

        if (!TryGetHospitalId(out var hospitalId))
        {
            return ApiResponseFactory.FailureResult<object>(ApiResponseCode.HOSPITAL_CLAIM_INVALID, requestId);
        }

        return await _proxyService.ProxyToAIServiceAsync(
            endpoint: "/api/v1.0/ai/evidence/review",
            httpMethod: HttpMethod.Post,
            requestBody: requestBody,
            additionalHeaders: BuildAiHeaders(hospitalId),
            cancellationToken: cancellationToken);
    }

    /// <summary>Phase 5.4 — CAPA corrective/preventive suggestions (proposals only).</summary>
    [HttpPost("advisor/capa-suggest")]
    public async Task<IActionResult> SuggestCapa(
        [FromBody] object requestBody,
        CancellationToken cancellationToken)
    {
        var requestId = HttpContext.TraceIdentifier;
        if (!_featurePolicy.CanUseAssist())
        {
            return ApiResponseFactory.FailureResult<object>(ApiResponseCode.FORBIDDEN, requestId);
        }

        if (!TryGetHospitalId(out var hospitalId))
        {
            return ApiResponseFactory.FailureResult<object>(ApiResponseCode.HOSPITAL_CLAIM_INVALID, requestId);
        }

        return await _proxyService.ProxyToAIServiceAsync(
            endpoint: "/api/v1.0/ai/advisor/capa-suggest",
            httpMethod: HttpMethod.Post,
            requestBody: requestBody,
            additionalHeaders: BuildAiHeaders(hospitalId),
            cancellationToken: cancellationToken);
    }

    /// <summary>Phase 5.5 — risk intelligence analyze.</summary>
    [HttpGet("risk-intelligence/analyze")]
    [HttpPost("risk-intelligence/analyze")]
    public async Task<IActionResult> AnalyzeRiskIntelligence(
        [FromBody] object? requestBody,
        CancellationToken cancellationToken)
    {
        var requestId = HttpContext.TraceIdentifier;
        if (!_featurePolicy.CanUseRead())
        {
            return ApiResponseFactory.FailureResult<object>(ApiResponseCode.FORBIDDEN, requestId);
        }

        if (!TryGetHospitalId(out var hospitalId))
        {
            return ApiResponseFactory.FailureResult<object>(ApiResponseCode.HOSPITAL_CLAIM_INVALID, requestId);
        }

        return await _proxyService.ProxyToAIServiceAsync(
            endpoint: "/api/v1.0/ai/risk-intelligence/analyze",
            httpMethod: Request.Method.Equals("GET", StringComparison.OrdinalIgnoreCase) ? HttpMethod.Get : HttpMethod.Post,
            requestBody: requestBody,
            additionalHeaders: BuildAiHeaders(hospitalId),
            cancellationToken: cancellationToken);
    }

    /// <summary>Phase 5.7 — predictive readiness.</summary>
    [HttpGet("predict/readiness")]
    public async Task<IActionResult> PredictReadiness([FromQuery] string? horizon, CancellationToken cancellationToken)
    {
        return await ProxyPredictAsync("readiness", horizon, cancellationToken);
    }

    [HttpGet("predict/delays")]
    public async Task<IActionResult> PredictDelays([FromQuery] string? horizon, CancellationToken cancellationToken)
    {
        return await ProxyPredictAsync("delays", horizon, cancellationToken);
    }

    [HttpGet("predict/overview")]
    public async Task<IActionResult> PredictOverview([FromQuery] string? horizon, CancellationToken cancellationToken)
    {
        return await ProxyPredictAsync("overview", horizon, cancellationToken);
    }

    /// <summary>Phase 5.6 — AI report generation (markdown).</summary>
    [HttpPost("reports/generate")]
    public async Task<IActionResult> GenerateReport(
        [FromBody] object requestBody,
        CancellationToken cancellationToken)
    {
        var requestId = HttpContext.TraceIdentifier;
        if (!_featurePolicy.CanUseRead())
        {
            return ApiResponseFactory.FailureResult<object>(ApiResponseCode.FORBIDDEN, requestId);
        }

        if (!TryGetHospitalId(out var hospitalId))
        {
            return ApiResponseFactory.FailureResult<object>(ApiResponseCode.HOSPITAL_CLAIM_INVALID, requestId);
        }

        return await _proxyService.ProxyToAIServiceAsync(
            endpoint: "/api/v1.0/ai/reports/generate",
            httpMethod: HttpMethod.Post,
            requestBody: requestBody,
            additionalHeaders: BuildAiHeaders(hospitalId),
            cancellationToken: cancellationToken);
    }

    /// <summary>Phase 5.10 — AI governance in-process metrics snapshot.</summary>
    [HttpGet("governance/metrics")]
    public async Task<IActionResult> GetGovernanceMetrics(CancellationToken cancellationToken)
    {
        var requestId = HttpContext.TraceIdentifier;
        if (!_featurePolicy.CanUseRead())
        {
            return ApiResponseFactory.FailureResult<object>(ApiResponseCode.FORBIDDEN, requestId);
        }

        if (!TryGetHospitalId(out var hospitalId))
        {
            return ApiResponseFactory.FailureResult<object>(ApiResponseCode.HOSPITAL_CLAIM_INVALID, requestId);
        }

        return await _proxyService.ProxyToAIServiceAsync(
            endpoint: "/api/v1.0/ai/governance/metrics",
            httpMethod: HttpMethod.Get,
            requestBody: null,
            additionalHeaders: BuildAiHeaders(hospitalId),
            cancellationToken: cancellationToken);
    }

    /// <summary>Phase 5 ACE — list advisory insights (Read flag; Proactive not required).</summary>
    [HttpGet("ace/insights")]
    public async Task<IActionResult> ListAceInsights(
        [FromQuery] string? status,
        [FromQuery] int take = 20,
        CancellationToken cancellationToken = default)
    {
        var requestId = HttpContext.TraceIdentifier;
        if (!_featurePolicy.CanUseRead())
        {
            return ApiResponseFactory.FailureResult<object>(ApiResponseCode.FORBIDDEN, requestId);
        }

        if (!TryGetHospitalId(out var hospitalId))
        {
            return ApiResponseFactory.FailureResult<object>(ApiResponseCode.HOSPITAL_CLAIM_INVALID, requestId);
        }

        var qs = $"take={take}";
        if (!string.IsNullOrWhiteSpace(status))
        {
            qs += $"&status={Uri.EscapeDataString(status)}";
        }

        return await _proxyService.ProxyToAIServiceAsync(
            endpoint: $"/api/v1.0/ai/ace/insights?{qs}",
            httpMethod: HttpMethod.Get,
            requestBody: null,
            additionalHeaders: BuildAiHeaders(hospitalId),
            cancellationToken: cancellationToken);
    }

    [HttpPost("ace/insights/{insightId}/acknowledge")]
    public async Task<IActionResult> AcknowledgeAceInsight(
        string insightId,
        [FromBody] object? requestBody,
        CancellationToken cancellationToken)
    {
        var requestId = HttpContext.TraceIdentifier;
        if (!_featurePolicy.CanUseRead())
        {
            return ApiResponseFactory.FailureResult<object>(ApiResponseCode.FORBIDDEN, requestId);
        }

        if (!TryGetHospitalId(out var hospitalId))
        {
            return ApiResponseFactory.FailureResult<object>(ApiResponseCode.HOSPITAL_CLAIM_INVALID, requestId);
        }

        return await _proxyService.ProxyToAIServiceAsync(
            endpoint: $"/api/v1.0/ai/ace/insights/{Uri.EscapeDataString(insightId)}/acknowledge",
            httpMethod: HttpMethod.Post,
            requestBody: requestBody ?? new { },
            additionalHeaders: BuildAiHeaders(hospitalId),
            cancellationToken: cancellationToken);
    }

    [HttpGet("proactive/suggestions/{entityType}/{entityId}")]
    public async Task<IActionResult> GetProactiveSuggestions(
        string entityType,
        string entityId,
        CancellationToken cancellationToken)
    {
        var requestId = HttpContext.TraceIdentifier;
        if (!_featurePolicy.CanUseRead())
        {
            return ApiResponseFactory.FailureResult<object>(ApiResponseCode.FORBIDDEN, requestId);
        }

        if (!TryGetHospitalId(out var hospitalId))
        {
            return ApiResponseFactory.FailureResult<object>(ApiResponseCode.HOSPITAL_CLAIM_INVALID, requestId);
        }

        return await _proxyService.ProxyToAIServiceAsync(
            endpoint: $"/api/v1.0/ai/proactive/suggestions/{Uri.EscapeDataString(entityType)}/{Uri.EscapeDataString(entityId)}",
            httpMethod: HttpMethod.Get,
            requestBody: null,
            additionalHeaders: BuildAiHeaders(hospitalId),
            cancellationToken: cancellationToken);
    }

    private async Task<IActionResult> ProxyPredictAsync(string path, string? horizon, CancellationToken cancellationToken)
    {
        var requestId = HttpContext.TraceIdentifier;
        if (!_featurePolicy.CanUseRead())
        {
            return ApiResponseFactory.FailureResult<object>(ApiResponseCode.FORBIDDEN, requestId);
        }

        if (!TryGetHospitalId(out var hospitalId))
        {
            return ApiResponseFactory.FailureResult<object>(ApiResponseCode.HOSPITAL_CLAIM_INVALID, requestId);
        }

        var qs = string.IsNullOrWhiteSpace(horizon) ? string.Empty : $"?horizon={Uri.EscapeDataString(horizon)}";
        if (qs.Length == 0)
            qs = $"?hospitalId={hospitalId}";
        else
            qs += $"&hospitalId={hospitalId}";

        return await _proxyService.ProxyToAIServiceAsync(
            endpoint: $"/api/v1.0/ai/predict/{path}{qs}",
            httpMethod: HttpMethod.Get,
            requestBody: null,
            additionalHeaders: BuildAiHeaders(hospitalId),
            cancellationToken: cancellationToken);
    }

    private bool TryGetHospitalId(out Guid hospitalId)
    {
        hospitalId = Guid.Empty;
        var hospitalIdClaim = User.FindFirst("hospital_id")?.Value;
        if (string.IsNullOrWhiteSpace(hospitalIdClaim))
        {
            _logger.LogWarning("Missing hospital_id claim in JWT token");
            return false;
        }

        if (!Guid.TryParse(hospitalIdClaim, out hospitalId))
        {
            _logger.LogWarning("Invalid hospital_id claim format");
            return false;
        }

        return true;
    }

    private Dictionary<string, string> BuildAiHeaders(Guid hospitalId)
    {
        return new Dictionary<string, string>
        {
            ["X-Hospital-Id"] = hospitalId.ToString(),
            ["X-Tenant-Id"] = hospitalId.ToString()
        };
    }
}
