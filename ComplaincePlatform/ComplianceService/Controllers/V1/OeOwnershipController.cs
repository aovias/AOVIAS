using ComplianceService.Application.DTOs;
using ComplianceService.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Infrastructure.Auth;
using Shared.Infrastructure.DTO;
using Shared.Infrastructure.Extensions;

namespace ComplianceService.Controllers.V1;

/// <summary>
/// Hospital OE ownership matrix (department + primary owner + due date).
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/hospitals/{hospitalId:guid}/oe-ownership")]
[Authorize(Policy = AuthConstants.Policies.ComplianceScope)]
public sealed class OeOwnershipController : ControllerBase
{
    private readonly ObjectiveElementOwnershipAppService _service;
    private readonly ILogger<OeOwnershipController> _logger;

    public OeOwnershipController(
        ObjectiveElementOwnershipAppService service,
        ILogger<OeOwnershipController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ObjectiveElementOwnershipMatrixItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListMatrix(
        Guid hospitalId,
        [FromQuery] Guid? frameworkId = null,
        CancellationToken cancellationToken = default)
    {
        if (hospitalId == Guid.Empty)
        {
            return this.BadRequestResponse("HospitalId is required", "hospitalId", "HospitalId cannot be empty");
        }

        try
        {
            var rows = await _service.ListMatrixAsync(hospitalId, frameworkId, cancellationToken);
            return this.OkResponse(rows);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing OE ownership for hospital {HospitalId}", hospitalId);
            return this.BadRequestResponse("An error occurred while listing OE ownership");
        }
    }

    [HttpPut]
    [ProducesResponseType(typeof(ApiResponse<ObjectiveElementOwnershipMatrixItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Upsert(
        Guid hospitalId,
        [FromBody] UpsertObjectiveElementOwnershipRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (hospitalId == Guid.Empty)
        {
            return this.BadRequestResponse("HospitalId is required", "hospitalId", "HospitalId cannot be empty");
        }

        if (request == null)
        {
            return this.BadRequestResponse("Request body is required");
        }

        try
        {
            var result = await _service.UpsertAsync(hospitalId, request, cancellationToken);
            return this.OkResponse(result);
        }
        catch (ArgumentException ex)
        {
            return this.BadRequestResponse(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "OE ownership upsert rejected for hospital {HospitalId}", hospitalId);
            return this.BadRequestResponse(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error upserting OE ownership for hospital {HospitalId}", hospitalId);
            return this.BadRequestResponse("An error occurred while upserting OE ownership");
        }
    }

    [HttpDelete("{objectiveElementId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Clear(
        Guid hospitalId,
        Guid objectiveElementId,
        CancellationToken cancellationToken = default)
    {
        if (hospitalId == Guid.Empty)
        {
            return this.BadRequestResponse("HospitalId is required", "hospitalId", "HospitalId cannot be empty");
        }

        if (objectiveElementId == Guid.Empty)
        {
            return this.BadRequestResponse(
                "ObjectiveElementId is required",
                "objectiveElementId",
                "ObjectiveElementId cannot be empty");
        }

        try
        {
            await _service.ClearAsync(hospitalId, objectiveElementId, cancellationToken);
            return NoContent();
        }
        catch (ArgumentException ex)
        {
            return this.BadRequestResponse(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error clearing OE ownership for hospital {HospitalId} OE {ObjectiveElementId}",
                hospitalId,
                objectiveElementId);
            return this.BadRequestResponse("An error occurred while clearing OE ownership");
        }
    }
}
