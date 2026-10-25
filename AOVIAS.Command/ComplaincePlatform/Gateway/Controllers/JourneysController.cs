using System.Text.Json;
using Gateway.Application.Commands;
using Gateway.Application.Queries;
using Gateway.DTOs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Infrastructure.Api;
using Shared.Infrastructure.Auth;

namespace Gateway.Controllers;

/// <summary>
    /// Gateway controller for compliance journey lifecycle operations.
    /// All client calls for journey lifecycle terminate here; this controller
    /// proxies the requests to ComplianceService.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/journeys")]
[Authorize(Policy = AuthConstants.Policies.ComplianceScope)]
public class JourneysController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<JourneysController> _logger;

    public JourneysController(
        IMediator mediator,
        ILogger<JourneysController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new compliance journey for a hospital and framework.
    /// Proxies to ComplianceService.
    /// </summary>
    [HttpPost("/api/v{version:apiVersion}/hospitals/{hospitalId}/frameworks/{frameworkId}/journeys")]
    public async Task<IActionResult> CreateJourney(
        Guid hospitalId,
        Guid frameworkId,
        CancellationToken cancellationToken)
    {
        if (hospitalId == Guid.Empty)
        {
            return BadRequest(new { error = "HospitalId is required" });
        }

        if (frameworkId == Guid.Empty)
        {
            return BadRequest(new { error = "FrameworkId is required" });
        }

        var apiVersion = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";

        _logger.LogInformation(
            "POST /api/v{Version}/hospitals/{HospitalId}/frameworks/{FrameworkId}/journeys - Dispatching CreateJourneyCommand",
            apiVersion, hospitalId, frameworkId);

        var command = new CreateJourneyCommand(hospitalId, frameworkId, apiVersion);
        return await _mediator.Send(command, cancellationToken);
    }

    /// <summary>
    /// Gets detailed information about a compliance journey by journey ID or composite ID (hospitalId_frameworkId).
    /// Proxies to ComplianceService.
    /// </summary>
    [HttpGet("{journeyIdOrComposite}")]
    public async Task<IActionResult> GetJourneyDetails(
        string journeyIdOrComposite,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(journeyIdOrComposite))
        {
            return BadRequest(new { error = "JourneyId is required" });
        }

        // Check if it's a composite ID format (hospitalId_frameworkId)
        if (journeyIdOrComposite.Contains('_'))
        {
            var parts = journeyIdOrComposite.Split('_');
            if (parts.Length == 2 && 
                Guid.TryParse(parts[0], out var hospitalId) && 
                Guid.TryParse(parts[1], out var frameworkId))
            {
                _logger.LogInformation(
                    "GET /api/v1/journeys/{CompositeId} - Parsed as hospital {HospitalId} and framework {FrameworkId}",
                    journeyIdOrComposite, hospitalId, frameworkId);
                
                return await GetJourneyByHospitalAndFramework(hospitalId, frameworkId, cancellationToken);
            }
            else
            {
                return BadRequest(new { error = "Invalid composite ID format. Expected: hospitalId_frameworkId" });
            }
        }

        // Try to parse as a regular GUID
        if (!Guid.TryParse(journeyIdOrComposite, out var journeyId))
        {
            return BadRequest(new { error = "Invalid JourneyId format. Must be a GUID or composite ID (hospitalId_frameworkId)" });
        }

        if (journeyId == Guid.Empty)
        {
            return BadRequest(new { error = "JourneyId is required" });
        }

        var apiVersion = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";

        _logger.LogInformation(
            "GET /api/v{Version}/journeys/{JourneyId} - Dispatching GetJourneyDetailsQuery",
            apiVersion, journeyId);

        var query = new GetJourneyDetailsQuery(journeyId, apiVersion);
        return await _mediator.Send(query, cancellationToken);
    }

    /// <summary>
    /// Gets detailed information about a compliance journey by hospital ID and framework ID.
    /// Proxies to ComplianceService.
    /// </summary>
    [HttpGet("/api/v{version:apiVersion}/hospitals/{hospitalId:guid}/frameworks/{frameworkId:guid}/journey")]
    public async Task<IActionResult> GetJourneyByHospitalAndFramework(
        Guid hospitalId,
        Guid frameworkId,
        CancellationToken cancellationToken)
    {
        var apiVersion = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";

        _logger.LogInformation(
            "GET /api/v{Version}/hospitals/{HospitalId}/frameworks/{FrameworkId}/journey - Dispatching GetJourneyByHospitalFrameworkQuery",
            apiVersion, hospitalId, frameworkId);

        var query = new GetJourneyByHospitalFrameworkQuery(hospitalId, frameworkId, apiVersion);
        return await _mediator.Send(query, cancellationToken);
    }

    /// <summary>
    /// Gets valid next lifecycle states for a journey.
    /// Proxies to ComplianceService.
    /// </summary>
    [HttpGet("{journeyId:guid}/valid-transitions")]
    public async Task<IActionResult> GetValidTransitions(
        Guid journeyId,
        CancellationToken cancellationToken)
    {
        if (journeyId == Guid.Empty)
        {
            return BadRequest(new { error = "JourneyId is required" });
        }

        var apiVersion = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";

        _logger.LogInformation(
            "GET /api/v{Version}/journeys/{JourneyId}/valid-transitions - Dispatching GetValidTransitionsQuery",
            apiVersion, journeyId);

        var query = new GetValidTransitionsQuery(journeyId, apiVersion);
        return await _mediator.Send(query, cancellationToken);
    }

    /// <summary>
    /// Optional maintenance: proxies to ComplianceService task generation. Evidence upload no longer depends on this call.
    /// </summary>
    [Obsolete("Evidence and task assignment are ensured on upload; use only for legacy maintenance if needed.")]
    [HttpPost("{journeyId:guid}/compliance-tasks/sync")]
    public Task<IActionResult> SyncComplianceTasks(Guid journeyId, CancellationToken cancellationToken)
    {
        if (journeyId == Guid.Empty)
        {
            return Task.FromResult<IActionResult>(BadRequest(new { error = "JourneyId is required" }));
        }

        var apiVersion = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";
        return _mediator.Send(new SyncComplianceTasksCommand(journeyId, apiVersion), cancellationToken);
    }

    /// <summary>
    /// Transitions a compliance journey to a new lifecycle state.
    /// Proxies to ComplianceService.
    /// </summary>
    [HttpPost("{journeyId:guid}/transition")]
    public async Task<IActionResult> TransitionJourney(
        Guid journeyId,
        [FromBody] object requestBody,
        CancellationToken cancellationToken)
    {
        if (journeyId == Guid.Empty)
        {
            return BadRequest(new { error = "JourneyId is required" });
        }

        var apiVersion = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";

        _logger.LogInformation(
            "POST /api/v1/journeys/{JourneyId}/transition - Dispatching TransitionJourneyCommand",
            journeyId);

        var command = new TransitionJourneyCommand(journeyId, requestBody, apiVersion);
        return await _mediator.Send(command, cancellationToken);
    }

    /// <summary>
    /// Alias endpoint for state transitions using plural route segment.
    /// Proxies to the same TransitionJourneyCommand.
    /// </summary>
    [HttpPost("{journeyId:guid}/transitions")]
    public async Task<IActionResult> TransitionJourneyPlural(
        Guid journeyId,
        [FromBody] object requestBody,
        CancellationToken cancellationToken)
    {
        if (journeyId == Guid.Empty)
        {
            return BadRequest(new { error = "JourneyId is required" });
        }

        var apiVersion = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";

        _logger.LogInformation(
            "POST /api/v1/journeys/{JourneyId}/transitions - Dispatching TransitionJourneyCommand",
            journeyId);

        var command = new TransitionJourneyCommand(journeyId, requestBody, apiVersion);
        return await _mediator.Send(command, cancellationToken);
    }

    /// <summary>
    /// Semantic endpoint to approve a compliance journey.
    /// Internally routes to the core transition endpoint with targetState=Approved.
    /// </summary>
    [HttpPost("{journeyId:guid}/approve")]
    public async Task<IActionResult> ApproveJourney(
        Guid journeyId,
        [FromBody] ApproveJourneyRequest request,
        CancellationToken cancellationToken)
    {
        if (journeyId == Guid.Empty)
        {
            return BadRequest(new { error = "JourneyId is required" });
        }

        // Map to core transition contract
        var transitionBody = new
        {
            targetStatus = "Approved",
            remarks = request?.Remarks,
            metadata = request?.Metadata ?? new Dictionary<string, object>()
        };

        var apiVersion = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";

        _logger.LogInformation(
            "POST /api/v1/journeys/{JourneyId}/approve - Dispatching TransitionJourneyCommand with targetStatus=Approved",
            journeyId);

        var command = new TransitionJourneyCommand(journeyId, transitionBody, apiVersion);
        return await _mediator.Send(command, cancellationToken);
    }

    /// <summary>
    /// Semantic endpoint to reject a compliance journey.
    /// Internally routes to the core transition endpoint with targetStatus=Rejected.
    /// </summary>
    [HttpPost("{journeyId:guid}/reject")]
    public async Task<IActionResult> RejectJourney(
        Guid journeyId,
        [FromBody] RejectJourneyRequest request,
        CancellationToken cancellationToken)
    {
        if (journeyId == Guid.Empty)
        {
            return BadRequest(new { error = "JourneyId is required" });
        }

        if (string.IsNullOrWhiteSpace(request?.Remarks))
        {
            return BadRequest(new { error = "Remarks are required when rejecting a journey." });
        }

        var transitionBody = new
        {
            targetStatus = "Rejected",
            remarks = request.Remarks,
            metadata = request.Metadata ?? new Dictionary<string, object>()
        };

        var apiVersion = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";

        _logger.LogInformation(
            "POST /api/v1/journeys/{JourneyId}/reject - Dispatching TransitionJourneyCommand with targetStatus=Rejected",
            journeyId);

        var command = new TransitionJourneyCommand(journeyId, transitionBody, apiVersion);
        return await _mediator.Send(command, cancellationToken);
    }

    /// <summary>
    /// Semantic endpoint to suspend a compliance journey.
    /// Internally routes to the core transition endpoint with targetStatus=Suspended.
    /// Domain role rules enforce that only Regulator can suspend from Approved.
    /// </summary>
    [HttpPost("{journeyId:guid}/suspend")]
    public async Task<IActionResult> SuspendJourney(
        Guid journeyId,
        [FromBody] SuspendJourneyRequest request,
        CancellationToken cancellationToken)
    {
        if (journeyId == Guid.Empty)
        {
            return BadRequest(new { error = "JourneyId is required" });
        }

        var transitionBody = new
        {
            targetStatus = "Suspended",
            remarks = request?.Remarks,
            metadata = request?.Metadata ?? new Dictionary<string, object>()
        };

        var apiVersion = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";

        _logger.LogInformation(
            "POST /api/v1/journeys/{JourneyId}/suspend - Dispatching TransitionJourneyCommand with targetStatus=Suspended",
            journeyId);

        var command = new TransitionJourneyCommand(journeyId, transitionBody, apiVersion);
        return await _mediator.Send(command, cancellationToken);
    }

    /// <summary>
    /// Semantic endpoint to revoke a compliance journey (high-severity suspension).
    /// Currently implemented as a suspension with a Revoked remark prefix.
    /// </summary>
    [HttpPost("{journeyId:guid}/revoke")]
    public async Task<IActionResult> RevokeJourney(
        Guid journeyId,
        [FromBody] RevokeJourneyRequest request,
        CancellationToken cancellationToken)
    {
        if (journeyId == Guid.Empty)
        {
            return BadRequest(new { error = "JourneyId is required" });
        }

        var remark = string.IsNullOrWhiteSpace(request?.Remarks)
            ? "Revoked by regulator"
            : $"REVOKED: {request.Remarks}";

        var transitionBody = new
        {
            targetStatus = "Suspended",
            remarks = remark,
            metadata = request?.Metadata ?? new Dictionary<string, object>()
        };

        var apiVersion = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";

        _logger.LogInformation(
            "POST /api/v1/journeys/{JourneyId}/revoke - Dispatching TransitionJourneyCommand with targetStatus=Suspended (revoked)",
            journeyId);

        var command = new TransitionJourneyCommand(journeyId, transitionBody, apiVersion);
        return await _mediator.Send(command, cancellationToken);
    }

    /// <summary>
    /// Semantic endpoint to lift suspension and reinstate a journey.
    /// Internally routes to the core transition endpoint with targetStatus=Approved.
    /// </summary>
    [HttpPost("{journeyId:guid}/reinstate")]
    public async Task<IActionResult> ReinstateJourney(
        Guid journeyId,
        [FromBody] ReinstateJourneyRequest request,
        CancellationToken cancellationToken)
    {
        if (journeyId == Guid.Empty)
        {
            return BadRequest(new { error = "JourneyId is required" });
        }

        var transitionBody = new
        {
            targetStatus = "Approved",
            remarks = request?.Remarks,
            metadata = request?.Metadata ?? new Dictionary<string, object>()
        };

        var apiVersion = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";

        _logger.LogInformation(
            "POST /api/v1/journeys/{JourneyId}/reinstate - Dispatching TransitionJourneyCommand with targetStatus=Approved",
            journeyId);

        var command = new TransitionJourneyCommand(journeyId, transitionBody, apiVersion);
        return await _mediator.Send(command, cancellationToken);
    }

    [HttpGet("{journeyId:guid}/workspace")]
    public Task<IActionResult> GetJourneyWorkspace(Guid journeyId, CancellationToken cancellationToken)
    {
        var apiVersion = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";
        var qs = Request.QueryString.HasValue ? Request.QueryString.Value : null;
        return _mediator.Send(new GetJourneyWorkspaceQuery(journeyId, apiVersion, qs), cancellationToken);
    }

    [HttpGet("{journeyId:guid}/session")]
    public Task<IActionResult> GetJourneySession(Guid journeyId, CancellationToken cancellationToken)
    {
        var apiVersion = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";
        var qs = Request.QueryString.HasValue ? Request.QueryString.Value : null;
        return _mediator.Send(new GetJourneySessionQuery(journeyId, apiVersion, qs), cancellationToken);
    }

    [HttpGet("{journeyId:guid}/dashboard")]
    public Task<IActionResult> GetJourneyDashboard(Guid journeyId, CancellationToken cancellationToken)
    {
        var apiVersion = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";
        var qs = Request.QueryString.HasValue ? Request.QueryString.Value : null;
        return _mediator.Send(new GetJourneyDashboardQuery(journeyId, apiVersion, qs), cancellationToken);
    }

    [HttpGet("{journeyId:guid}/chapters")]
    public Task<IActionResult> GetJourneyChaptersIndex(Guid journeyId, CancellationToken cancellationToken)
    {
        var apiVersion = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";
        var qs = Request.QueryString.HasValue ? Request.QueryString.Value : null;
        return _mediator.Send(new GetJourneyChaptersSplitQuery(journeyId, apiVersion, qs), cancellationToken);
    }

    [HttpGet("{journeyId:guid}/objective-elements/{objectiveElementId:guid}")]
    public Task<IActionResult> GetObjectiveElementDetail(
        Guid journeyId,
        Guid objectiveElementId,
        CancellationToken cancellationToken)
    {
        var apiVersion = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";
        var qs = Request.QueryString.HasValue ? Request.QueryString.Value : null;
        return _mediator.Send(
            new GetObjectiveElementDetailQuery(journeyId, objectiveElementId, apiVersion, qs),
            cancellationToken);
    }

    [HttpGet("{journeyId:guid}/objective-elements/{objectiveElementId:guid}/oe-execution-wizard")]
    public Task<IActionResult> GetOeExecutionWizard(
        Guid journeyId,
        Guid objectiveElementId,
        CancellationToken cancellationToken)
    {
        var apiVersion = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";
        return _mediator.Send(new GetOeExecutionWizardQuery(journeyId, objectiveElementId, apiVersion), cancellationToken);
    }

    [HttpPut("{journeyId:guid}/objective-elements/{objectiveElementId:guid}/oe-execution-wizard")]
    public Task<IActionResult> PutOeExecutionWizardDraft(
        Guid journeyId,
        Guid objectiveElementId,
        [FromBody] SaveObjectiveJourneyDraftRequest body,
        CancellationToken cancellationToken)
    {
        var apiVersion = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";
        return _mediator.Send(
            new SaveOeExecutionWizardDraftCommand(journeyId, objectiveElementId, body, apiVersion),
            cancellationToken);
    }

    [HttpPost("{journeyId:guid}/objective-elements/{objectiveElementId:guid}/oe-execution-wizard/complete")]
    public Task<IActionResult> PostOeExecutionWizardComplete(
        Guid journeyId,
        Guid objectiveElementId,
        [FromBody] CompleteObjectiveJourneyRequest body,
        CancellationToken cancellationToken)
    {
        var apiVersion = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";
        var qs = Request.QueryString.HasValue ? Request.QueryString.Value : null;
        return _mediator.Send(
            new CompleteOeExecutionWizardCommand(journeyId, objectiveElementId, body, apiVersion, qs),
            cancellationToken);
    }

    [HttpPatch("{journeyId:guid}/session/operational-mode")]
    public Task<IActionResult> PatchJourneyOperationalMode(
        Guid journeyId,
        [FromBody] object requestBody,
        CancellationToken cancellationToken)
    {
        var apiVersion = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";
        return _mediator.Send(new PatchJourneyOperationalModeCommand(journeyId, requestBody, apiVersion), cancellationToken);
    }

    [HttpGet("{journeyId:guid}/chapters/{chapterId:guid}/standards")]
    public Task<IActionResult> GetChapterStandards(
        Guid journeyId,
        Guid chapterId,
        CancellationToken cancellationToken)
    {
        var apiVersion = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";
        var qs = Request.QueryString.HasValue ? Request.QueryString.Value : null;
        return _mediator.Send(new GetChapterStandardsQuery(journeyId, chapterId, apiVersion, qs), cancellationToken);
    }

    [HttpPatch("{journeyId:guid}/objective-elements/{objectiveElementId:guid}")]
    public Task<IActionResult> PatchObjectiveElement(
        Guid journeyId,
        Guid objectiveElementId,
        [FromBody] object requestBody,
        CancellationToken cancellationToken)
    {
        var apiVersion = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";
        var qs = Request.QueryString.HasValue ? Request.QueryString.Value : null;
        return _mediator.Send(
            new PatchObjectiveElementCommand(journeyId, objectiveElementId, requestBody, apiVersion, qs),
            cancellationToken);
    }

    [HttpPost("{journeyId:guid}/objective-elements/{objectiveElementId:guid}/evidence/bulk")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(15 * 10_485_760)]
    public Task<IActionResult> PostObjectiveElementEvidenceBulkMultipart(
        Guid journeyId,
        Guid objectiveElementId,
        CancellationToken cancellationToken)
    {
        var apiVersion = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";
        return _mediator.Send(
            new PostObjectiveElementEvidenceBulkMultipartCommand(journeyId, objectiveElementId, apiVersion),
            cancellationToken);
    }

    [HttpGet("{journeyId:guid}/objective-elements/{objectiveElementId:guid}/evidence/{evidenceId:guid}")]
    public Task<IActionResult> GetObjectiveElementEvidenceDetail(
        Guid journeyId,
        Guid objectiveElementId,
        Guid evidenceId,
        CancellationToken cancellationToken)
    {
        var apiVersion = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";
        return _mediator.Send(
            new GetObjectiveElementEvidenceDetailQuery(journeyId, objectiveElementId, evidenceId, apiVersion),
            cancellationToken);
    }

    [HttpGet("{journeyId:guid}/objective-elements/{objectiveElementId:guid}/evidence/{evidenceId:guid}/download")]
    public Task<IActionResult> GetObjectiveElementEvidenceDownload(
        Guid journeyId,
        Guid objectiveElementId,
        Guid evidenceId,
        CancellationToken cancellationToken)
    {
        var apiVersion = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";
        return _mediator.Send(
            new GetObjectiveElementEvidenceDownloadQuery(journeyId, objectiveElementId, evidenceId, apiVersion),
            cancellationToken);
    }

    [Obsolete("Use POST /api/v1.0/evidence/{evidenceId}/approval. Retained for backward compatibility.")]
    [HttpPost("{journeyId:guid}/objective-elements/{objectiveElementId:guid}/evidence/{evidenceId:guid}/verify")]
    public Task<IActionResult> PostObjectiveElementEvidenceVerify(
        Guid journeyId,
        Guid objectiveElementId,
        Guid evidenceId,
        [FromBody] object requestBody,
        CancellationToken cancellationToken)
    {
        var apiVersion = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";
        return _mediator.Send(
            new PostObjectiveElementEvidenceVerifyCommand(journeyId, objectiveElementId, evidenceId, requestBody, apiVersion),
            cancellationToken);
    }

    [HttpPost("{journeyId:guid}/objective-elements/{objectiveElementId:guid}/evidence/{evidenceId:guid}/versions")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(10_485_760)]
    public Task<IActionResult> PostObjectiveElementEvidenceVersionMultipart(
        Guid journeyId,
        Guid objectiveElementId,
        Guid evidenceId,
        CancellationToken cancellationToken)
    {
        var apiVersion = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";
        return _mediator.Send(
            new PostObjectiveElementEvidenceVersionMultipartCommand(journeyId, objectiveElementId, evidenceId, apiVersion),
            cancellationToken);
    }

    [HttpGet("{journeyId:guid}/objective-elements/{objectiveElementId:guid}/evidence")]
    public Task<IActionResult> GetObjectiveElementEvidence(
        Guid journeyId,
        Guid objectiveElementId,
        CancellationToken cancellationToken)
    {
        var apiVersion = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";
        return _mediator.Send(
            new GetObjectiveElementEvidenceQuery(journeyId, objectiveElementId, apiVersion),
            cancellationToken);
    }

    /// <summary>
    /// Registers evidence (JSON URL reference) or uploads a file (multipart).
    /// Single action avoids <see cref="ConsumesAttribute"/> selection issues when clients omit or misstate Content-Type,
    /// which previously produced 400 <c>application/problem+json</c> from model binding instead of reaching ComplianceService.
    /// </summary>
    [HttpPost("{journeyId:guid}/objective-elements/{objectiveElementId:guid}/evidence")]
    [RequestSizeLimit(10_485_760)]
    public async Task<IActionResult> PostObjectiveElementEvidence(
        Guid journeyId,
        Guid objectiveElementId,
        CancellationToken cancellationToken)
    {
        var apiVersion = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";
        var traceId = HttpContext.TraceIdentifier;

        if (Request.HasFormContentType)
        {
            return await _mediator.Send(
                new PostObjectiveElementEvidenceMultipartCommand(journeyId, objectiveElementId, apiVersion),
                cancellationToken);
        }

        Request.EnableBuffering();
        Request.Body.Position = 0;
        using var reader = new StreamReader(Request.Body, leaveOpen: true);
        var json = await reader.ReadToEndAsync(cancellationToken);
        Request.Body.Position = 0;

        if (string.IsNullOrWhiteSpace(json))
        {
            return ApiResponseFactory.FailureResult<object>(ApiResponseCode.VALIDATION_FAILED, traceId);
        }

        object? body;
        try
        {
            body = JsonSerializer.Deserialize<JsonElement>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (JsonException)
        {
            return ApiResponseFactory.FailureResult<object>(ApiResponseCode.VALIDATION_FAILED, traceId);
        }

        return await _mediator.Send(
            new PostObjectiveElementEvidenceCommand(journeyId, objectiveElementId, body, apiVersion),
            cancellationToken);
    }

    [HttpDelete("{journeyId:guid}/objective-elements/{objectiveElementId:guid}/evidence/{evidenceId:guid}")]
    public Task<IActionResult> DeleteObjectiveElementEvidence(
        Guid journeyId,
        Guid objectiveElementId,
        Guid evidenceId,
        CancellationToken cancellationToken)
    {
        var apiVersion = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";
        return _mediator.Send(
            new DeleteObjectiveElementEvidenceCommand(journeyId, objectiveElementId, evidenceId, apiVersion),
            cancellationToken);
    }

    [HttpGet("{journeyId:guid}/objective-elements/{objectiveElementId:guid}/history")]
    public Task<IActionResult> GetObjectiveElementHistory(
        Guid journeyId,
        Guid objectiveElementId,
        CancellationToken cancellationToken)
    {
        var apiVersion = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";
        return _mediator.Send(
            new GetObjectiveElementHistoryQuery(journeyId, objectiveElementId, apiVersion),
            cancellationToken);
    }
}

public record ApproveJourneyRequest
{
    public string? Remarks { get; init; }
    public Dictionary<string, object>? Metadata { get; init; }
}

/// <summary>
/// Request payload for rejecting a journey.
/// </summary>
public record RejectJourneyRequest
{
    public string Remarks { get; init; } = string.Empty;
    public Dictionary<string, object>? Metadata { get; init; }
}

/// <summary>
/// Request payload for suspending a journey.
/// </summary>
public record SuspendJourneyRequest
{
    public string? Remarks { get; init; }
    public Dictionary<string, object>? Metadata { get; init; }
}

/// <summary>
/// Request payload for revoking a journey (high-severity suspension).
/// </summary>
public record RevokeJourneyRequest
{
    public string? Remarks { get; init; }
    public Dictionary<string, object>? Metadata { get; init; }
}

/// <summary>
/// Request payload for lifting suspension / reinstating a journey.
/// </summary>
public record ReinstateJourneyRequest
{
    public string? Remarks { get; init; }
    public Dictionary<string, object>? Metadata { get; init; }
}
