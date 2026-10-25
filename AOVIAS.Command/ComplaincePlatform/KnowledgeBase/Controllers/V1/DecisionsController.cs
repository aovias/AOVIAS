using KnowledgeBase.Application.DTOs;
using KnowledgeBase.Application.Services;
using Microsoft.AspNetCore.Mvc;
using Shared.Infrastructure.Api;
using Shared.Infrastructure.DTO;

namespace KnowledgeBase.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/decisions")]
public sealed class DecisionsController : ControllerBase
{
    private readonly DecisionAppService _decisions;

    public DecisionsController(DecisionAppService decisions) => _decisions = decisions;

    [HttpPost]
    public async Task<ActionResult<ApiResponse<DecisionLogDto>>> Create([FromBody] CreateDecisionRequestDto request, CancellationToken cancellationToken)
    {
        var traceId = HttpContext.TraceIdentifier;
        try
        {
            var data = await _decisions.CreateAsync(request, cancellationToken);
            var response = ApiResponseFactory.Success(ApiResponseCode.SUCCESS, data, traceId);
            var meta = ApiResponseCodes.Registry[ApiResponseCode.SUCCESS];
            return StatusCode(meta.HttpStatus, response);
        }
        catch (InvalidOperationException)
        {
            var meta = ApiResponseCodes.Registry[ApiResponseCode.RESOURCE_NOT_FOUND];
            return StatusCode(meta.HttpStatus, ApiResponseFactory.Failure<DecisionLogDto>(ApiResponseCode.RESOURCE_NOT_FOUND, traceId));
        }
    }
}
