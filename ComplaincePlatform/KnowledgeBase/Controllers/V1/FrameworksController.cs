using KnowledgeFrameworkDto = KnowledgeBase.Application.DTOs.FrameworkDto;
using KnowledgeBase.Application.DTOs;
using KnowledgeBase.Application.Services;
using Microsoft.AspNetCore.Mvc;
using Shared.Infrastructure.Api;
using Shared.Infrastructure.DTO;

namespace KnowledgeBase.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/frameworks")]
public sealed class FrameworksController : ControllerBase
{
    private readonly KnowledgebaseQueryAppService _queries;
    private readonly QuestionnaireAppService _questionnaires;
    private readonly CoreAssessmentService _coreAssessment;

    public FrameworksController(
        KnowledgebaseQueryAppService queries,
        QuestionnaireAppService questionnaires,
        CoreAssessmentService coreAssessment)
    {
        _queries = queries;
        _questionnaires = questionnaires;
        _coreAssessment = coreAssessment;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<KnowledgeFrameworkDto>>>> List(CancellationToken cancellationToken)
    {
        var traceId = HttpContext.TraceIdentifier;
        var data = await _queries.ListFrameworksAsync(cancellationToken);
        var response = ApiResponseFactory.Success(ApiResponseCode.SUCCESS, data, traceId);
        var meta = ApiResponseCodes.Registry[ApiResponseCode.SUCCESS];
        return StatusCode(meta.HttpStatus, response);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<KnowledgeFrameworkDto>>> Get(Guid id, CancellationToken cancellationToken)
    {
        var traceId = HttpContext.TraceIdentifier;
        var f = await _queries.GetFrameworkAsync(id, cancellationToken);
        if (f is null)
        {
            var meta = ApiResponseCodes.Registry[ApiResponseCode.RESOURCE_NOT_FOUND];
            return StatusCode(meta.HttpStatus, ApiResponseFactory.Failure<KnowledgeFrameworkDto>(ApiResponseCode.RESOURCE_NOT_FOUND, traceId));
        }

        var response = ApiResponseFactory.Success(ApiResponseCode.SUCCESS, f, traceId);
        var ok = ApiResponseCodes.Registry[ApiResponseCode.SUCCESS];
        return StatusCode(ok.HttpStatus, response);
    }

    [HttpGet("{id:guid}/questionnaire")]
    public async Task<ActionResult<ApiResponse<FrameworkQuestionnaireDto>>> GetQuestionnaire(
        Guid id,
        CancellationToken cancellationToken)
    {
        var traceId = HttpContext.TraceIdentifier;
        var questionnaire = await _questionnaires.GetFrameworkQuestionnaireAsync(id, cancellationToken);
        if (questionnaire is null)
        {
            var meta = ApiResponseCodes.Registry[ApiResponseCode.RESOURCE_NOT_FOUND];
            return StatusCode(meta.HttpStatus, ApiResponseFactory.Failure<FrameworkQuestionnaireDto>(ApiResponseCode.RESOURCE_NOT_FOUND, traceId));
        }

        var response = ApiResponseFactory.Success(ApiResponseCode.SUCCESS, questionnaire, traceId);
        var ok = ApiResponseCodes.Registry[ApiResponseCode.SUCCESS];
        return StatusCode(ok.HttpStatus, response);
    }

    /// <summary>
    /// Returns the risk-selected Core maturity question list for a framework.
    /// GET api/v1/frameworks/{id}/core-assessment?count=40
    /// </summary>
    [HttpGet("{id:guid}/core-assessment")]
    public async Task<ActionResult<ApiResponse<CoreAssessmentDto>>> GetCoreAssessment(
        Guid id,
        [FromQuery] int count = 40,
        CancellationToken cancellationToken = default)
    {
        var traceId = HttpContext.TraceIdentifier;
        var dto = await _coreAssessment.GetCoreQuestionsAsync(id, count, cancellationToken);
        if (dto is null)
        {
            var nf = ApiResponseCodes.Registry[ApiResponseCode.RESOURCE_NOT_FOUND];
            return StatusCode(nf.HttpStatus, ApiResponseFactory.Failure<CoreAssessmentDto>(ApiResponseCode.RESOURCE_NOT_FOUND, traceId));
        }

        var ok = ApiResponseCodes.Registry[ApiResponseCode.SUCCESS];
        return StatusCode(ok.HttpStatus, ApiResponseFactory.Success(ApiResponseCode.SUCCESS, dto, traceId));
    }
}
