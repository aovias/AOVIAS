using KnowledgeBase.Application.DTOs;
using KnowledgeBase.Application.Services;
using Microsoft.AspNetCore.Mvc;
using Shared.Infrastructure.Api;
using Shared.Infrastructure.DTO;

namespace KnowledgeBase.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/assessments")]
public sealed class AssessmentsController : ControllerBase
{
    private readonly AssessmentAppService _assessments;
    private readonly GapAnalysisAppService _gapAnalysis;

    public AssessmentsController(AssessmentAppService assessments, GapAnalysisAppService gapAnalysis)
    {
        _assessments = assessments;
        _gapAnalysis = gapAnalysis;
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<AssessmentDto>>> Create([FromBody] CreateAssessmentRequestDto request, CancellationToken cancellationToken)
    {
        var traceId = HttpContext.TraceIdentifier;
        if (request.FrameworkId == Guid.Empty || request.FacilityId == Guid.Empty)
        {
            var vm = ApiResponseCodes.Registry[ApiResponseCode.VALIDATION_FAILED];
            return StatusCode(vm.HttpStatus, ApiResponseFactory.Failure<AssessmentDto>(ApiResponseCode.VALIDATION_FAILED, traceId));
        }

        var data = await _assessments.CreateAsync(request, cancellationToken);
        var response = ApiResponseFactory.Success(ApiResponseCode.SUCCESS, data, traceId);
        var meta = ApiResponseCodes.Registry[ApiResponseCode.SUCCESS];
        return StatusCode(meta.HttpStatus, response);
    }

    [HttpPut("{id:guid}/items/{nodeId:guid}")]
    public async Task<ActionResult<ApiResponse<object?>>> UpsertItem(
        Guid id,
        Guid nodeId,
        [FromBody] UpdateAssessmentItemRequestDto request,
        CancellationToken cancellationToken)
    {
        var traceId = HttpContext.TraceIdentifier;
        try
        {
            await _assessments.UpsertItemAsync(id, nodeId, request, cancellationToken);
            var response = ApiResponseFactory.Success<object?>(ApiResponseCode.SUCCESS, null, traceId);
            var meta = ApiResponseCodes.Registry[ApiResponseCode.SUCCESS];
            return StatusCode(meta.HttpStatus, response);
        }
        catch (InvalidOperationException ex)
        {
            if (ex.Message.Contains("Action plan", StringComparison.OrdinalIgnoreCase))
            {
                var vm = ApiResponseCodes.Registry[ApiResponseCode.VALIDATION_FAILED];
                return StatusCode(vm.HttpStatus, ApiResponseFactory.Failure<object?>(ApiResponseCode.VALIDATION_FAILED, traceId));
            }

            var nf = ApiResponseCodes.Registry[ApiResponseCode.RESOURCE_NOT_FOUND];
            return StatusCode(nf.HttpStatus, ApiResponseFactory.Failure<object?>(ApiResponseCode.RESOURCE_NOT_FOUND, traceId));
        }
    }

    [HttpGet("{id:guid}/summary")]
    public async Task<ActionResult<ApiResponse<AssessmentSummaryResponseDto>>> Summary(Guid id, CancellationToken cancellationToken)
    {
        var traceId = HttpContext.TraceIdentifier;
        try
        {
            var data = await _assessments.GetSummaryAsync(id, cancellationToken);
            var response = ApiResponseFactory.Success(ApiResponseCode.SUCCESS, data, traceId);
            var meta = ApiResponseCodes.Registry[ApiResponseCode.SUCCESS];
            return StatusCode(meta.HttpStatus, response);
        }
        catch (InvalidOperationException)
        {
            var meta = ApiResponseCodes.Registry[ApiResponseCode.RESOURCE_NOT_FOUND];
            return StatusCode(meta.HttpStatus, ApiResponseFactory.Failure<AssessmentSummaryResponseDto>(ApiResponseCode.RESOURCE_NOT_FOUND, traceId));
        }
    }

    [HttpPost("from-wizard")]
    public async Task<ActionResult<ApiResponse<SubmitWizardAssessmentResponseDto>>> SubmitFromWizard(
        [FromBody] SubmitWizardAssessmentRequestDto request,
        CancellationToken cancellationToken)
    {
        var traceId = HttpContext.TraceIdentifier;
        if (request.FacilityId == Guid.Empty || request.FrameworkId == Guid.Empty)
        {
            var vm = ApiResponseCodes.Registry[ApiResponseCode.VALIDATION_FAILED];
            return StatusCode(vm.HttpStatus, ApiResponseFactory.Failure<SubmitWizardAssessmentResponseDto>(ApiResponseCode.VALIDATION_FAILED, traceId));
        }

        try
        {
            var data = await _assessments.SubmitWizardAnswersAsync(request, cancellationToken);
            var response = ApiResponseFactory.Success(ApiResponseCode.SUCCESS, data, traceId);
            var meta = ApiResponseCodes.Registry[ApiResponseCode.SUCCESS];
            return StatusCode(meta.HttpStatus, response);
        }
        catch (ArgumentException)
        {
            var vm = ApiResponseCodes.Registry[ApiResponseCode.VALIDATION_FAILED];
            return StatusCode(vm.HttpStatus, ApiResponseFactory.Failure<SubmitWizardAssessmentResponseDto>(ApiResponseCode.VALIDATION_FAILED, traceId));
        }
        catch (InvalidOperationException)
        {
            var nf = ApiResponseCodes.Registry[ApiResponseCode.RESOURCE_NOT_FOUND];
            return StatusCode(nf.HttpStatus, ApiResponseFactory.Failure<SubmitWizardAssessmentResponseDto>(ApiResponseCode.RESOURCE_NOT_FOUND, traceId));
        }
    }

    /// <summary>
    /// Submits Core wizard answers and returns the Core readiness score, critical failures,
    /// and progression eligibility.
    /// POST api/v1/assessments/from-core-wizard
    /// </summary>
    [HttpPost("from-core-wizard")]
    public async Task<ActionResult<ApiResponse<SubmitCoreWizardResponseDto>>> SubmitFromCoreWizard(
        [FromBody] SubmitCoreWizardRequestDto request,
        CancellationToken cancellationToken)
    {
        var traceId = HttpContext.TraceIdentifier;
        if (request.FacilityId == Guid.Empty || request.FrameworkId == Guid.Empty)
        {
            var vm = ApiResponseCodes.Registry[ApiResponseCode.VALIDATION_FAILED];
            return StatusCode(vm.HttpStatus, ApiResponseFactory.Failure<SubmitCoreWizardResponseDto>(ApiResponseCode.VALIDATION_FAILED, traceId));
        }

        try
        {
            var data = await _assessments.SubmitCoreWizardAnswersAsync(request, cancellationToken);
            var ok = ApiResponseCodes.Registry[ApiResponseCode.SUCCESS];
            return StatusCode(ok.HttpStatus, ApiResponseFactory.Success(ApiResponseCode.SUCCESS, data, traceId));
        }
        catch (ArgumentException)
        {
            var vm = ApiResponseCodes.Registry[ApiResponseCode.VALIDATION_FAILED];
            return StatusCode(vm.HttpStatus, ApiResponseFactory.Failure<SubmitCoreWizardResponseDto>(ApiResponseCode.VALIDATION_FAILED, traceId));
        }
        catch (InvalidOperationException)
        {
            var nf = ApiResponseCodes.Registry[ApiResponseCode.RESOURCE_NOT_FOUND];
            return StatusCode(nf.HttpStatus, ApiResponseFactory.Failure<SubmitCoreWizardResponseDto>(ApiResponseCode.RESOURCE_NOT_FOUND, traceId));
        }
    }

    [HttpPost("{id:guid}/gap-analysis")]
    public async Task<ActionResult<ApiResponse<GapAnalysisReportDto>>> GenerateGapAnalysis(
        Guid id,
        CancellationToken cancellationToken)
    {
        var traceId = HttpContext.TraceIdentifier;
        if (id == Guid.Empty)
        {
            var vm = ApiResponseCodes.Registry[ApiResponseCode.VALIDATION_FAILED];
            return StatusCode(vm.HttpStatus, ApiResponseFactory.Failure<GapAnalysisReportDto>(ApiResponseCode.VALIDATION_FAILED, traceId));
        }

        try
        {
            var data = await _gapAnalysis.GenerateAsync(new GenerateGapAnalysisCommand(id), cancellationToken);
            var response = ApiResponseFactory.Success(ApiResponseCode.SUCCESS, data, traceId);
            var meta = ApiResponseCodes.Registry[ApiResponseCode.SUCCESS];
            return StatusCode(meta.HttpStatus, response);
        }
        catch (InvalidOperationException)
        {
            var nf = ApiResponseCodes.Registry[ApiResponseCode.RESOURCE_NOT_FOUND];
            return StatusCode(nf.HttpStatus, ApiResponseFactory.Failure<GapAnalysisReportDto>(ApiResponseCode.RESOURCE_NOT_FOUND, traceId));
        }
    }

    [HttpGet("{id:guid}/gap-analysis")]
    public async Task<ActionResult<ApiResponse<GapAnalysisReportDto>>> GetGapAnalysis(
        Guid id,
        CancellationToken cancellationToken)
    {
        var traceId = HttpContext.TraceIdentifier;
        if (id == Guid.Empty)
        {
            var vm = ApiResponseCodes.Registry[ApiResponseCode.VALIDATION_FAILED];
            return StatusCode(vm.HttpStatus, ApiResponseFactory.Failure<GapAnalysisReportDto>(ApiResponseCode.VALIDATION_FAILED, traceId));
        }

        try
        {
            var data = await _gapAnalysis.GetByAssessmentIdAsync(new GetGapAnalysisByAssessmentQuery(id), cancellationToken);
            if (data is null)
            {
                var nf = ApiResponseCodes.Registry[ApiResponseCode.RESOURCE_NOT_FOUND];
                return StatusCode(nf.HttpStatus, ApiResponseFactory.Failure<GapAnalysisReportDto>(ApiResponseCode.RESOURCE_NOT_FOUND, traceId));
            }

            var response = ApiResponseFactory.Success(ApiResponseCode.SUCCESS, data, traceId);
            var meta = ApiResponseCodes.Registry[ApiResponseCode.SUCCESS];
            return StatusCode(meta.HttpStatus, response);
        }
        catch (InvalidOperationException)
        {
            var nf = ApiResponseCodes.Registry[ApiResponseCode.RESOURCE_NOT_FOUND];
            return StatusCode(nf.HttpStatus, ApiResponseFactory.Failure<GapAnalysisReportDto>(ApiResponseCode.RESOURCE_NOT_FOUND, traceId));
        }
    }
}
