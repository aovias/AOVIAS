using Gateway.Application.Queries;
using Gateway.DTOs;
using Gateway.Services;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Text.Json;

namespace Gateway.Application.Handlers;

/// <summary>
/// Handler for GetDashboardSummaryQuery that proxies to ComplianceService.
/// </summary>
public class GetDashboardSummaryQueryHandler : IRequestHandler<GetDashboardSummaryQuery, DashboardSummaryDto>
{
    private readonly IProxyService _proxyService;
    private readonly ILogger<GetDashboardSummaryQueryHandler> _logger;

    public GetDashboardSummaryQueryHandler(
        IProxyService proxyService,
        ILogger<GetDashboardSummaryQueryHandler> logger)
    {
        _proxyService = proxyService;
        _logger = logger;
    }

    public async Task<DashboardSummaryDto> Handle(GetDashboardSummaryQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Retrieving dashboard summary for hospital {HospitalId}", request.HospitalId);

        try
        {
            var endpoint = $"/api/v1/dashboard/summary?hospitalId={request.HospitalId}";

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
                    try
                    {
                        var apiResponse = JsonSerializer.Deserialize<ComplianceServiceApiResponse<BackendDashboardSummaryDto>>(
                            contentResult.Content,
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                        if (apiResponse?.Data == null)
                        {
            _logger.LogWarning("Failed to deserialize dashboard summary data for hospital {HospitalId}. Content: {Content}", 
                request.HospitalId, contentResult.Content);
            return new DashboardSummaryDto();
                        }

                        var data = apiResponse.Data;

                        var summary = new DashboardSummaryDto
                        {
                            HospitalName = "Hospital",
                            ReadinessPercentage = data.ComplianceScore,
                            TotalTasks = data.TotalTasks,
                            CriticalPending = data.OpenAuditFindings,
                            UpcomingAudits = data.UpcomingRenewals
                        };

                        _logger.LogInformation("Successfully retrieved dashboard summary for hospital {HospitalId}", request.HospitalId);
                        return summary;
                    }
                    catch (JsonException jsonEx)
                    {
                        _logger.LogError(jsonEx,
                            "JSON deserialization error for dashboard summary for hospital {HospitalId}. Content: {Content}",
                            request.HospitalId, contentResult.Content);
                        throw new InvalidOperationException($"Failed to parse response from ComplianceService: {jsonEx.Message}");
                    }
                }

                if (contentResult.StatusCode >= 500)
                {
                    _logger.LogError(
                        "ComplianceService returned error {StatusCode} for hospital {HospitalId}. Response: {Response}",
                        contentResult.StatusCode, request.HospitalId, contentResult.Content);
                    throw new InvalidOperationException($"ComplianceService error: {contentResult.Content ?? "Internal server error"}");
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
                    _logger.LogError(
                        "ComplianceService returned error {StatusCode} for hospital {HospitalId}",
                        statusCodeResult.StatusCode, request.HospitalId);
                    throw new InvalidOperationException("ComplianceService internal server error");
                }
            }

            throw new InvalidOperationException("Unexpected response from ComplianceService.");
        }
        catch (UnauthorizedAccessException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving dashboard summary for hospital {HospitalId}", request.HospitalId);
            throw;
        }
    }
}

internal class BackendDashboardSummaryDto
{
    public Guid HospitalId { get; set; }
    public decimal ComplianceScore { get; set; }
    public int TotalTasks { get; set; }
    public int CompletedTasks { get; set; }
    public int PendingTasks { get; set; }
    public int OpenAuditFindings { get; set; }
    public int UpcomingRenewals { get; set; }
}
