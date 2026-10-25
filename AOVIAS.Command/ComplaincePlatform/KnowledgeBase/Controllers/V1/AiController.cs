using KnowledgeBase.Application.DTOs;
using KnowledgeBase.Application.Services;
using Microsoft.AspNetCore.Mvc;
using Shared.Infrastructure.Api;
using Shared.Infrastructure.DTO;

namespace KnowledgeBase.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/ai")]
public sealed class AiController : ControllerBase
{
    private readonly AiAskAppService _ai;

    public AiController(AiAskAppService ai) => _ai = ai;

    [HttpPost("ask")]
    public async Task<ActionResult<ApiResponse<AiAskResponseDto>>> Ask([FromBody] AiAskRequestDto request, CancellationToken cancellationToken)
    {
        var traceId = HttpContext.TraceIdentifier;
        if (request.FrameworkId == Guid.Empty || string.IsNullOrWhiteSpace(request.Query))
        {
            var vm = ApiResponseCodes.Registry[ApiResponseCode.VALIDATION_FAILED];
            return StatusCode(vm.HttpStatus, ApiResponseFactory.Failure<AiAskResponseDto>(ApiResponseCode.VALIDATION_FAILED, traceId));
        }

        try
        {
            var data = await _ai.AskAsync(request, cancellationToken);
            var response = ApiResponseFactory.Success(ApiResponseCode.SUCCESS, data, traceId);
            var meta = ApiResponseCodes.Registry[ApiResponseCode.SUCCESS];
            return StatusCode(meta.HttpStatus, response);
        }
        catch (ArgumentException ex)
        {
            var vm = ApiResponseCodes.Registry[ApiResponseCode.VALIDATION_FAILED];
            return StatusCode(
                vm.HttpStatus,
                ApiResponseFactory.Failure<AiAskResponseDto>(
                    ApiResponseCode.VALIDATION_FAILED,
                    new List<ApiError> { new() { Message = ex.Message } },
                    traceId));
        }
    }
}
