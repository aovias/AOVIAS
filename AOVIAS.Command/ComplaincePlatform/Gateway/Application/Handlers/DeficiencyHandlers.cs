using Gateway.Application.Queries;
using Gateway.Services;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Shared.Infrastructure.Api;

namespace Gateway.Application.Handlers;

public class GetDeficienciesQueryHandler
    : IRequestHandler<GetDeficienciesQuery, IActionResult>
{
    private readonly IProxyService _proxyService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public GetDeficienciesQueryHandler(
        IProxyService proxyService,
        IHttpContextAccessor httpContextAccessor)
    {
        _proxyService        = proxyService;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<IActionResult> Handle(
        GetDeficienciesQuery request,
        CancellationToken cancellationToken)
    {
        var traceId = _httpContextAccessor.HttpContext?.TraceIdentifier;
        if (request.HospitalId == Guid.Empty || request.FrameworkId == Guid.Empty)
            return ApiResponseFactory.FailureResult<object>(ApiResponseCode.VALIDATION_FAILED, traceId);

        var qs = $"hospitalId={request.HospitalId}&frameworkId={request.FrameworkId}" +
                 $"&page={request.Page}&pageSize={request.PageSize}" +
                 (request.Status   != null ? $"&status={request.Status}"     : "") +
                 (request.Severity != null ? $"&severity={request.Severity}" : "");

        var endpoint = $"/internal/v1/deficiencies?{qs}";
        return await _proxyService.ProxyToComplianceServiceAsync(endpoint, HttpMethod.Get, null, null, cancellationToken);
    }
}

public class GetDeficiencyByIdQueryHandler
    : IRequestHandler<GetDeficiencyByIdQuery, IActionResult>
{
    private readonly IProxyService _proxyService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public GetDeficiencyByIdQueryHandler(
        IProxyService proxyService,
        IHttpContextAccessor httpContextAccessor)
    {
        _proxyService        = proxyService;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<IActionResult> Handle(
        GetDeficiencyByIdQuery request,
        CancellationToken cancellationToken)
    {
        var traceId = _httpContextAccessor.HttpContext?.TraceIdentifier;
        if (request.DeficiencyId == Guid.Empty)
            return ApiResponseFactory.FailureResult<object>(ApiResponseCode.VALIDATION_FAILED, traceId);

        var endpoint = $"/internal/v1/deficiencies/{request.DeficiencyId}";
        return await _proxyService.ProxyToComplianceServiceAsync(endpoint, HttpMethod.Get, null, null, cancellationToken);
    }
}

public class GetDeficiencySummaryQueryHandler
    : IRequestHandler<GetDeficiencySummaryQuery, IActionResult>
{
    private readonly IProxyService _proxyService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public GetDeficiencySummaryQueryHandler(
        IProxyService proxyService,
        IHttpContextAccessor httpContextAccessor)
    {
        _proxyService        = proxyService;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<IActionResult> Handle(
        GetDeficiencySummaryQuery request,
        CancellationToken cancellationToken)
    {
        var traceId = _httpContextAccessor.HttpContext?.TraceIdentifier;
        if (request.HospitalId == Guid.Empty || request.FrameworkId == Guid.Empty)
            return ApiResponseFactory.FailureResult<object>(ApiResponseCode.VALIDATION_FAILED, traceId);

        var endpoint = $"/internal/v1/deficiencies/summary?hospitalId={request.HospitalId}&frameworkId={request.FrameworkId}";
        return await _proxyService.ProxyToComplianceServiceAsync(endpoint, HttpMethod.Get, null, null, cancellationToken);
    }
}

public class GetDeficienciesByStageQueryHandler
    : IRequestHandler<GetDeficienciesByStageQuery, IActionResult>
{
    private readonly IProxyService _proxyService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public GetDeficienciesByStageQueryHandler(
        IProxyService proxyService,
        IHttpContextAccessor httpContextAccessor)
    {
        _proxyService        = proxyService;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<IActionResult> Handle(
        GetDeficienciesByStageQuery request,
        CancellationToken cancellationToken)
    {
        var traceId = _httpContextAccessor.HttpContext?.TraceIdentifier;
        if (request.HospitalId == Guid.Empty || request.FrameworkId == Guid.Empty)
            return ApiResponseFactory.FailureResult<object>(ApiResponseCode.VALIDATION_FAILED, traceId);

        var endpoint = $"/internal/v1/deficiencies/by-stage?hospitalId={request.HospitalId}&frameworkId={request.FrameworkId}";
        return await _proxyService.ProxyToComplianceServiceAsync(endpoint, HttpMethod.Get, null, null, cancellationToken);
    }
}

public class PatchDeficiencyStatusCommandHandler
    : IRequestHandler<PatchDeficiencyStatusCommand, IActionResult>
{
    private readonly IProxyService _proxyService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public PatchDeficiencyStatusCommandHandler(
        IProxyService proxyService,
        IHttpContextAccessor httpContextAccessor)
    {
        _proxyService        = proxyService;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<IActionResult> Handle(
        PatchDeficiencyStatusCommand request,
        CancellationToken cancellationToken)
    {
        var traceId = _httpContextAccessor.HttpContext?.TraceIdentifier;
        if (request.DeficiencyId == Guid.Empty)
            return ApiResponseFactory.FailureResult<object>(ApiResponseCode.VALIDATION_FAILED, traceId);

        var endpoint = $"/internal/v1/deficiencies/{request.DeficiencyId}/status";
        return await _proxyService.ProxyToComplianceServiceAsync(endpoint, HttpMethod.Patch, request.Body, null, cancellationToken);
    }
}

public class AssignDeficiencyCommandHandler
    : IRequestHandler<AssignDeficiencyCommand, IActionResult>
{
    private readonly IProxyService _proxyService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AssignDeficiencyCommandHandler(
        IProxyService proxyService,
        IHttpContextAccessor httpContextAccessor)
    {
        _proxyService        = proxyService;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<IActionResult> Handle(
        AssignDeficiencyCommand request,
        CancellationToken cancellationToken)
    {
        var traceId = _httpContextAccessor.HttpContext?.TraceIdentifier;
        if (request.DeficiencyId == Guid.Empty)
            return ApiResponseFactory.FailureResult<object>(ApiResponseCode.VALIDATION_FAILED, traceId);

        var endpoint = $"/internal/v1/deficiencies/{request.DeficiencyId}/assign";
        return await _proxyService.ProxyToComplianceServiceAsync(endpoint, HttpMethod.Post, request.Body, null, cancellationToken);
    }
}

public class AcceptDeficiencyGatewayCommandHandler
    : IRequestHandler<AcceptDeficiencyGatewayCommand, IActionResult>
{
    private readonly IProxyService _proxyService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AcceptDeficiencyGatewayCommandHandler(
        IProxyService proxyService,
        IHttpContextAccessor httpContextAccessor)
    {
        _proxyService = proxyService;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<IActionResult> Handle(
        AcceptDeficiencyGatewayCommand request,
        CancellationToken cancellationToken)
    {
        var traceId = _httpContextAccessor.HttpContext?.TraceIdentifier;
        if (request.DeficiencyId == Guid.Empty)
            return ApiResponseFactory.FailureResult<object>(ApiResponseCode.VALIDATION_FAILED, traceId);

        var endpoint = $"/internal/v1/deficiencies/{request.DeficiencyId}/accept";
        return await _proxyService.ProxyToComplianceServiceAsync(
            endpoint, HttpMethod.Post, request.Body ?? new { }, null, cancellationToken);
    }
}

public class RejectDeficiencyOwnershipGatewayCommandHandler
    : IRequestHandler<RejectDeficiencyOwnershipGatewayCommand, IActionResult>
{
    private readonly IProxyService _proxyService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public RejectDeficiencyOwnershipGatewayCommandHandler(
        IProxyService proxyService,
        IHttpContextAccessor httpContextAccessor)
    {
        _proxyService = proxyService;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<IActionResult> Handle(
        RejectDeficiencyOwnershipGatewayCommand request,
        CancellationToken cancellationToken)
    {
        var traceId = _httpContextAccessor.HttpContext?.TraceIdentifier;
        if (request.DeficiencyId == Guid.Empty)
            return ApiResponseFactory.FailureResult<object>(ApiResponseCode.VALIDATION_FAILED, traceId);

        var endpoint = $"/internal/v1/deficiencies/{request.DeficiencyId}/reject-ownership";
        return await _proxyService.ProxyToComplianceServiceAsync(
            endpoint, HttpMethod.Post, request.Body ?? new { }, null, cancellationToken);
    }
}

public class BootstrapDeficienciesFromOnboardingCommandHandler
    : IRequestHandler<BootstrapDeficienciesFromOnboardingCommand, IActionResult>
{
    private readonly IProxyService _proxyService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public BootstrapDeficienciesFromOnboardingCommandHandler(
        IProxyService proxyService,
        IHttpContextAccessor httpContextAccessor)
    {
        _proxyService        = proxyService;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<IActionResult> Handle(
        BootstrapDeficienciesFromOnboardingCommand request,
        CancellationToken cancellationToken)
    {
        var traceId = _httpContextAccessor.HttpContext?.TraceIdentifier;
        if (request.OrganizationId == Guid.Empty
         || request.HospitalId == Guid.Empty
         || request.FrameworkId == Guid.Empty)
            return ApiResponseFactory.FailureResult<object>(ApiResponseCode.VALIDATION_FAILED, traceId);

        var body = new
        {
            organizationId = request.OrganizationId,
            hospitalId = request.HospitalId,
            frameworkId = request.FrameworkId,
        };

        var endpoint = "/internal/v1/obligations/apply-onboarding-assessment";
        return await _proxyService.ProxyToComplianceServiceAsync(
            endpoint, HttpMethod.Post, body, null, cancellationToken);
    }
}

public class GetDeficiencyCommandCenterDashboardQueryHandler
    : IRequestHandler<GetDeficiencyCommandCenterDashboardQuery, IActionResult>
{
    private readonly IProxyService _proxyService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public GetDeficiencyCommandCenterDashboardQueryHandler(IProxyService proxyService, IHttpContextAccessor httpContextAccessor)
    {
        _proxyService = proxyService;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<IActionResult> Handle(GetDeficiencyCommandCenterDashboardQuery request, CancellationToken cancellationToken)
    {
        var traceId = _httpContextAccessor.HttpContext?.TraceIdentifier;
        if (request.HospitalId == Guid.Empty || request.FrameworkId == Guid.Empty)
            return ApiResponseFactory.FailureResult<object>(ApiResponseCode.VALIDATION_FAILED, traceId);

        var endpoint = $"/internal/v1/deficiencies/command-center/dashboard?hospitalId={request.HospitalId}&frameworkId={request.FrameworkId}";
        return await _proxyService.ProxyToComplianceServiceAsync(endpoint, HttpMethod.Get, null, null, cancellationToken);
    }
}

public class GetDeficiencyWorkspaceQueryHandler
    : IRequestHandler<GetDeficiencyWorkspaceQuery, IActionResult>
{
    private readonly IProxyService _proxyService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public GetDeficiencyWorkspaceQueryHandler(IProxyService proxyService, IHttpContextAccessor httpContextAccessor)
    {
        _proxyService = proxyService;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<IActionResult> Handle(GetDeficiencyWorkspaceQuery request, CancellationToken cancellationToken)
    {
        var traceId = _httpContextAccessor.HttpContext?.TraceIdentifier;
        if (request.HospitalId == Guid.Empty || request.FrameworkId == Guid.Empty)
            return ApiResponseFactory.FailureResult<object>(ApiResponseCode.VALIDATION_FAILED, traceId);

        var qs = $"hospitalId={request.HospitalId}&frameworkId={request.FrameworkId}" +
                 $"&page={request.Page}&pageSize={request.PageSize}&sortDesc={request.SortDesc.ToString().ToLowerInvariant()}" +
                 (request.Status != null ? $"&status={request.Status}" : "") +
                 (request.Severity != null ? $"&severity={request.Severity}" : "") +
                 (request.MaturityLevel != null ? $"&maturityLevel={request.MaturityLevel}" : "") +
                 (request.Search != null ? $"&search={Uri.EscapeDataString(request.Search)}" : "") +
                 (request.ObjectiveElementId.HasValue ? $"&objectiveElementId={request.ObjectiveElementId}" : "") +
                 (request.SortBy != null ? $"&sortBy={request.SortBy}" : "");

        var endpoint = $"/internal/v1/deficiencies/command-center/workspace?{qs}";
        return await _proxyService.ProxyToComplianceServiceAsync(endpoint, HttpMethod.Get, null, null, cancellationToken);
    }
}

public class GetDeficiencyWorkspaceDetailQueryHandler
    : IRequestHandler<GetDeficiencyWorkspaceDetailQuery, IActionResult>
{
    private readonly IProxyService _proxyService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public GetDeficiencyWorkspaceDetailQueryHandler(IProxyService proxyService, IHttpContextAccessor httpContextAccessor)
    {
        _proxyService = proxyService;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<IActionResult> Handle(GetDeficiencyWorkspaceDetailQuery request, CancellationToken cancellationToken)
    {
        var traceId = _httpContextAccessor.HttpContext?.TraceIdentifier;
        if (request.DeficiencyId == Guid.Empty)
            return ApiResponseFactory.FailureResult<object>(ApiResponseCode.VALIDATION_FAILED, traceId);

        var endpoint = $"/internal/v1/deficiencies/command-center/workspace/{request.DeficiencyId}";
        return await _proxyService.ProxyToComplianceServiceAsync(endpoint, HttpMethod.Get, null, null, cancellationToken);
    }
}

public class PatchDeficiencyRootCauseCommandHandler
    : IRequestHandler<PatchDeficiencyRootCauseCommand, IActionResult>
{
    private readonly IProxyService _proxyService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public PatchDeficiencyRootCauseCommandHandler(IProxyService proxyService, IHttpContextAccessor httpContextAccessor)
    {
        _proxyService = proxyService;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<IActionResult> Handle(PatchDeficiencyRootCauseCommand request, CancellationToken cancellationToken)
    {
        var traceId = _httpContextAccessor.HttpContext?.TraceIdentifier;
        if (request.DeficiencyId == Guid.Empty)
            return ApiResponseFactory.FailureResult<object>(ApiResponseCode.VALIDATION_FAILED, traceId);

        var endpoint = $"/internal/v1/deficiencies/{request.DeficiencyId}/root-cause";
        return await _proxyService.ProxyToComplianceServiceAsync(endpoint, HttpMethod.Patch, request.Body, null, cancellationToken);
    }
}

public class PatchDeficiencyRcaCommandHandler
    : IRequestHandler<PatchDeficiencyRcaCommand, IActionResult>
{
    private readonly IProxyService _proxyService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public PatchDeficiencyRcaCommandHandler(IProxyService proxyService, IHttpContextAccessor httpContextAccessor)
    {
        _proxyService = proxyService;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<IActionResult> Handle(PatchDeficiencyRcaCommand request, CancellationToken cancellationToken)
    {
        var traceId = _httpContextAccessor.HttpContext?.TraceIdentifier;
        if (request.DeficiencyId == Guid.Empty)
            return ApiResponseFactory.FailureResult<object>(ApiResponseCode.VALIDATION_FAILED, traceId);

        var endpoint = $"/internal/v1/deficiencies/{request.DeficiencyId}/rca";
        return await _proxyService.ProxyToComplianceServiceAsync(endpoint, HttpMethod.Patch, request.Body, null, cancellationToken);
    }
}

public class PatchDeficiencyRemediationCommandHandler
    : IRequestHandler<PatchDeficiencyRemediationCommand, IActionResult>
{
    private readonly IProxyService _proxyService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public PatchDeficiencyRemediationCommandHandler(IProxyService proxyService, IHttpContextAccessor httpContextAccessor)
    {
        _proxyService = proxyService;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<IActionResult> Handle(PatchDeficiencyRemediationCommand request, CancellationToken cancellationToken)
    {
        var traceId = _httpContextAccessor.HttpContext?.TraceIdentifier;
        if (request.DeficiencyId == Guid.Empty)
            return ApiResponseFactory.FailureResult<object>(ApiResponseCode.VALIDATION_FAILED, traceId);

        var endpoint = $"/internal/v1/deficiencies/{request.DeficiencyId}/remediation";
        return await _proxyService.ProxyToComplianceServiceAsync(endpoint, HttpMethod.Patch, request.Body, null, cancellationToken);
    }
}

public class PatchDeficiencyCorrectiveActionCommandHandler
    : IRequestHandler<PatchDeficiencyCorrectiveActionCommand, IActionResult>
{
    private readonly IProxyService _proxyService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public PatchDeficiencyCorrectiveActionCommandHandler(IProxyService proxyService, IHttpContextAccessor httpContextAccessor)
    {
        _proxyService = proxyService;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<IActionResult> Handle(PatchDeficiencyCorrectiveActionCommand request, CancellationToken cancellationToken)
    {
        var traceId = _httpContextAccessor.HttpContext?.TraceIdentifier;
        if (request.DeficiencyId == Guid.Empty)
            return ApiResponseFactory.FailureResult<object>(ApiResponseCode.VALIDATION_FAILED, traceId);

        var endpoint = $"/internal/v1/deficiencies/{request.DeficiencyId}/corrective-action";
        return await _proxyService.ProxyToComplianceServiceAsync(endpoint, HttpMethod.Patch, request.Body, null, cancellationToken);
    }
}

public class PatchDeficiencyPreventiveActionCommandHandler
    : IRequestHandler<PatchDeficiencyPreventiveActionCommand, IActionResult>
{
    private readonly IProxyService _proxyService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public PatchDeficiencyPreventiveActionCommandHandler(IProxyService proxyService, IHttpContextAccessor httpContextAccessor)
    {
        _proxyService = proxyService;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<IActionResult> Handle(PatchDeficiencyPreventiveActionCommand request, CancellationToken cancellationToken)
    {
        var traceId = _httpContextAccessor.HttpContext?.TraceIdentifier;
        if (request.DeficiencyId == Guid.Empty)
            return ApiResponseFactory.FailureResult<object>(ApiResponseCode.VALIDATION_FAILED, traceId);

        var endpoint = $"/internal/v1/deficiencies/{request.DeficiencyId}/preventive-action";
        return await _proxyService.ProxyToComplianceServiceAsync(endpoint, HttpMethod.Patch, request.Body, null, cancellationToken);
    }
}

public class PatchDeficiencyEffectivenessReviewCommandHandler
    : IRequestHandler<PatchDeficiencyEffectivenessReviewCommand, IActionResult>
{
    private readonly IProxyService _proxyService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public PatchDeficiencyEffectivenessReviewCommandHandler(IProxyService proxyService, IHttpContextAccessor httpContextAccessor)
    {
        _proxyService = proxyService;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<IActionResult> Handle(PatchDeficiencyEffectivenessReviewCommand request, CancellationToken cancellationToken)
    {
        var traceId = _httpContextAccessor.HttpContext?.TraceIdentifier;
        if (request.DeficiencyId == Guid.Empty)
            return ApiResponseFactory.FailureResult<object>(ApiResponseCode.VALIDATION_FAILED, traceId);

        var endpoint = $"/internal/v1/deficiencies/{request.DeficiencyId}/effectiveness-review";
        return await _proxyService.ProxyToComplianceServiceAsync(endpoint, HttpMethod.Patch, request.Body, null, cancellationToken);
    }
}

public class GetDeficiencyCapaQueryHandler
    : IRequestHandler<GetDeficiencyCapaQuery, IActionResult>
{
    private readonly IProxyService _proxyService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public GetDeficiencyCapaQueryHandler(IProxyService proxyService, IHttpContextAccessor httpContextAccessor)
    {
        _proxyService = proxyService;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<IActionResult> Handle(GetDeficiencyCapaQuery request, CancellationToken cancellationToken)
    {
        var traceId = _httpContextAccessor.HttpContext?.TraceIdentifier;
        if (request.DeficiencyId == Guid.Empty)
            return ApiResponseFactory.FailureResult<object>(ApiResponseCode.VALIDATION_FAILED, traceId);

        var endpoint = $"/internal/v1/deficiencies/{request.DeficiencyId}/capa";
        return await _proxyService.ProxyToComplianceServiceAsync(endpoint, HttpMethod.Get, null, null, cancellationToken);
    }
}
