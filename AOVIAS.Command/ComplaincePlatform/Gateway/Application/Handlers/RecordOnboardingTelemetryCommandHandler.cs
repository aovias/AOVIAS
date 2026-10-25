using System.Security.Claims;
using Gateway.Application.Commands;
using Gateway.Services;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Shared.Infrastructure.Api;

namespace Gateway.Application.Handlers;

public sealed class RecordOnboardingTelemetryCommandHandler
    : IRequestHandler<RecordOnboardingTelemetryCommand, IActionResult>
{
    private readonly IProxyService _proxy;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<RecordOnboardingTelemetryCommandHandler> _logger;

    public RecordOnboardingTelemetryCommandHandler(
        IProxyService proxy,
        IHttpContextAccessor httpContextAccessor,
        ILogger<RecordOnboardingTelemetryCommandHandler> logger)
    {
        _proxy = proxy;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task<IActionResult> Handle(RecordOnboardingTelemetryCommand request, CancellationToken cancellationToken)
    {
        var http = _httpContextAccessor.HttpContext;
        var traceId = http?.TraceIdentifier ?? Guid.NewGuid().ToString("N");
        var user = http?.User;

        var orgFromClaim = ParseGuid(user?.FindFirst("organization_id")?.Value)
                           ?? ParseGuid(user?.FindFirst("organizationId")?.Value);
        var orgId = request.Body.OrganizationId is { } oid && oid != Guid.Empty ? oid : orgFromClaim;
        if (orgId is null || orgId == Guid.Empty)
        {
            return ApiResponseFactory.FailureResult<object>(ApiResponseCode.VALIDATION_FAILED, traceId);
        }

        var userId = ParseGuid(user?.FindFirst(ClaimTypes.NameIdentifier)?.Value)
                     ?? ParseGuid(user?.FindFirst("sub")?.Value);
        var hospitalId = request.Body.HospitalId
                         ?? ParseGuid(user?.FindFirst("hospital_id")?.Value)
                         ?? ParseGuid(user?.FindFirst("hospitalId")?.Value);

        _logger.LogInformation(
            "OnboardingTelemetryGateway {EventType} Org={OrganizationId} Stage={Stage} CorrelationId={CorrelationId}",
            request.Body.EventType,
            orgId,
            request.Body.Stage,
            traceId);

        return await _proxy.ProxyToOrganizationServiceAsync(
            "/internal/onboarding/events",
            HttpMethod.Post,
            new
            {
                OrganizationId = orgId,
                UserId = userId,
                HospitalId = hospitalId,
                EventType = request.Body.EventType,
                Stage = request.Body.Stage,
                CorrelationId = traceId,
                PayloadJson = request.Body.PayloadJson
            },
            cancellationToken);
    }

    private static Guid? ParseGuid(string? value)
        => Guid.TryParse(value, out var g) ? g : null;
}
