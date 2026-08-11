using KnowledgeBase.Application.DTOs;
using KnowledgeBase.Application.Services;
using Microsoft.AspNetCore.Mvc;
using Shared.Infrastructure.Api;
using Shared.Infrastructure.DTO;

namespace KnowledgeBase.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/knowledge")]
public sealed class KnowledgeController : ControllerBase
{
    private readonly KnowledgebaseQueryAppService _queries;

    public KnowledgeController(KnowledgebaseQueryAppService queries) => _queries = queries;

    [HttpGet("node/{nodeId:guid}")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<KnowledgeItemDto>>>> ByNode(Guid nodeId, CancellationToken cancellationToken)
    {
        var traceId = HttpContext.TraceIdentifier;
        var data = await _queries.GetKnowledgeForNodeAsync(nodeId, cancellationToken);
        var response = ApiResponseFactory.Success(ApiResponseCode.SUCCESS, data, traceId);
        var meta = ApiResponseCodes.Registry[ApiResponseCode.SUCCESS];
        return StatusCode(meta.HttpStatus, response);
    }

    [HttpGet("node/code/{nodeCode}")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<KnowledgeItemDto>>>> ByNodeCode(
        string nodeCode,
        CancellationToken cancellationToken)
    {
        var traceId = HttpContext.TraceIdentifier;
        if (string.IsNullOrWhiteSpace(nodeCode))
        {
            var vm = ApiResponseCodes.Registry[ApiResponseCode.VALIDATION_FAILED];
            return StatusCode(vm.HttpStatus, ApiResponseFactory.Failure<IReadOnlyList<KnowledgeItemDto>>(ApiResponseCode.VALIDATION_FAILED, traceId));
        }

        var result = await _queries.GetKnowledgeForNodeByCodeAsync(nodeCode.Trim(), cancellationToken);
        return result.Status switch
        {
            KnowledgeByNodeCodeStatus.NotFound =>
                StatusCode(
                    ApiResponseCodes.Registry[ApiResponseCode.RESOURCE_NOT_FOUND].HttpStatus,
                    ApiResponseFactory.Failure<IReadOnlyList<KnowledgeItemDto>>(ApiResponseCode.RESOURCE_NOT_FOUND, traceId)),
            KnowledgeByNodeCodeStatus.Ambiguous =>
                StatusCode(
                    ApiResponseCodes.Registry[ApiResponseCode.RESOURCE_CONFLICT].HttpStatus,
                    ApiResponseFactory.Failure<IReadOnlyList<KnowledgeItemDto>>(ApiResponseCode.RESOURCE_CONFLICT, traceId)),
            KnowledgeByNodeCodeStatus.Ok =>
                StatusCode(
                    ApiResponseCodes.Registry[ApiResponseCode.SUCCESS].HttpStatus,
                    ApiResponseFactory.Success(ApiResponseCode.SUCCESS, result.Items!, traceId)),
            _ => throw new InvalidOperationException($"Unexpected {nameof(KnowledgeByNodeCodeStatus)}: {result.Status}"),
        };
    }

    [HttpGet("search")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<KnowledgeItemDto>>>> Search(
        [FromQuery] string? q,
        [FromQuery] Guid? frameworkId,
        CancellationToken cancellationToken)
    {
        var traceId = HttpContext.TraceIdentifier;
        try
        {
            var data = await _queries.SearchKnowledgeAsync(q, frameworkId, cancellationToken);
            var response = ApiResponseFactory.Success(ApiResponseCode.SUCCESS, data, traceId);
            var meta = ApiResponseCodes.Registry[ApiResponseCode.SUCCESS];
            return StatusCode(meta.HttpStatus, response);
        }
        catch (ArgumentException ex)
        {
            var vm = ApiResponseCodes.Registry[ApiResponseCode.VALIDATION_FAILED];
            return StatusCode(
                vm.HttpStatus,
                ApiResponseFactory.Failure<IReadOnlyList<KnowledgeItemDto>>(
                    ApiResponseCode.VALIDATION_FAILED,
                    new List<ApiError> { new() { Message = ex.Message } },
                    traceId));
        }
    }
}
