using KnowledgeBase.Application.DTOs;
using KnowledgeBase.Application.Services;
using Microsoft.AspNetCore.Mvc;
using Shared.Infrastructure.Api;
using Shared.Infrastructure.DTO;

namespace KnowledgeBase.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/nodes")]
public sealed class NodesController : ControllerBase
{
    private readonly KnowledgebaseQueryAppService _queries;
    private readonly DecisionAppService _decisions;

    public NodesController(KnowledgebaseQueryAppService queries, DecisionAppService decisions)
    {
        _queries = queries;
        _decisions = decisions;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ComplianceNodeDto>>>> List([FromQuery] Guid frameworkId, CancellationToken cancellationToken)
    {
        var traceId = HttpContext.TraceIdentifier;
        if (frameworkId == Guid.Empty)
        {
            var vm = ApiResponseCodes.Registry[ApiResponseCode.VALIDATION_FAILED];
            return StatusCode(vm.HttpStatus, ApiResponseFactory.Failure<IReadOnlyList<ComplianceNodeDto>>(ApiResponseCode.VALIDATION_FAILED, traceId));
        }

        try
        {
            var data = await _queries.ListNodesAsync(frameworkId, cancellationToken);
            var response = ApiResponseFactory.Success(ApiResponseCode.SUCCESS, data, traceId);
            var meta = ApiResponseCodes.Registry[ApiResponseCode.SUCCESS];
            return StatusCode(meta.HttpStatus, response);
        }
        catch (ArgumentException ex)
        {
            var vm = ApiResponseCodes.Registry[ApiResponseCode.VALIDATION_FAILED];
            return StatusCode(
                vm.HttpStatus,
                ApiResponseFactory.Failure<IReadOnlyList<ComplianceNodeDto>>(
                    ApiResponseCode.VALIDATION_FAILED,
                    new List<ApiError> { new() { Message = ex.Message } },
                    traceId));
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<ComplianceNodeDto>>> Get(Guid id, CancellationToken cancellationToken)
    {
        var traceId = HttpContext.TraceIdentifier;
        var n = await _queries.GetNodeAsync(id, cancellationToken);
        if (n is null)
        {
            var meta = ApiResponseCodes.Registry[ApiResponseCode.RESOURCE_NOT_FOUND];
            return StatusCode(meta.HttpStatus, ApiResponseFactory.Failure<ComplianceNodeDto>(ApiResponseCode.RESOURCE_NOT_FOUND, traceId));
        }

        var response = ApiResponseFactory.Success(ApiResponseCode.SUCCESS, n, traceId);
        var ok = ApiResponseCodes.Registry[ApiResponseCode.SUCCESS];
        return StatusCode(ok.HttpStatus, response);
    }

    [HttpGet("{id:guid}/children")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ComplianceNodeDto>>>> Children(Guid id, CancellationToken cancellationToken)
    {
        var traceId = HttpContext.TraceIdentifier;
        var data = await _queries.GetChildrenAsync(id, cancellationToken);
        var response = ApiResponseFactory.Success(ApiResponseCode.SUCCESS, data, traceId);
        var meta = ApiResponseCodes.Registry[ApiResponseCode.SUCCESS];
        return StatusCode(meta.HttpStatus, response);
    }

    [HttpGet("{id:guid}/details")]
    public async Task<ActionResult<ApiResponse<NodeDetailsDto>>> Details(Guid id, CancellationToken cancellationToken)
    {
        var traceId = HttpContext.TraceIdentifier;
        var d = await _queries.GetNodeDetailsAsync(id, cancellationToken);
        if (d is null)
        {
            var meta = ApiResponseCodes.Registry[ApiResponseCode.RESOURCE_NOT_FOUND];
            return StatusCode(meta.HttpStatus, ApiResponseFactory.Failure<NodeDetailsDto>(ApiResponseCode.RESOURCE_NOT_FOUND, traceId));
        }

        var response = ApiResponseFactory.Success(ApiResponseCode.SUCCESS, d, traceId);
        var ok = ApiResponseCodes.Registry[ApiResponseCode.SUCCESS];
        return StatusCode(ok.HttpStatus, response);
    }

    [HttpGet("code/{nodeCode}/details")]
    public async Task<ActionResult<ApiResponse<NodeDetailsDto>>> DetailsByCode(
        string nodeCode,
        CancellationToken cancellationToken)
    {
        var traceId = HttpContext.TraceIdentifier;
        if (string.IsNullOrWhiteSpace(nodeCode))
        {
            var vm = ApiResponseCodes.Registry[ApiResponseCode.VALIDATION_FAILED];
            return StatusCode(vm.HttpStatus, ApiResponseFactory.Failure<NodeDetailsDto>(ApiResponseCode.VALIDATION_FAILED, traceId));
        }

        var result = await _queries.GetNodeDetailsByCodeAsync(nodeCode.Trim(), cancellationToken);
        return result.Status switch
        {
            KnowledgeByNodeCodeStatus.NotFound =>
                StatusCode(
                    ApiResponseCodes.Registry[ApiResponseCode.RESOURCE_NOT_FOUND].HttpStatus,
                    ApiResponseFactory.Failure<NodeDetailsDto>(ApiResponseCode.RESOURCE_NOT_FOUND, traceId)),
            KnowledgeByNodeCodeStatus.Ambiguous =>
                StatusCode(
                    ApiResponseCodes.Registry[ApiResponseCode.RESOURCE_CONFLICT].HttpStatus,
                    ApiResponseFactory.Failure<NodeDetailsDto>(ApiResponseCode.RESOURCE_CONFLICT, traceId)),
            KnowledgeByNodeCodeStatus.Ok =>
                StatusCode(
                    ApiResponseCodes.Registry[ApiResponseCode.SUCCESS].HttpStatus,
                    ApiResponseFactory.Success(ApiResponseCode.SUCCESS, result.Details!, traceId)),
            _ => throw new InvalidOperationException($"Unexpected {nameof(KnowledgeByNodeCodeStatus)}: {result.Status}"),
        };
    }

    [HttpGet("{nodeId:guid}/decisions")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<DecisionLogDto>>>> Decisions(Guid nodeId, CancellationToken cancellationToken)
    {
        var traceId = HttpContext.TraceIdentifier;
        var data = await _decisions.ListByNodeAsync(nodeId, cancellationToken);
        var response = ApiResponseFactory.Success(ApiResponseCode.SUCCESS, data, traceId);
        var meta = ApiResponseCodes.Registry[ApiResponseCode.SUCCESS];
        return StatusCode(meta.HttpStatus, response);
    }
}
