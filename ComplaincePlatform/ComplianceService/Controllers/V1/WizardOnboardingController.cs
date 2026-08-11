using ComplianceService.Application.DTOs;
using ComplianceService.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Infrastructure.Auth;
using Shared.Infrastructure.DTO;
using Shared.Infrastructure.Extensions;

namespace ComplianceService.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/onboarding/wizard")]
[Authorize(Policy = AuthConstants.Policies.ComplianceScope)]
public sealed class WizardOnboardingController : ControllerBase
{
    private readonly WizardOnboardingAssessmentService _service;
    private readonly ILogger<WizardOnboardingController> _logger;

    public WizardOnboardingController(
        WizardOnboardingAssessmentService service,
        ILogger<WizardOnboardingController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// Persists onboarding wizard assessment answers for a hospital.
    /// </summary>
    [HttpPost("assessments")]
    [ProducesResponseType(typeof(ApiResponse<WizardOnboardingAssessmentResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SaveAssessment(
        [FromBody] SaveWizardAssessmentRequestDto request,
        CancellationToken cancellationToken)
    {
        var traceId = HttpContext.TraceIdentifier;

        if (request.HospitalId == Guid.Empty || request.OrganizationId == Guid.Empty)
        {
            return this.BadRequestResponse("OrganizationId and HospitalId are required.");
        }

        _logger.LogInformation(
            "POST wizard assessment: HospitalId={HospitalId}, OrgId={OrganizationId}, TraceId={TraceId}",
            request.HospitalId, request.OrganizationId, traceId);

        var result = await _service.SaveAsync(request, cancellationToken);
        return this.OkResponse(result);
    }

    /// <summary>
    /// Returns the most recent wizard assessment for a hospital.
    /// </summary>
    [HttpGet("hospitals/{hospitalId:guid}/assessments/latest")]
    [ProducesResponseType(typeof(ApiResponse<WizardOnboardingAssessmentResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetLatestAssessment(
        Guid hospitalId,
        CancellationToken cancellationToken)
    {
        if (hospitalId == Guid.Empty)
        {
            return this.BadRequestResponse("HospitalId is required.");
        }

        var result = await _service.GetLatestForHospitalAsync(hospitalId, cancellationToken);
        if (result == null)
        {
            return NotFound();
        }

        return this.OkResponse(result);
    }
}
