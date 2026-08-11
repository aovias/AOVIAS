using Gateway.Application.Handlers;
using Gateway.Application.Http;
using Gateway.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Infrastructure.Auth;

namespace Gateway.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/tasks")]
[Authorize(Policy = AuthConstants.Policies.ComplianceScope)]
public sealed class TasksV1ProxyController : ControllerBase
{
    private readonly IProxyService _proxy;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<TasksV1ProxyController> _logger;

    public TasksV1ProxyController(
        IProxyService proxy,
        IHttpContextAccessor httpContextAccessor,
        ILogger<TasksV1ProxyController> logger)
    {
        _proxy = proxy;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    [HttpGet("summary")]
    public Task<IActionResult> Summary(CancellationToken cancellationToken)
    {
        var v = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";
        var suffix = QuerySuffix();
        var endpoint = $"/api/v{v}/tasks/summary{suffix}";
        return _proxy.ProxyToComplianceServiceAsync(endpoint, HttpMethod.Get, null, ForwardIfMatch(), cancellationToken);
    }

    [HttpGet]
    public Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var v = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";
        var suffix = QuerySuffix();
        var endpoint = $"/api/v{v}/tasks{suffix}";
        return _proxy.ProxyToComplianceServiceAsync(endpoint, HttpMethod.Get, null, ForwardIfMatch(), cancellationToken);
    }

    [HttpGet("{taskId:guid}")]
    public Task<IActionResult> GetById(Guid taskId, CancellationToken cancellationToken)
    {
        var v = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";
        var suffix = QuerySuffix();
        var endpoint = $"/api/v{v}/tasks/{taskId}{suffix}";
        return _proxy.ProxyToComplianceServiceAsync(endpoint, HttpMethod.Get, null, ForwardIfMatch(), cancellationToken);
    }

    [HttpGet("{taskId:guid}/evidences")]
    public Task<IActionResult> Evidences(Guid taskId, CancellationToken cancellationToken)
    {
        var v = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";
        var suffix = QuerySuffix();
        var endpoint = $"/api/v{v}/tasks/{taskId}/evidences{suffix}";
        return _proxy.ProxyToComplianceServiceAsync(endpoint, HttpMethod.Get, null, ForwardIfMatch(), cancellationToken);
    }

    [HttpGet("{taskId:guid}/comments")]
    public Task<IActionResult> Comments(Guid taskId, CancellationToken cancellationToken)
    {
        var v = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";
        var suffix = QuerySuffix();
        var endpoint = $"/api/v{v}/tasks/{taskId}/comments{suffix}";
        return _proxy.ProxyToComplianceServiceAsync(endpoint, HttpMethod.Get, null, ForwardIfMatch(), cancellationToken);
    }

    [HttpGet("{taskId:guid}/history")]
    public Task<IActionResult> History(Guid taskId, CancellationToken cancellationToken)
    {
        var v = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";
        var suffix = QuerySuffix();
        var endpoint = $"/api/v{v}/tasks/{taskId}/history{suffix}";
        return _proxy.ProxyToComplianceServiceAsync(endpoint, HttpMethod.Get, null, ForwardIfMatch(), cancellationToken);
    }

    [HttpPost]
    public Task<IActionResult> Create([FromBody] object body, CancellationToken cancellationToken)
    {
        var v = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";
        var suffix = QuerySuffix();
        var endpoint = $"/api/v{v}/tasks{suffix}";
        return _proxy.ProxyToComplianceServiceAsync(endpoint, HttpMethod.Post, body, ForwardIfMatch(), cancellationToken);
    }

    [HttpPatch("{taskId:guid}")]
    public Task<IActionResult> Patch(Guid taskId, [FromBody] object body, CancellationToken cancellationToken)
    {
        var v = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";
        var suffix = QuerySuffix();
        var endpoint = $"/api/v{v}/tasks/{taskId}{suffix}";
        return _proxy.ProxyToComplianceServiceAsync(endpoint, HttpMethod.Patch, body, ForwardIfMatch(), cancellationToken);
    }

    [HttpPut("{taskId:guid}/complete")]
    public Task<IActionResult> Complete(Guid taskId, CancellationToken cancellationToken)
    {
        var v = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";
        var suffix = QuerySuffix();
        var endpoint = $"/api/v{v}/tasks/{taskId}/complete{suffix}";
        return _proxy.ProxyToComplianceServiceAsync(endpoint, HttpMethod.Put, null, ForwardIfMatch(), cancellationToken);
    }

    [HttpPost("{taskId:guid}/reassign")]
    public Task<IActionResult> Reassign(Guid taskId, [FromBody] object body, CancellationToken cancellationToken)
    {
        var v = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";
        var suffix = QuerySuffix();
        var endpoint = $"/api/v{v}/tasks/{taskId}/reassign{suffix}";
        return _proxy.ProxyToComplianceServiceAsync(endpoint, HttpMethod.Post, body, ForwardIfMatch(), cancellationToken);
    }

    [HttpPost("{taskId:guid}/comments")]
    public Task<IActionResult> AddComment(Guid taskId, [FromBody] object body, CancellationToken cancellationToken)
    {
        var v = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";
        var suffix = QuerySuffix();
        var endpoint = $"/api/v{v}/tasks/{taskId}/comments{suffix}";
        return _proxy.ProxyToComplianceServiceAsync(endpoint, HttpMethod.Post, body, ForwardIfMatch(), cancellationToken);
    }

    [HttpPost("{taskId:guid}/evidences")]
    public async Task<IActionResult> UploadEvidence(Guid taskId, CancellationToken cancellationToken)
    {
        var v = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";
        var suffix = QuerySuffix();
        var endpoint = $"/api/v{v}/tasks/{taskId}/evidences{suffix}";

        var httpContext = _httpContextAccessor.HttpContext!;
        var correlationId = EvidenceMultipartLogging.GetCorrelationId(httpContext);
        var (content, bodyByteLength) = await MultipartProxyRequestBody.CreateStreamContentAsync(
            httpContext.Request,
            cancellationToken,
            _logger);

        _logger.LogInformation(
            "Task evidence multipart proxy. CorrelationId={CorrelationId} TaskId={TaskId} BufferedBodyBytes={BufferedBodyBytes}",
            correlationId,
            taskId,
            bodyByteLength);

        return await _proxy.ProxyToComplianceServiceRawAsync(
            endpoint,
            HttpMethod.Post,
            content,
            ForwardIfMatch(),
            cancellationToken);
    }

    private Dictionary<string, string>? ForwardIfMatch()
    {
        if (!Request.Headers.TryGetValue("If-Match", out var v) || string.IsNullOrEmpty(v))
        {
            return null;
        }

        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["If-Match"] = v.ToString()
        };
    }

    private string QuerySuffix()
    {
        if (!Request.QueryString.HasValue || string.IsNullOrEmpty(Request.QueryString.Value))
        {
            return string.Empty;
        }

        return Request.QueryString.Value.StartsWith('?')
            ? Request.QueryString.Value
            : "?" + Request.QueryString.Value;
    }
}
