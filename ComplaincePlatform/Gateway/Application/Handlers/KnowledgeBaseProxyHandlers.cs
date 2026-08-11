using Gateway.Application.Commands;
using Gateway.Application.Queries;
using Gateway.Services;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shared.Infrastructure.Api;
using System.Text.Json;

namespace Gateway.Application.Handlers;

public class ListKnowledgeBaseFrameworksQueryHandler : IRequestHandler<ListKnowledgeBaseFrameworksQuery, IActionResult>
{
    private readonly IProxyService _proxy;

    public ListKnowledgeBaseFrameworksQueryHandler(IProxyService proxy) => _proxy = proxy;

    public Task<IActionResult> Handle(ListKnowledgeBaseFrameworksQuery request, CancellationToken cancellationToken) =>
        _proxy.ProxyToKnowledgeBaseServiceAsync(
            $"/api/v{request.ApiVersion}/frameworks",
            HttpMethod.Get,
            null,
            null,
            cancellationToken);
}

public class GetKnowledgeBaseFrameworkQueryHandler : IRequestHandler<GetKnowledgeBaseFrameworkQuery, IActionResult>
{
    private readonly IProxyService _proxy;
    private readonly IHttpContextAccessor _http;

    public GetKnowledgeBaseFrameworkQueryHandler(IProxyService proxy, IHttpContextAccessor http)
    {
        _proxy = proxy;
        _http = http;
    }

    public Task<IActionResult> Handle(GetKnowledgeBaseFrameworkQuery request, CancellationToken cancellationToken)
    {
        if (request.Id == Guid.Empty)
        {
            return Task.FromResult<IActionResult>(
                ApiResponseFactory.FailureResult<object>(ApiResponseCode.VALIDATION_FAILED, _http.HttpContext?.TraceIdentifier));
        }

        return _proxy.ProxyToKnowledgeBaseServiceAsync(
            $"/api/v{request.ApiVersion}/frameworks/{request.Id}",
            HttpMethod.Get,
            null,
            null,
            cancellationToken);
    }
}

public class ListKnowledgeBaseNodesQueryHandler : IRequestHandler<ListKnowledgeBaseNodesQuery, IActionResult>
{
    private readonly IProxyService _proxy;
    private readonly IHttpContextAccessor _http;

    public ListKnowledgeBaseNodesQueryHandler(IProxyService proxy, IHttpContextAccessor http)
    {
        _proxy = proxy;
        _http = http;
    }

    public Task<IActionResult> Handle(ListKnowledgeBaseNodesQuery request, CancellationToken cancellationToken)
    {
        if (request.FrameworkId == Guid.Empty)
        {
            return Task.FromResult<IActionResult>(
                ApiResponseFactory.FailureResult<object>(ApiResponseCode.VALIDATION_FAILED, _http.HttpContext?.TraceIdentifier));
        }

        return _proxy.ProxyToKnowledgeBaseServiceAsync(
            $"/api/v{request.ApiVersion}/nodes?frameworkId={request.FrameworkId}",
            HttpMethod.Get,
            null,
            null,
            cancellationToken);
    }
}

public class GetKnowledgeBaseNodeQueryHandler : IRequestHandler<GetKnowledgeBaseNodeQuery, IActionResult>
{
    private readonly IProxyService _proxy;
    private readonly IHttpContextAccessor _http;

    public GetKnowledgeBaseNodeQueryHandler(IProxyService proxy, IHttpContextAccessor http)
    {
        _proxy = proxy;
        _http = http;
    }

    public Task<IActionResult> Handle(GetKnowledgeBaseNodeQuery request, CancellationToken cancellationToken)
    {
        if (request.Id == Guid.Empty)
        {
            return Task.FromResult<IActionResult>(
                ApiResponseFactory.FailureResult<object>(ApiResponseCode.VALIDATION_FAILED, _http.HttpContext?.TraceIdentifier));
        }

        return _proxy.ProxyToKnowledgeBaseServiceAsync(
            $"/api/v{request.ApiVersion}/nodes/{request.Id}",
            HttpMethod.Get,
            null,
            null,
            cancellationToken);
    }
}

public class GetKnowledgeBaseNodeChildrenQueryHandler : IRequestHandler<GetKnowledgeBaseNodeChildrenQuery, IActionResult>
{
    private readonly IProxyService _proxy;
    private readonly IHttpContextAccessor _http;

    public GetKnowledgeBaseNodeChildrenQueryHandler(IProxyService proxy, IHttpContextAccessor http)
    {
        _proxy = proxy;
        _http = http;
    }

