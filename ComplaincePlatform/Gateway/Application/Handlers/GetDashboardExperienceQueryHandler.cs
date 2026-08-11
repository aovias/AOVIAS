using Gateway.Application.Queries;
using Gateway.Resilience;
using Gateway.Services;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Platform.Dashboard.Models;
using Shared.Infrastructure.Api;
using Shared.Infrastructure.DTO;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Gateway.Application.Handlers;

public class GetDashboardExperienceQueryHandler : IRequestHandler<GetDashboardExperienceQuery, DashboardExperienceResult>
{
    private readonly IProxyService _proxyService;
    private readonly ILogger<GetDashboardExperienceQueryHandler> _logger;

    /// <summary>
    /// Must match ComplianceService JSON options (camelCase string enums).
    /// Without JsonStringEnumConverter, widget/section phase values fail to deserialize.
    /// </summary>
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: true) },
    };

    public GetDashboardExperienceQueryHandler(
        IProxyService proxyService,
        ILogger<GetDashboardExperienceQueryHandler> logger)
    {
        _proxyService = proxyService;
        _logger = logger;
    }

    public async Task<DashboardExperienceResult> Handle(
        GetDashboardExperienceQuery request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Retrieving platform dashboard experience for hospital {HospitalId}",
            request.HospitalId);

        var query = new List<string> { $"hospitalId={request.HospitalId}" };
        if (request.FrameworkId.HasValue && request.FrameworkId.Value != Guid.Empty)
        {
            query.Add($"frameworkId={request.FrameworkId.Value}");
        }

        if (request.ForceRefresh)
        {
            query.Add("forceRefresh=true");
        }

        var endpoint = $"/api/v1/platform/dashboard/experience?{string.Join('&', query)}";

        var result = await _proxyService.ProxyToComplianceServiceAsync(
            endpoint: endpoint,
            httpMethod: HttpMethod.Get,
            requestBody: null,
            cancellationToken: cancellationToken);

        if (result is ContentResult contentResult)
        {
            if (contentResult.StatusCode == (int)HttpStatusCode.Unauthorized)
            {
                throw new UnauthorizedAccessException("Unauthorized access to ComplianceService");
            }

            if (contentResult.StatusCode == (int)HttpStatusCode.Forbidden)
            {
                throw new UnauthorizedAccessException("Hospital access denied");
            }

            if (contentResult.StatusCode == (int)HttpStatusCode.OK && !string.IsNullOrEmpty(contentResult.Content))
            {
                var apiResponse = JsonSerializer.Deserialize<ComplianceServiceApiResponse<DashboardExperienceResult>>(
                    contentResult.Content,
                    JsonOptions);

                if (apiResponse?.Data is null)
                {
                    _logger.LogWarning(
                        "Empty platform dashboard experience for hospital {HospitalId}",
                        request.HospitalId);
                    return new DashboardExperienceResult();
                }

                return apiResponse.Data;
            }

            if (contentResult.StatusCode >= 500)
            {
                // Read-path enrichment: prefer empty shell over failing the whole UX.
                return GracefulDegradation.FromFallback(
                    new DashboardExperienceResult(),
                    _logger,
                    "dashboard-experience-downstream",
                    new InvalidOperationException(
                        $"ComplianceService error: {contentResult.Content ?? "Internal server error"}"));
            }
        }

        if (result is ObjectResult objectResult)
        {
            if (objectResult.StatusCode == (int)HttpStatusCode.Unauthorized
                || objectResult.StatusCode == (int)HttpStatusCode.Forbidden)
            {
                throw new UnauthorizedAccessException("Unauthorized access to ComplianceService");
            }

            // Proxy outage / timeout envelopes — degrade dashboard reads rather than 502 the shell.
            if (objectResult.StatusCode is >= 500 and < 600)
            {
                var message = ExtractProxyFailureMessage(objectResult.Value)
                    ?? "ComplianceService is unavailable. Please try again later.";
                return GracefulDegradation.FromFallback(
                    new DashboardExperienceResult(),
                    _logger,
                    "dashboard-experience-proxy",
                    new HttpRequestException(message));
            }
        }

        if (result is StatusCodeResult statusCodeResult)
        {
            if (statusCodeResult.StatusCode == (int)HttpStatusCode.Unauthorized)
            {
                throw new UnauthorizedAccessException("Unauthorized access to ComplianceService");
            }

            if (statusCodeResult.StatusCode == (int)HttpStatusCode.Forbidden)
            {
                throw new UnauthorizedAccessException("Hospital access denied");
            }

            if (statusCodeResult.StatusCode >= 500)
            {
                return GracefulDegradation.FromFallback(
                    new DashboardExperienceResult(),
                    _logger,
                    "dashboard-experience-status",
                    new HttpRequestException("ComplianceService internal server error"));
            }
        }

        throw new InvalidOperationException("Unexpected response from ComplianceService.");
    }

    private static string? ExtractProxyFailureMessage(object? value)
    {
        if (value is ApiResponse<object> envelope && !string.IsNullOrWhiteSpace(envelope.Message))
        {
            return envelope.Message;
        }

        return null;
    }
}
