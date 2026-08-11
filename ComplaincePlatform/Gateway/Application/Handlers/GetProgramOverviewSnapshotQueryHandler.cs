using Gateway.Application.Queries;
using Gateway.DTOs;
using Gateway.Services;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Shared.Infrastructure.DTO;
using System.Net;
using System.Text.Json;

namespace Gateway.Application.Handlers;

/// <summary>
/// Handler for GetProgramOverviewSnapshotQuery that proxies to ComplianceService (standardized ApiResponse envelope).
/// </summary>
public class GetProgramOverviewSnapshotQueryHandler : IRequestHandler<GetProgramOverviewSnapshotQuery, DashboardSnapshotDto>
{
    private readonly IProxyService _proxyService;
    private readonly ILogger<GetProgramOverviewSnapshotQueryHandler> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public GetProgramOverviewSnapshotQueryHandler(
        IProxyService proxyService,
        ILogger<GetProgramOverviewSnapshotQueryHandler> logger)
    {
        _proxyService = proxyService;
        _logger = logger;
    }

    public async Task<DashboardSnapshotDto> Handle(GetProgramOverviewSnapshotQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Retrieving program overview snapshot for hospital {HospitalId}, framework {FrameworkId}",
            request.HospitalId,
            request.FrameworkId);

        try
        {
            var endpoint = $"/api/v1/compliance/snapshots/hospital/{request.HospitalId}";
            if (request.FrameworkId.HasValue)
            {
                endpoint += $"?frameworkId={request.FrameworkId.Value}";
            }

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

                if (contentResult.StatusCode >= 500)
                {
                    _logger.LogError(
                        "ComplianceService returned error {StatusCode} for hospital {HospitalId}. Response: {Response}",
                        contentResult.StatusCode,
                        request.HospitalId,
                        contentResult.Content);
                    throw new InvalidOperationException(
                        $"ComplianceService error: {contentResult.Content ?? "Internal server error"}");
                }

                if (contentResult.StatusCode == (int)HttpStatusCode.OK)
                {
                    if (string.IsNullOrEmpty(contentResult.Content))
                    {
                        _logger.LogWarning(
                            "ComplianceService returned OK but with empty content for hospital {HospitalId}",
                            request.HospitalId);
                        return new DashboardSnapshotDto { IsSnapshotAvailable = false };
                    }

                    var envelope = JsonSerializer.Deserialize<ApiResponse<DashboardSnapshotDto>>(
                        contentResult.Content,
                        JsonOptions);

                    if (envelope == null)
                    {
                        throw new InvalidOperationException("Failed to parse response from ComplianceService.");
                    }

                    if (!envelope.Success || envelope.Data == null)
                    {
                        _logger.LogError(
                            "ComplianceService returned unsuccessful snapshot response for hospital {HospitalId}",
                            request.HospitalId);
                        throw new InvalidOperationException(
                            envelope.Message ?? "ComplianceService did not return snapshot data.");
                    }

                    _logger.LogInformation(
                        "Successfully retrieved program overview snapshot for hospital {HospitalId}",
                        request.HospitalId);

                    return envelope.Data;
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
                    throw new InvalidOperationException("ComplianceService internal server error");
                }
            }

            if (result is ObjectResult objectResult)
            {
                _logger.LogError(
                    "ProxyService returned ObjectResult with status {StatusCode} for hospital {HospitalId}. Value: {Value}",
                    objectResult.StatusCode,
                    request.HospitalId,
                    objectResult.Value != null ? JsonSerializer.Serialize(objectResult.Value) : "null");

                if (objectResult.StatusCode == (int)HttpStatusCode.Unauthorized
                    || objectResult.StatusCode == (int)HttpStatusCode.Forbidden)
                {
                    throw new UnauthorizedAccessException("Unauthorized access to ComplianceService");
                }

                // Proxy outage / timeout envelopes — preserve as service unavailable (not 500).
                if (objectResult.StatusCode is >= 500 and < 600)
                {
                    throw new HttpRequestException("ComplianceService is unavailable. Please try again later.");
                }
            }

            _logger.LogWarning(
                "Unexpected response type from ComplianceService for hospital {HospitalId}. Response type: {ResponseType}",
                request.HospitalId,
                result?.GetType().Name ?? "null");
            throw new InvalidOperationException(
                $"Unexpected response from ComplianceService. Response type: {result?.GetType().Name ?? "null"}");
        }
        catch (UnauthorizedAccessException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving program overview snapshot for hospital {HospitalId}", request.HospitalId);
            throw;
        }
    }
}