    public Task<IActionResult> Handle(GetKnowledgeBaseNodeChildrenQuery request, CancellationToken cancellationToken)
    {
        if (request.Id == Guid.Empty)
        {
            return Task.FromResult<IActionResult>(
                ApiResponseFactory.FailureResult<object>(ApiResponseCode.VALIDATION_FAILED, _http.HttpContext?.TraceIdentifier));
        }

        return _proxy.ProxyToKnowledgeBaseServiceAsync(
            $"/api/v{request.ApiVersion}/nodes/{request.Id}/children",
            HttpMethod.Get,
            null,
            null,
            cancellationToken);
    }
}

public class GetKnowledgeBaseNodeDetailsQueryHandler : IRequestHandler<GetKnowledgeBaseNodeDetailsQuery, IActionResult>
{
    private readonly IProxyService _proxy;
    private readonly IHttpContextAccessor _http;

    public GetKnowledgeBaseNodeDetailsQueryHandler(IProxyService proxy, IHttpContextAccessor http)
    {
        _proxy = proxy;
        _http = http;
    }

    public Task<IActionResult> Handle(GetKnowledgeBaseNodeDetailsQuery request, CancellationToken cancellationToken)
    {
        if (request.Id == Guid.Empty)
        {
            return Task.FromResult<IActionResult>(
                ApiResponseFactory.FailureResult<object>(ApiResponseCode.VALIDATION_FAILED, _http.HttpContext?.TraceIdentifier));
        }

        return _proxy.ProxyToKnowledgeBaseServiceAsync(
            $"/api/v{request.ApiVersion}/nodes/{request.Id}/details",
            HttpMethod.Get,
            null,
            null,
            cancellationToken);
    }
}

public class GetKnowledgeBaseNodeDetailsByCodeQueryHandler : IRequestHandler<GetKnowledgeBaseNodeDetailsByCodeQuery, IActionResult>
{
    private readonly IProxyService _proxy;
    private readonly IHttpContextAccessor _http;

    public GetKnowledgeBaseNodeDetailsByCodeQueryHandler(IProxyService proxy, IHttpContextAccessor http)
    {
        _proxy = proxy;
        _http = http;
    }

    public Task<IActionResult> Handle(GetKnowledgeBaseNodeDetailsByCodeQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.NodeCode))
        {
            return Task.FromResult<IActionResult>(
                ApiResponseFactory.FailureResult<object>(ApiResponseCode.VALIDATION_FAILED, _http.HttpContext?.TraceIdentifier));
        }

        var path = $"/api/v{request.ApiVersion}/nodes/code/{Uri.EscapeDataString(request.NodeCode.Trim())}/details";

        return _proxy.ProxyToKnowledgeBaseServiceAsync(
            path,
            HttpMethod.Get,
            null,
            null,
            cancellationToken);
    }
}

public class GetKnowledgeBaseNodeDecisionsQueryHandler : IRequestHandler<GetKnowledgeBaseNodeDecisionsQuery, IActionResult>
{
    private readonly IProxyService _proxy;
    private readonly IHttpContextAccessor _http;

    public GetKnowledgeBaseNodeDecisionsQueryHandler(IProxyService proxy, IHttpContextAccessor http)
    {
        _proxy = proxy;
        _http = http;
    }

    public Task<IActionResult> Handle(GetKnowledgeBaseNodeDecisionsQuery request, CancellationToken cancellationToken)
    {
        if (request.NodeId == Guid.Empty)
        {
            return Task.FromResult<IActionResult>(
                ApiResponseFactory.FailureResult<object>(ApiResponseCode.VALIDATION_FAILED, _http.HttpContext?.TraceIdentifier));
        }

        return _proxy.ProxyToKnowledgeBaseServiceAsync(
            $"/api/v{request.ApiVersion}/nodes/{request.NodeId}/decisions",
            HttpMethod.Get,
            null,
            null,
            cancellationToken);
    }
}

public class GetKnowledgeBaseKnowledgeByNodeQueryHandler : IRequestHandler<GetKnowledgeBaseKnowledgeByNodeQuery, IActionResult>
{
    private readonly IProxyService _proxy;
    private readonly IHttpContextAccessor _http;

    public GetKnowledgeBaseKnowledgeByNodeQueryHandler(IProxyService proxy, IHttpContextAccessor http)
    {
        _proxy = proxy;
        _http = http;
    }

    public Task<IActionResult> Handle(GetKnowledgeBaseKnowledgeByNodeQuery request, CancellationToken cancellationToken)
    {
        if (request.NodeId == Guid.Empty)
        {
            return Task.FromResult<IActionResult>(
                ApiResponseFactory.FailureResult<object>(ApiResponseCode.VALIDATION_FAILED, _http.HttpContext?.TraceIdentifier));
        }

        return _proxy.ProxyToKnowledgeBaseServiceAsync(
            $"/api/v{request.ApiVersion}/knowledge/node/{request.NodeId}",
            HttpMethod.Get,
            null,
            null,
            cancellationToken);
    }
}

