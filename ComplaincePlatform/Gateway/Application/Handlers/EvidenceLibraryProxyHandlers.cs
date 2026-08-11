using Gateway.Application.Commands;
using Gateway.Application.Queries;
using Gateway.Services;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shared.Infrastructure.Api;

namespace Gateway.Application.Handlers;

public sealed class GetEvidenceSummaryQueryHandler : IRequestHandler<GetEvidenceSummaryQuery, IActionResult>
{
    private readonly IProxyService _proxyService;

    public GetEvidenceSummaryQueryHandler(IProxyService proxyService)
    {
        _proxyService = proxyService;
    }

    public Task<IActionResult> Handle(GetEvidenceSummaryQuery request, CancellationToken cancellationToken)
    {
        var endpoint = $"/api/v{request.ApiVersion}/evidence/summary?frameworkId={request.FrameworkId}";
        return _proxyService.ProxyToComplianceServiceAsync(endpoint, HttpMethod.Get, null, null, cancellationToken);
    }
}

public sealed class GetEvidenceListQueryHandler : IRequestHandler<GetEvidenceListQuery, IActionResult>
{
    private readonly IProxyService _proxyService;

    public GetEvidenceListQueryHandler(IProxyService proxyService)
    {
        _proxyService = proxyService;
    }

    public Task<IActionResult> Handle(GetEvidenceListQuery request, CancellationToken cancellationToken)
    {
        var q = new List<string> { $"frameworkId={request.FrameworkId}" };

        if (request.ObjectiveElementId is { } oe && oe != Guid.Empty)
        {
            q.Add($"objectiveElementId={oe}");
        }

        if (request.ChapterId is { } ch && ch != Guid.Empty)
        {
            q.Add($"chapterId={ch}");
        }

        if (request.StandardId is { } st && st != Guid.Empty)
        {
            q.Add($"standardId={st}");
        }

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            q.Add($"status={Uri.EscapeDataString(request.Status)}");
        }

        if (request.DepartmentId is { } d && d != Guid.Empty)
        {
            q.Add($"departmentId={d}");
        }

        if (!string.IsNullOrWhiteSpace(request.FileType))
        {
            q.Add($"fileType={Uri.EscapeDataString(request.FileType)}");
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            q.Add($"search={Uri.EscapeDataString(request.Search)}");
        }

        q.Add($"page={request.Page}");
        q.Add($"pageSize={request.PageSize}");

        if (!string.IsNullOrWhiteSpace(request.SortBy))
        {
            q.Add($"sortBy={Uri.EscapeDataString(request.SortBy)}");
        }

        if (!string.IsNullOrWhiteSpace(request.SortOrder))
        {
            q.Add($"sortOrder={Uri.EscapeDataString(request.SortOrder)}");
        }

        var endpoint = $"/api/v{request.ApiVersion}/evidence/list?" + string.Join("&", q);
        return _proxyService.ProxyToComplianceServiceAsync(endpoint, HttpMethod.Get, null, null, cancellationToken);
    }
}

public sealed class GetEvidenceAnalyticsQueryHandler : IRequestHandler<GetEvidenceAnalyticsQuery, IActionResult>
{
    private readonly IProxyService _proxyService;

    public GetEvidenceAnalyticsQueryHandler(IProxyService proxyService)
    {
        _proxyService = proxyService;
    }

    public Task<IActionResult> Handle(GetEvidenceAnalyticsQuery request, CancellationToken cancellationToken)
    {
        var endpoint = $"/api/v{request.ApiVersion}/evidence/analytics?frameworkId={request.FrameworkId}";
        return _proxyService.ProxyToComplianceServiceAsync(endpoint, HttpMethod.Get, null, null, cancellationToken);
    }
}

public sealed class GetEvidenceRecentActivityQueryHandler : IRequestHandler<GetEvidenceRecentActivityQuery, IActionResult>
{
    private readonly IProxyService _proxyService;

    public GetEvidenceRecentActivityQueryHandler(IProxyService proxyService)
    {
        _proxyService = proxyService;
    }

    public Task<IActionResult> Handle(GetEvidenceRecentActivityQuery request, CancellationToken cancellationToken)
    {
        var take = Math.Clamp(request.Take, 1, 20);
        var endpoint =
            $"/api/v{request.ApiVersion}/evidence/recent-activity?frameworkId={request.FrameworkId}&take={take}";
        return _proxyService.ProxyToComplianceServiceAsync(endpoint, HttpMethod.Get, null, null, cancellationToken);
    }
}

public sealed class GetEvidenceDashboardQueryHandler : IRequestHandler<GetEvidenceDashboardQuery, IActionResult>
{
    private readonly IProxyService _proxyService;

    public GetEvidenceDashboardQueryHandler(IProxyService proxyService)
    {
        _proxyService = proxyService;
    }

    public Task<IActionResult> Handle(GetEvidenceDashboardQuery request, CancellationToken cancellationToken)
    {
        var endpoint = $"/api/v{request.ApiVersion}/evidence/dashboard?frameworkId={request.FrameworkId}";
        return _proxyService.ProxyToComplianceServiceAsync(endpoint, HttpMethod.Get, null, null, cancellationToken);
    }
}

public sealed class GetEvidenceUploadContextQueryHandler : IRequestHandler<GetEvidenceUploadContextQuery, IActionResult>
{
    private readonly IProxyService _proxyService;

    public GetEvidenceUploadContextQueryHandler(IProxyService proxyService)
    {
        _proxyService = proxyService;
    }

