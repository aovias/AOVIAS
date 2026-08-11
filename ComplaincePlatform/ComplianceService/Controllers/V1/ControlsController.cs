using ComplianceService.Application.Contracts;
using ComplianceService.Domain;
using ComplianceService.Infrastructure.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Infrastructure.Api;
using Shared.Infrastructure.DTO;
using Shared.Infrastructure.Services;

namespace ComplianceService.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/compliance")]
[Authorize(Policy = Shared.Infrastructure.Auth.AuthConstants.Policies.ComplianceScope)]
[ValidateHospitalId]
public class ControlsController : ControllerBase
{
    private readonly IControlRepository _repository;
    private readonly IComplianceProgramRepository _programRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IReasoningCapture _reasoningCapture;
    private readonly IUserContextService _userContext;
    private readonly ILogger<ControlsController> _logger;

    public ControlsController(
        IControlRepository repository,
        IComplianceProgramRepository programRepository,
        IUnitOfWork unitOfWork,
        IReasoningCapture reasoningCapture,
        IUserContextService userContext,
        ILogger<ControlsController> logger)
    {
        _repository = repository;
        _programRepository = programRepository;
        _unitOfWork = unitOfWork;
        _reasoningCapture = reasoningCapture;
        _userContext = userContext;
        _logger = logger;
    }

    [HttpPost("programs/{programId}/controls")]
    public async Task<ActionResult<ApiResponse<object>>> Load(Guid programId, [FromBody] LoadControlsRequest request, CancellationToken ct)
    {
        var traceId = HttpContext.TraceIdentifier;

        if (programId == Guid.Empty || string.IsNullOrWhiteSpace(request?.Regulation))
        {
            var result = ApiResponseFactory.Failure<object>(
                ApiResponseCode.VALIDATION_FAILED,
                traceId);

            var meta = ApiResponseCodes.Registry[ApiResponseCode.VALIDATION_FAILED];
            return StatusCode(meta.HttpStatus, result);
        }

        // Verify program exists
        var program = await _programRepository.GetAsync(programId);
        if (program == null)
        {
            var result = ApiResponseFactory.Failure<object>(
                ApiResponseCode.RESOURCE_NOT_FOUND,
                traceId);

            var meta = ApiResponseCodes.Registry[ApiResponseCode.RESOURCE_NOT_FOUND];
            return StatusCode(meta.HttpStatus, result);
        }

        // Placeholder: Create sample controls
        var controls = new List<Control>
        {
            new Control
            {
                Id = Guid.NewGuid(),
                ProgramId = programId,
                Code = $"{request.Regulation}-01",
                Description = $"Control 1 for {request.Regulation}",
                CreatedAt = DateTime.UtcNow
            },
            new Control
            {
                Id = Guid.NewGuid(),
                ProgramId = programId,
                Code = $"{request.Regulation}-02",
                Description = $"Control 2 for {request.Regulation}",
                CreatedAt = DateTime.UtcNow
            }
        };

        foreach (var control in controls)
        {
            // Attach explainability metadata
            var explainability = new ExplainabilityDto
            {
                Reasoning = $"Control {control.Code} was loaded as part of the compliance program setup for {request.Regulation} regulation. These controls are system-generated placeholders created to assist in establishing a basic control structure for the compliance program. The controls are based on the regulation identifier provided ({request.Regulation}) and are intended to serve as a starting point. These placeholder controls require human review, validation, and customization to ensure they accurately reflect the specific regulatory requirements and are appropriate for the organization's context.",
                RulesReferenced = new List<string> { request.Regulation, control.Code, $"Program:{programId}" },
                ConfidenceScore = 0.3,
                HumanReviewRequired = true,
                GeneratedBy = "ComplianceService"
            };
            _reasoningCapture.Attach(control, explainability);

            await _repository.AddAsync(control);
        }

        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("Loaded {Count} controls for program {ProgramId}, regulation: {Regulation}", 
            controls.Count, programId, request.Regulation);

        var data = new { Count = controls.Count };

        var response = ApiResponseFactory.Success(
            ApiResponseCode.SUCCESS,
            (object)data,
            traceId);

        var successMeta = ApiResponseCodes.Registry[ApiResponseCode.SUCCESS];
        return StatusCode(successMeta.HttpStatus, response);
    }

    [HttpGet("programs/{programId}/controls")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<Control>>>> GetByProgramId(Guid programId, CancellationToken ct)
    {
        var traceId = HttpContext.TraceIdentifier;

        if (programId == Guid.Empty)
        {
            var result = ApiResponseFactory.Failure<IReadOnlyList<Control>>(
                ApiResponseCode.VALIDATION_FAILED,
                traceId);

            var meta = ApiResponseCodes.Registry[ApiResponseCode.VALIDATION_FAILED];
            return StatusCode(meta.HttpStatus, result);
        }

        var program = await _programRepository.GetAsync(programId);
        if (program == null)
        {
            var result = ApiResponseFactory.Failure<IReadOnlyList<Control>>(
                ApiResponseCode.RESOURCE_NOT_FOUND,
                traceId);

            var meta = ApiResponseCodes.Registry[ApiResponseCode.RESOURCE_NOT_FOUND];
            return StatusCode(meta.HttpStatus, result);
        }

        var controls = await _repository.GetByProgramIdAsync(programId);

        var response = ApiResponseFactory.Success(
            ApiResponseCode.SUCCESS,
            (IReadOnlyList<Control>)controls,
            traceId);

        var successMeta = ApiResponseCodes.Registry[ApiResponseCode.SUCCESS];
        return StatusCode(successMeta.HttpStatus, response);
    }
}

public record LoadControlsRequest
{
    public string Regulation { get; init; } = string.Empty;
}