public class GetKnowledgeBaseKnowledgeByNodeCodeQueryHandler : IRequestHandler<GetKnowledgeBaseKnowledgeByNodeCodeQuery, IActionResult>
{
    private readonly IProxyService _proxy;
    private readonly IHttpContextAccessor _http;

    public GetKnowledgeBaseKnowledgeByNodeCodeQueryHandler(IProxyService proxy, IHttpContextAccessor http)
    {
        _proxy = proxy;
        _http = http;
    }

    public Task<IActionResult> Handle(GetKnowledgeBaseKnowledgeByNodeCodeQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.NodeCode))
        {
            return Task.FromResult<IActionResult>(
                ApiResponseFactory.FailureResult<object>(ApiResponseCode.VALIDATION_FAILED, _http.HttpContext?.TraceIdentifier));
        }

        var path = $"/api/v{request.ApiVersion}/knowledge/node/code/{Uri.EscapeDataString(request.NodeCode.Trim())}";

        return _proxy.ProxyToKnowledgeBaseServiceAsync(
            path,
            HttpMethod.Get,
            null,
            null,
            cancellationToken);
    }
}

public class SearchKnowledgeBaseKnowledgeQueryHandler : IRequestHandler<SearchKnowledgeBaseKnowledgeQuery, IActionResult>
{
    private readonly IProxyService _proxy;

    public SearchKnowledgeBaseKnowledgeQueryHandler(IProxyService proxy) => _proxy = proxy;

    public Task<IActionResult> Handle(SearchKnowledgeBaseKnowledgeQuery request, CancellationToken cancellationToken)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(request.Q))
        {
            parts.Add($"q={Uri.EscapeDataString(request.Q)}");
        }

        if (request.FrameworkId.HasValue)
        {
            parts.Add($"frameworkId={request.FrameworkId.Value}");
        }

        var qs = parts.Count > 0 ? "?" + string.Join("&", parts) : string.Empty;
        return _proxy.ProxyToKnowledgeBaseServiceAsync(
            $"/api/v{request.ApiVersion}/knowledge/search{qs}",
            HttpMethod.Get,
            null,
            null,
            cancellationToken);
    }
}

public class GetKnowledgeBaseAssessmentSummaryQueryHandler : IRequestHandler<GetKnowledgeBaseAssessmentSummaryQuery, IActionResult>
{
    private readonly IProxyService _proxy;
    private readonly IHttpContextAccessor _http;

    public GetKnowledgeBaseAssessmentSummaryQueryHandler(IProxyService proxy, IHttpContextAccessor http)
    {
        _proxy = proxy;
        _http = http;
    }

    public Task<IActionResult> Handle(GetKnowledgeBaseAssessmentSummaryQuery request, CancellationToken cancellationToken)
    {
        if (request.Id == Guid.Empty)
        {
            return Task.FromResult<IActionResult>(
                ApiResponseFactory.FailureResult<object>(ApiResponseCode.VALIDATION_FAILED, _http.HttpContext?.TraceIdentifier));
        }

        return _proxy.ProxyToKnowledgeBaseServiceAsync(
            $"/api/v{request.ApiVersion}/assessments/{request.Id}/summary",
            HttpMethod.Get,
            null,
            null,
            cancellationToken);
    }
}

public class PostKnowledgeBaseGapAnalysisCommandHandler : IRequestHandler<PostKnowledgeBaseGapAnalysisCommand, IActionResult>
{
    private readonly IProxyService _proxy;
    private readonly IHttpContextAccessor _http;

    public PostKnowledgeBaseGapAnalysisCommandHandler(IProxyService proxy, IHttpContextAccessor http)
    {
        _proxy = proxy;
        _http = http;
    }

    public Task<IActionResult> Handle(PostKnowledgeBaseGapAnalysisCommand request, CancellationToken cancellationToken)
    {
        if (request.AssessmentId == Guid.Empty)
        {
            return Task.FromResult<IActionResult>(
                ApiResponseFactory.FailureResult<object>(ApiResponseCode.VALIDATION_FAILED, _http.HttpContext?.TraceIdentifier));
        }

        return _proxy.ProxyToKnowledgeBaseServiceAsync(
            $"/api/v{request.ApiVersion}/assessments/{request.AssessmentId}/gap-analysis",
            HttpMethod.Post,
            null,
            null,
            cancellationToken);
    }
}

public class GetKnowledgeBaseGapAnalysisByAssessmentQueryHandler : IRequestHandler<GetKnowledgeBaseGapAnalysisByAssessmentQuery, IActionResult>
{
    private readonly IProxyService _proxy;
    private readonly IHttpContextAccessor _http;

    public GetKnowledgeBaseGapAnalysisByAssessmentQueryHandler(IProxyService proxy, IHttpContextAccessor http)
    {
        _proxy = proxy;
        _http = http;
    }

