using Gateway.Application.Queries;
using Gateway.DTOs;
using Gateway.Services;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Text.Json;

namespace Gateway.Application.Handlers;

/// <summary>
/// Handler for GetJourneySummaryQuery that proxies to ComplianceService.
/// </summary>
public class GetJourneySummaryQueryHandler : IRequestHandler<GetJourneySummaryQuery, JourneySummaryDto?>
{
    private readonly IProxyService _proxyService;
    private readonly ILogger<GetJourneySummaryQueryHandler> _logger;

    public GetJourneySummaryQueryHandler(
        IProxyService proxyService,
        ILogger<GetJourneySummaryQueryHandler> logger)
    {
        _proxyService = proxyService;
        _logger = logger;
    }

    public async Task<JourneySummaryDto?> Handle(GetJourneySummaryQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Retrieving journey summary for hospital {HospitalId}, framework {FrameworkId}", 
            request.HospitalId, request.FrameworkId);

        try
        {
            // Build endpoint with optional frameworkId query parameter
            var endpoint = $"/api/v{request.ApiVersion}/journeys/{request.HospitalId}/journey-summary";
            if (request.FrameworkId.HasValue)
            {
                endpoint += $"?frameworkId={request.FrameworkId.Value}";
            }
            
            var result = await _proxyService.ProxyToComplianceServiceAsync(
                endpoint: endpoint,
                httpMethod: HttpMethod.Get,
                requestBody: null,
                cancellationToken: cancellationToken);

            // Handle ContentResult (normal response with content)
            if (result is ContentResult contentResult)
            {
                if (contentResult.StatusCode == (int)HttpStatusCode.NotFound)
                {
                    _logger.LogInformation("Journey summary not found for hospital {HospitalId}. Returning default summary.", request.HospitalId);
                    // Return default summary with zeros instead of null
                    return new JourneySummaryDto
                    {
                        TotalFrameworks = 0,
                        Approved = 0,
                        ExpiringIn90Days = 0,
                        Suspended = 0,
                        RiskIndex = 0
                    };
                }

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
                        contentResult.StatusCode, request.HospitalId, contentResult.Content);
                    throw new InvalidOperationException($"ComplianceService error: {contentResult.Content ?? "Internal server error"}");
                }

                if (contentResult.StatusCode == (int)HttpStatusCode.OK)
                {
                    if (string.IsNullOrEmpty(contentResult.Content))
                    {
                        _logger.LogWarning("ComplianceService returned OK but with empty content for hospital {HospitalId}. Returning default summary.", request.HospitalId);
                        // Return default summary with zeros instead of null
                        return new JourneySummaryDto
                        {
                            TotalFrameworks = 0,
                            Approved = 0,
                            ExpiringIn90Days = 0,
                            Suspended = 0,
                            RiskIndex = 0
                        };
                    }

                    try
                    {
                        // ComplianceService returns an envelope with Data/Success/Error, so we need to deserialize it
                        var apiResponse = JsonSerializer.Deserialize<ComplianceServiceApiResponse<JourneySummaryDto>>(
                            contentResult.Content,
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                        if (apiResponse?.Data == null)
                        {
                            _logger.LogWarning("Failed to deserialize journey summary data for hospital {HospitalId}. Content: {Content}. Returning default summary.", 
                                request.HospitalId, contentResult.Content);
                            // Return default summary with zeros instead of null
                            return new JourneySummaryDto
                            {
                                TotalFrameworks = 0,
                                Approved = 0,
                                ExpiringIn90Days = 0,
                                Suspended = 0,
                                RiskIndex = 0
                            };
                        }

                        _logger.LogInformation("Successfully retrieved journey summary for hospital {HospitalId}", request.HospitalId);
                        return apiResponse.Data;
                    }
                    catch (JsonException jsonEx)
                    {
                        _logger.LogError(jsonEx, 
                            "JSON deserialization error for hospital {HospitalId}. Content: {Content}", 
                            request.HospitalId, contentResult.Content);
                        throw new InvalidOperationException($"Failed to parse response from ComplianceService: {jsonEx.Message}");
                    }
                }
            }

            // Handle StatusCodeResult (response without content)
            if (result is StatusCodeResult statusCodeResult)
            {
                if (statusCodeResult.StatusCode == (int)HttpStatusCode.NotFound)
                {
                    _logger.LogInformation("Journey summary not found for hospital {HospitalId}. Returning default summary.", request.HospitalId);
                    // Return default summary with zeros instead of null
                    return new JourneySummaryDto
                    {
                        TotalFrameworks = 0,
                        Approved = 0,
                        ExpiringIn90Days = 0,
                        Suspended = 0,
                        RiskIndex = 0
                    };
                }

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

            // Handle ObjectResult (error responses from ProxyService)
            if (result is ObjectResult objectResult)
            {
                _logger.LogError(
                    "ProxyService returned ObjectResult with status {StatusCode} for hospital {HospitalId}. Value: {Value}",
                    objectResult.StatusCode, request.HospitalId, 
                    objectResult.Value != null ? JsonSerializer.Serialize(objectResult.Value) : "null");

                if (objectResult.StatusCode == (int)HttpStatusCode.BadGateway)
                {
                    throw new InvalidOperationException("ComplianceService is unavailable. Please try again later.");
                }

                if (objectResult.StatusCode >= 500)
                {
                    var errorMessage = "Internal server error";
                    if (objectResult.Value != null)
                    {
                        var errorObj = objectResult.Value.ToString();
                        errorMessage = errorObj ?? "Internal server error";
                    }
                    throw new InvalidOperationException($"ComplianceService error: {errorMessage}");
                }
            }

            _logger.LogWarning(
                "Unexpected response type from ComplianceService for hospital {HospitalId}. Response type: {ResponseType}, ToString: {ToString}",
                request.HospitalId, result?.GetType().Name ?? "null", result?.ToString() ?? "null");
            throw new InvalidOperationException($"Unexpected response from ComplianceService. Response type: {result?.GetType().Name ?? "null"}");
        }
        catch (UnauthorizedAccessException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving journey summary for hospital {HospitalId}", request.HospitalId);
            throw;
        }
    }
}

/// <summary>
/// Lightweight wrapper for ApiResponse from ComplianceService used only for proxy deserialization.
/// </summary>
internal class ComplianceServiceApiResponse<T>
{
    public T? Data { get; set; }
    public bool Success { get; set; }
    public object? Error { get; set; }
}
