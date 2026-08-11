using ComplianceService.Application.Contracts;
using ComplianceService.Application.DTOs;
using ComplianceService.Application.Events;
using ComplianceService.Domain;
using ComplianceService.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Api;
using Shared.Infrastructure.DTO;

namespace ComplianceService.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/compliance")]
[Authorize(Policy = Shared.Infrastructure.Auth.AuthConstants.Policies.ComplianceScope)]
public class EvidenceController : ControllerBase
{
    private readonly IEvidenceRegistrationService _evidenceRegistration;
    private readonly IComplianceTaskRepository _taskRepository;
    private readonly ILogger<EvidenceController> _logger;

    public EvidenceController(
        IEvidenceRegistrationService evidenceRegistration,
        IComplianceTaskRepository taskRepository,
        ILogger<EvidenceController> logger)
    {
        _evidenceRegistration = evidenceRegistration;
        _taskRepository = taskRepository;
        _logger = logger;
    }

    [HttpPost("tasks/{taskId}/evidence")]
    public async Task<ActionResult<ApiResponse<object>>> Create(Guid taskId, [FromBody] CreateEvidenceRequest request, CancellationToken ct)
    {
        var requestId = HttpContext.TraceIdentifier;

        var task = await _taskRepository.GetAsync(taskId);
        if (task == null)
        {
            var response = ApiResponseFactory.Failure<object>(
                ApiResponseCode.RESOURCE_NOT_FOUND,
                requestId);

            var meta = ApiResponseCodes.Registry[ApiResponseCode.RESOURCE_NOT_FOUND];
            return StatusCode(meta.HttpStatus, response);
        }

        var userId = Guid.TryParse(User.FindFirst("sub")?.Value, out var uid) ? uid : Guid.Empty;
        var tenantId = Guid.TryParse(User.FindFirst("organization_id")?.Value, out var org) ? org : Guid.Empty;

        var result = await _evidenceRegistration.RegisterFromUrlAsync(
            new EvidenceRegistrationUrlRequest
            {
                Context = EvidenceRegistrationContext.ForTask(taskId, tenantId, userId),
                EvidenceType = request.EvidenceType ?? string.Empty,
                FileUrl = request.FileUrl ?? string.Empty,
                Metadata = request.Metadata
            },
            ct);

        _logger.LogInformation("Created evidence {EvidenceId} for task {TaskId}", result.EvidenceId, taskId);

        var data = new { EvidenceId = result.EvidenceId };
        var successResponse = ApiResponseFactory.Success(ApiResponseCode.EVIDENCE_UPLOADED, (object)data, requestId);
        var successMeta = ApiResponseCodes.Registry[ApiResponseCode.EVIDENCE_UPLOADED];

        return StatusCode(successMeta.HttpStatus, successResponse);
    }
}

public record CreateEvidenceRequest
{
    public string? EvidenceType { get; init; }
    public string? FileUrl { get; init; }
    public string? Metadata { get; init; }
}