    public Task<IActionResult> Handle(GetKnowledgeBaseGapAnalysisByAssessmentQuery request, CancellationToken cancellationToken)
    {
        if (request.AssessmentId == Guid.Empty)
        {
            return Task.FromResult<IActionResult>(
                ApiResponseFactory.FailureResult<object>(ApiResponseCode.VALIDATION_FAILED, _http.HttpContext?.TraceIdentifier));
        }

        return _proxy.ProxyToKnowledgeBaseServiceAsync(
            $"/api/v{request.ApiVersion}/assessments/{request.AssessmentId}/gap-analysis",
            HttpMethod.Get,
            null,
            null,
            cancellationToken);
    }
}

public class PostKnowledgeBaseAiAskCommandHandler : IRequestHandler<PostKnowledgeBaseAiAskCommand, IActionResult>
{
    private readonly IProxyService _proxy;
    private readonly IHttpContextAccessor _http;

    public PostKnowledgeBaseAiAskCommandHandler(IProxyService proxy, IHttpContextAccessor http)
    {
        _proxy = proxy;
        _http = http;
    }

    public Task<IActionResult> Handle(PostKnowledgeBaseAiAskCommand request, CancellationToken cancellationToken)
    {
        if (!TryGetCanonicalFrameworkId(request.Body, out var frameworkId) || frameworkId == Guid.Empty)
        {
            return Task.FromResult<IActionResult>(
                ApiResponseFactory.FailureResult<object>(ApiResponseCode.VALIDATION_FAILED, _http.HttpContext?.TraceIdentifier));
        }

        return _proxy.ProxyToKnowledgeBaseServiceAsync(
            $"/api/v{request.ApiVersion}/ai/ask",
            HttpMethod.Post,
            request.Body,
            null,
            cancellationToken);
    }

    private static bool TryGetCanonicalFrameworkId(JsonElement body, out Guid frameworkId)
    {
        frameworkId = Guid.Empty;
        if (body.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        if (!body.TryGetProperty("frameworkId", out var frameworkProperty))
        {
            return false;
        }

        if (frameworkProperty.ValueKind == JsonValueKind.String &&
            Guid.TryParse(frameworkProperty.GetString(), out frameworkId))
        {
            return true;
        }

        return frameworkProperty.ValueKind == JsonValueKind.Undefined
            ? false
            : Guid.TryParse(frameworkProperty.GetRawText().Trim('"'), out frameworkId);
    }
}

public class PostKnowledgeBaseDecisionCommandHandler : IRequestHandler<PostKnowledgeBaseDecisionCommand, IActionResult>
{
    private readonly IProxyService _proxy;

    public PostKnowledgeBaseDecisionCommandHandler(IProxyService proxy) => _proxy = proxy;

    public Task<IActionResult> Handle(PostKnowledgeBaseDecisionCommand request, CancellationToken cancellationToken) =>
        _proxy.ProxyToKnowledgeBaseServiceAsync(
            $"/api/v{request.ApiVersion}/decisions",
            HttpMethod.Post,
            request.Body,
            null,
            cancellationToken);
}

public class PostKnowledgeBaseAssessmentCommandHandler : IRequestHandler<PostKnowledgeBaseAssessmentCommand, IActionResult>
{
    private readonly IProxyService _proxy;

    public PostKnowledgeBaseAssessmentCommandHandler(IProxyService proxy) => _proxy = proxy;

    public Task<IActionResult> Handle(PostKnowledgeBaseAssessmentCommand request, CancellationToken cancellationToken) =>
        _proxy.ProxyToKnowledgeBaseServiceAsync(
            $"/api/v{request.ApiVersion}/assessments",
            HttpMethod.Post,
            request.Body,
            null,
            cancellationToken);
}

public class PutKnowledgeBaseAssessmentItemCommandHandler : IRequestHandler<PutKnowledgeBaseAssessmentItemCommand, IActionResult>
{
    private readonly IProxyService _proxy;
    private readonly IHttpContextAccessor _http;

    public PutKnowledgeBaseAssessmentItemCommandHandler(IProxyService proxy, IHttpContextAccessor http)
    {
        _proxy = proxy;
        _http = http;
    }

    public Task<IActionResult> Handle(PutKnowledgeBaseAssessmentItemCommand request, CancellationToken cancellationToken)
    {
        if (request.AssessmentId == Guid.Empty || request.NodeId == Guid.Empty)
        {
            return Task.FromResult<IActionResult>(
                ApiResponseFactory.FailureResult<object>(ApiResponseCode.VALIDATION_FAILED, _http.HttpContext?.TraceIdentifier));
        }

        return _proxy.ProxyToKnowledgeBaseServiceAsync(
            $"/api/v{request.ApiVersion}/assessments/{request.AssessmentId}/items/{request.NodeId}",
            HttpMethod.Put,
            request.Body,
            null,
            cancellationToken);
    }
}
