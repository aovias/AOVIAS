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
[Route("api/v{version:apiVersion}/compliance/programs")]
[Authorize(Policy = Shared.Infrastructure.Auth.AuthConstants.Policies.ComplianceScope)]
[ValidateHospitalId]
public class ProgramsController : ControllerBase
{
    private readonly IComplianceProgramRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPlatformIdGenerator _idGenerator;
    private readonly IReasoningCapture _reasoningCapture;
    private readonly IUserContextService _userContext;
    private readonly ILogger<ProgramsController> _logger;

    public ProgramsController(
        IComplianceProgramRepository repository,
        IUnitOfWork unitOfWork,
        IPlatformIdGenerator idGenerator,
        IReasoningCapture reasoningCapture,
        IUserContextService userContext,
        ILogger<ProgramsController> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _idGenerator = idGenerator;
        _reasoningCapture = reasoningCapture;
        _userContext = userContext;
        _logger = logger;
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<object>>> Create([FromBody] CreateProgramRequest request, CancellationToken ct)
    {
        var traceId = HttpContext.TraceIdentifier;

        if (string.IsNullOrWhiteSpace(request?.Regulation))
        {
            var result = ApiResponseFactory.Failure<object>(
                ApiResponseCode.VALIDATION_FAILED,
                traceId);

            var meta = ApiResponseCodes.Registry[ApiResponseCode.VALIDATION_FAILED];
            return StatusCode(meta.HttpStatus, result);
        }

        // Use HospitalId from user context (JWT token)
        var program = new ComplianceProgram
        {
            Id = Guid.NewGuid(),
            PlatformId = await _idGenerator.GenerateAsync("PROG", ct),
            HospitalId = _userContext.HospitalId,
            Name = $"{request.Regulation} Compliance Program",
            CreatedAt = DateTime.UtcNow
        };

        // Attach explainability metadata
        var explainability = new ExplainabilityDto
        {
            Reasoning = $"This compliance program was created in response to a request to establish compliance with the {request.Regulation} regulation for hospital {_userContext.HospitalId}. The program was created to assist in organizing and tracking compliance activities related to this regulatory framework. This is an initial program structure that requires human review and validation to ensure it aligns with the hospital's specific compliance needs and regulatory requirements.",
            RulesReferenced = new List<string> { request.Regulation, $"Hospital:{_userContext.HospitalId}" },
            ConfidenceScore = 0.3,
            HumanReviewRequired = true,
            GeneratedBy = "ComplianceService"
        };
        _reasoningCapture.Attach(program, explainability);

        await _repository.AddAsync(program);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("Created compliance program {ProgramId} (PlatformId: {PlatformId}) for hospital {HospitalId}", 
            program.Id, program.PlatformId, _userContext.HospitalId);

        var data = new { ProgramId = program.Id };

        var response = ApiResponseFactory.Success(
            ApiResponseCode.SUCCESS,
            (object)data,
            traceId);

        var successMeta = ApiResponseCodes.Registry[ApiResponseCode.SUCCESS];
        return StatusCode(successMeta.HttpStatus, response);
    }

    [HttpGet("{programId}")]
    public async Task<ActionResult<ApiResponse<ComplianceProgram>>> Get(Guid programId, CancellationToken ct)
    {
        var traceId = HttpContext.TraceIdentifier;

        if (programId == Guid.Empty)
        {
            var result = ApiResponseFactory.Failure<ComplianceProgram>(
                ApiResponseCode.VALIDATION_FAILED,
                traceId);

            var meta = ApiResponseCodes.Registry[ApiResponseCode.VALIDATION_FAILED];
            return StatusCode(meta.HttpStatus, result);
        }

        var program = await _repository.GetAsync(programId);
        if (program == null)
        {
            var result = ApiResponseFactory.Failure<ComplianceProgram>(
                ApiResponseCode.RESOURCE_NOT_FOUND,
                traceId);

            var meta = ApiResponseCodes.Registry[ApiResponseCode.RESOURCE_NOT_FOUND];
            return StatusCode(meta.HttpStatus, result);
        }

        var response = ApiResponseFactory.Success(
            ApiResponseCode.SUCCESS,
            program,
            traceId);

        var successMeta = ApiResponseCodes.Registry[ApiResponseCode.SUCCESS];
        return StatusCode(successMeta.HttpStatus, response);
    }

    /// <summary>
    /// Example protected endpoint - returns 401 if no token, 403 if wrong scope
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<object>>> GetComplianceData(CancellationToken ct)
    {
        var traceId = HttpContext.TraceIdentifier;

        // Use UserContext instead of reading claims directly
        var data = new
        {
            Message = "Compliance data retrieved successfully",
            UserId = _userContext.UserId,
            Email = _userContext.Email,
            HospitalId = _userContext.HospitalId,
            Role = _userContext.Role,
            Timestamp = DateTime.UtcNow
        };

        var response = ApiResponseFactory.Success(
            ApiResponseCode.SUCCESS,
            (object)data,
            traceId);

        var successMeta = ApiResponseCodes.Registry[ApiResponseCode.SUCCESS];
        return StatusCode(successMeta.HttpStatus, response);
    }
}

public record CreateProgramRequest
{
    public string Regulation { get; init; } = string.Empty;
    public Guid? HospitalId { get; init; } // Optional - will be validated against token
}