    public Task<IActionResult> Handle(GetEvidenceUploadContextQuery request, CancellationToken cancellationToken)
    {
        var endpoint = $"/api/v{request.ApiVersion}/evidence/upload-context?frameworkId={request.FrameworkId}";
        return _proxyService.ProxyToComplianceServiceAsync(endpoint, HttpMethod.Get, null, null, cancellationToken);
    }
}

public sealed class GetLibraryEvidenceDetailQueryHandler : IRequestHandler<GetLibraryEvidenceDetailQuery, IActionResult>
{
    private readonly IProxyService _proxyService;

    public GetLibraryEvidenceDetailQueryHandler(IProxyService proxyService)
    {
        _proxyService = proxyService;
    }

    public Task<IActionResult> Handle(GetLibraryEvidenceDetailQuery request, CancellationToken cancellationToken)
    {
        var endpoint =
            $"/api/v{request.ApiVersion}/evidence/{request.EvidenceId}/detail?frameworkId={request.FrameworkId}";
        return _proxyService.ProxyToComplianceServiceAsync(endpoint, HttpMethod.Get, null, null, cancellationToken);
    }
}

public sealed class GetLibraryEvidenceDownloadQueryHandler : IRequestHandler<GetLibraryEvidenceDownloadQuery, IActionResult>
{
    private readonly IProxyService _proxyService;

    public GetLibraryEvidenceDownloadQueryHandler(IProxyService proxyService)
    {
        _proxyService = proxyService;
    }

    public Task<IActionResult> Handle(GetLibraryEvidenceDownloadQuery request, CancellationToken cancellationToken)
    {
        var endpoint =
            $"/api/v{request.ApiVersion}/evidence/{request.EvidenceId}/download?frameworkId={request.FrameworkId}";
        return _proxyService.ProxyComplianceServiceStreamGetAsync(endpoint, cancellationToken);
    }
}

public sealed class PostEvidenceApprovalCommandHandler : IRequestHandler<PostEvidenceApprovalCommand, IActionResult>
{
    private readonly IProxyService _proxyService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public PostEvidenceApprovalCommandHandler(IProxyService proxyService, IHttpContextAccessor httpContextAccessor)
    {
        _proxyService = proxyService;
        _httpContextAccessor = httpContextAccessor;
    }

    public Task<IActionResult> Handle(PostEvidenceApprovalCommand request, CancellationToken cancellationToken)
    {
        var traceId = _httpContextAccessor.HttpContext?.TraceIdentifier;
        if (request.EvidenceId == Guid.Empty || request.Body is null)
        {
            IActionResult bad = ApiResponseFactory.FailureResult<object>(ApiResponseCode.VALIDATION_FAILED, traceId);
            return Task.FromResult(bad);
        }

        var endpoint = $"/api/v{request.ApiVersion}/evidence/{request.EvidenceId}/approval";
        return _proxyService.ProxyToComplianceServiceAsync(endpoint, HttpMethod.Post, request.Body, null, cancellationToken);
    }
}

public sealed class GetEvidenceReviewQueueQueryHandler : IRequestHandler<GetEvidenceReviewQueueQuery, IActionResult>
{
    private readonly IProxyService _proxyService;

    public GetEvidenceReviewQueueQueryHandler(IProxyService proxyService)
    {
        _proxyService = proxyService;
    }

    public Task<IActionResult> Handle(GetEvidenceReviewQueueQuery request, CancellationToken cancellationToken)
    {
        var q = new List<string> { $"frameworkId={request.FrameworkId}", $"page={request.Page}", $"pageSize={request.PageSize}" };
        if (!string.IsNullOrWhiteSpace(request.Status)) q.Add($"status={Uri.EscapeDataString(request.Status)}");
        if (!string.IsNullOrWhiteSpace(request.Stage)) q.Add($"stage={Uri.EscapeDataString(request.Stage)}");
        if (request.DepartmentId is { } d && d != Guid.Empty) q.Add($"departmentId={d}");
        if (request.OwnerId is { } o && o != Guid.Empty) q.Add($"ownerId={o}");
        if (!string.IsNullOrWhiteSpace(request.EvidenceType)) q.Add($"evidenceType={Uri.EscapeDataString(request.EvidenceType)}");
        if (!string.IsNullOrWhiteSpace(request.Search)) q.Add($"search={Uri.EscapeDataString(request.Search)}");
        var endpoint = $"/api/v{request.ApiVersion}/evidence/review-queue?{string.Join("&", q)}";
        return _proxyService.ProxyToComplianceServiceAsync(endpoint, HttpMethod.Get, null, null, cancellationToken);
    }
}

public sealed class GetEvidenceAuditTrailQueryHandler : IRequestHandler<GetEvidenceAuditTrailQuery, IActionResult>
{
    private readonly IProxyService _proxyService;

    public GetEvidenceAuditTrailQueryHandler(IProxyService proxyService)
    {
        _proxyService = proxyService;
    }

    public Task<IActionResult> Handle(GetEvidenceAuditTrailQuery request, CancellationToken cancellationToken)
    {
        var endpoint =
            $"/api/v{request.ApiVersion}/evidence/{request.EvidenceId}/audit-trail?frameworkId={request.FrameworkId}";
        if (request.ObjectiveElementId is { } oe && oe != Guid.Empty)
        {
            endpoint += $"&objectiveElementId={oe}";
        }

        return _proxyService.ProxyToComplianceServiceAsync(endpoint, HttpMethod.Get, null, null, cancellationToken);
    }
}
