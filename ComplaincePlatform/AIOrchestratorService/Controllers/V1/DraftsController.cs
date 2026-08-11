using AIOrchestratorService.Application.Services;
using AIOrchestratorService.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIOrchestratorService.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/drafts")]
[Authorize(Policy = "AiScope")]
public class DraftsController : ControllerBase
{
    private readonly IDraftGenerationService _draftGenerationService;
    private readonly DraftingRequestBuilder _requestBuilder;
    private readonly ILogger<DraftsController> _logger;

    public DraftsController(
        IDraftGenerationService draftGenerationService,
        DraftingRequestBuilder requestBuilder,
        ILogger<DraftsController> logger)
    {
        _draftGenerationService = draftGenerationService;
        _requestBuilder = requestBuilder;
        _logger = logger;
    }

    /// <summary>
    /// Generates an AI-assisted draft.
    /// </summary>
    [HttpPost("generate")]
    public async Task<IActionResult> GenerateDraft([FromBody] GenerateDraftRequest request, CancellationToken cancellationToken)
    {
        if (request == null)
        {
            return BadRequest(new { error = "Request is required" });
        }

        if (!Enum.IsDefined(typeof(DraftType), request.DraftType))
        {
            return BadRequest(new { error = "Invalid DraftType" });
        }

        if (request.SourceEntityId == Guid.Empty)
        {
            return BadRequest(new { error = "SourceEntityId is required" });
        }

        try
        {
            // Step 2: DraftingRequestBuilder builds context
            _requestBuilder
                .ForDraft(request.DraftType, request.SourceEntityId);

            if (request.ComplianceProgram != null)
            {
                _requestBuilder.WithComplianceProgram(request.ComplianceProgram);
            }

            if (request.Control != null)
            {
                _requestBuilder.WithControl(request.Control);
            }

            if (request.ComplianceTask != null)
            {
                _requestBuilder.WithComplianceTask(request.ComplianceTask);
            }

            if (request.Evidences != null && request.Evidences.Any())
            {
                _requestBuilder.WithEvidences(request.Evidences);
            }

            if (request.AuditFinding != null)
            {
                _requestBuilder.WithAuditFinding(request.AuditFinding);
            }

            var draftingRequest = _requestBuilder.Build();

            // Generate draft (Steps 3-5 handled by service)
            var generatedBy = request.GeneratedBy ?? "System";
            var draftArtifact = await _draftGenerationService.GenerateDraftAsync(
                draftingRequest,
                generatedBy,
                cancellationToken);

            var response = new GenerateDraftResponse
            {
                Success = true,
                DraftArtifactId = draftArtifact.Id,
                DraftType = draftArtifact.DraftType.ToString(),
                SourceEntityId = draftArtifact.SourceEntityId,
                Status = draftArtifact.Status.ToString(),
                GeneratedAt = draftArtifact.GeneratedAt,
                GeneratedBy = draftArtifact.GeneratedBy
            };

            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid request: {Message}", ex.Message);
            return BadRequest(new GenerateDraftResponse
            {
                Success = false,
                ErrorMessage = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Draft generation failed: {Message}", ex.Message);
            return BadRequest(new GenerateDraftResponse
            {
                Success = false,
                ErrorMessage = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating draft: {Message}", ex.Message);
            return StatusCode(500, new GenerateDraftResponse
            {
                Success = false,
                ErrorMessage = "An error occurred while generating the draft"
            });
        }
    }
}

/// <summary>
/// Request to generate a draft.
/// </summary>
public record GenerateDraftRequest
{
    public DraftType DraftType { get; init; }
    public Guid SourceEntityId { get; init; }
    public ComplianceProgramContext? ComplianceProgram { get; init; }
    public ControlContext? Control { get; init; }
    public ComplianceTaskContext? ComplianceTask { get; init; }
    public List<EvidenceContext>? Evidences { get; init; }
    public AuditFindingContext? AuditFinding { get; init; }
    public string? GeneratedBy { get; init; }
}

/// <summary>
/// Response from draft generation.
/// </summary>
public record GenerateDraftResponse
{
    public bool Success { get; init; }
    public Guid? DraftArtifactId { get; init; }
    public string? DraftType { get; init; }
    public Guid? SourceEntityId { get; init; }
    public string? Status { get; init; }
    public DateTime? GeneratedAt { get; init; }
    public string? GeneratedBy { get; init; }
    public string? ErrorMessage { get; init; }
}
