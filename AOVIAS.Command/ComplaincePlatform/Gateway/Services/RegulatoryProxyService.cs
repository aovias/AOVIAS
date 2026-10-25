using System.Net;
using System.Text.Json;
using Gateway.DTOs;
using Microsoft.AspNetCore.Mvc;
using Shared.Infrastructure.DTO;

namespace Gateway.Services;

/// <summary>
/// Implementation of IRegulatoryProxyService that calls ComplianceService via IProxyService.
/// All cross-service messages are standard ApiResponse&lt;T&gt; payloads.
/// </summary>
public class RegulatoryProxyService : IRegulatoryProxyService
{
    private readonly IProxyService _proxyService;
    private readonly ILogger<RegulatoryProxyService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public RegulatoryProxyService(
        IProxyService proxyService,
        ILogger<RegulatoryProxyService> logger)
    {
        _proxyService = proxyService;
        _logger = logger;
    }

    public Task<ApiResponse<RegulatoryOverviewDto>> GetRegulatoryOverviewAsync(
        Guid orgId,
        string traceId,
        CancellationToken cancellationToken)
    {
        // Route in ComplianceService: ComplianceIntelligenceController
        // [Route("api/v{version:apiVersion}/organizations")]
        // [HttpGet("{orgId:guid}/regulatory-overview")]
        var endpoint = $"/api/v1/organizations/{orgId}/regulatory-overview";
        return ProxyAndDeserializeAsync<RegulatoryOverviewDto>(endpoint, HttpMethod.Get, null, traceId, null, cancellationToken);
    }

    public Task<ApiResponse<UpdateJurisdictionResponseDto>> UpdateJurisdictionAsync(
        Guid orgId,
        UpdateJurisdictionRequestDto request,
        string traceId,
        CancellationToken cancellationToken)
    {
        // Route in ComplianceService: ComplianceIntelligenceController
        // [Route("api/v{version:apiVersion}/organizations")]
        // [HttpPut("{orgId:guid}/jurisdiction")]
        var endpoint = $"/api/v1/organizations/{orgId}/jurisdiction";
        return ProxyAndDeserializeAsync<UpdateJurisdictionResponseDto>(endpoint, HttpMethod.Put, request, traceId, null, cancellationToken);
    }

    public Task<ApiResponse<IReadOnlyList<AccreditationSummaryDto>>> GetAccreditationsAsync(
        Guid orgId,
        string traceId,
        CancellationToken cancellationToken)
    {
        // Route in ComplianceService: OrganizationAccreditationsController
        // [Route("api/v{version:apiVersion}/organizations")]
        // [HttpGet("{orgId:guid}/accreditations")]
        var endpoint = $"/api/v1/organizations/{orgId}/accreditations";
        return ProxyAndDeserializeAsync<IReadOnlyList<AccreditationSummaryDto>>(endpoint, HttpMethod.Get, null, traceId, null, cancellationToken);
    }

    public Task<ApiResponse<IReadOnlyList<RenewalDto>>> GetRenewalsAsync(
        Guid orgId,
        int withinDays,
        string traceId,
        CancellationToken cancellationToken)
    {
        // Route in ComplianceService: OrganizationAccreditationsController
        // [Route("api/v{version:apiVersion}/organizations")]
        // [HttpGet("{orgId:guid}/renewals")]
        var endpoint = $"/api/v1/organizations/{orgId}/renewals?withinDays={withinDays}";
        return ProxyAndDeserializeAsync<IReadOnlyList<RenewalDto>>(endpoint, HttpMethod.Get, null, traceId, null, cancellationToken);
    }

    public Task<ApiResponse<FrameworkCatalogPageDto>> GetFrameworkCatalogAsync(
        Guid orgId,
        string? country,
        string? accreditationBody,
        string? type,
        string? version,
        int page,
        int pageSize,
        string? search,
        string traceId,
        CancellationToken cancellationToken)
    {
        var query = new List<string>
        {
            $"page={page}",
            $"pageSize={pageSize}"
        };

        if (!string.IsNullOrWhiteSpace(country)) query.Add($"country={Uri.EscapeDataString(country)}");
        if (!string.IsNullOrWhiteSpace(accreditationBody)) query.Add($"accreditationBody={Uri.EscapeDataString(accreditationBody)}");
        if (!string.IsNullOrWhiteSpace(type)) query.Add($"type={Uri.EscapeDataString(type)}");
        if (!string.IsNullOrWhiteSpace(version)) query.Add($"version={Uri.EscapeDataString(version)}");
        if (!string.IsNullOrWhiteSpace(search)) query.Add($"search={Uri.EscapeDataString(search)}");

        // Route in ComplianceService: ComplianceIntelligenceController
        // [Route("api/v{version:apiVersion}/organizations")]
        // [HttpGet("{orgId:guid}/frameworks/catalog")]
        var endpoint = $"/api/v1/organizations/{orgId}/frameworks/catalog?{string.Join("&", query)}";
        return ProxyAndDeserializeAsync<FrameworkCatalogPageDto>(endpoint, HttpMethod.Get, null, traceId, null, cancellationToken);
    }

    public Task<ApiResponse<ActivationMetadataDto>> GetActivationMetadataAsync(
        Guid orgId,
        Guid frameworkId,
        string traceId,
        CancellationToken cancellationToken)
    {
        var endpoint = $"/api/v1/compliance/organizations/{orgId}/frameworks/{frameworkId}/activation-metadata";
        return ProxyAndDeserializeAsync<ActivationMetadataDto>(endpoint, HttpMethod.Get, null, traceId, null, cancellationToken);
    }

    public Task<ApiResponse<ActivateAccreditationResponseDto>> ActivateAccreditationAsync(
        Guid orgId,
        ActivateAccreditationRequestDto request,
        string traceId,
        string? idempotencyKey = null,
        CancellationToken cancellationToken = default)
    {
        var endpoint = $"/api/v1/compliance/organizations/{orgId}/accreditations";
        
        // Include Idempotency-Key header if provided to prevent duplicate activations
        Dictionary<string, string>? headers = null;
        if (!string.IsNullOrWhiteSpace(idempotencyKey))
        {
            headers = new Dictionary<string, string>
            {
                { "Idempotency-Key", idempotencyKey }
            };
        }
        
        return ProxyAndDeserializeAsync<ActivateAccreditationResponseDto>(
            endpoint, 
            HttpMethod.Post, 
            request, 
            traceId, 
            headers, 
            cancellationToken);
    }

    public Task<ApiResponse<AccreditationLifecycleChangeResponseDto>> SuspendAccreditationAsync(
        Guid orgId,
        Guid accreditationId,
        SuspendAccreditationRequestDto request,
        string traceId,
        string? ifMatch = null,
        CancellationToken cancellationToken = default)
    {
        var endpoint = $"/api/v1/compliance/organizations/{orgId}/accreditations/{accreditationId}/suspend";
        
        // Include If-Match header for optimistic concurrency control
        Dictionary<string, string>? headers = null;
        if (!string.IsNullOrWhiteSpace(ifMatch))
        {
            headers = new Dictionary<string, string>
            {
                { "If-Match", ifMatch }
            };
        }
        
        return ProxyAndDeserializeAsync<AccreditationLifecycleChangeResponseDto>(
            endpoint, 
            HttpMethod.Patch, 
            request, 
            traceId, 
            headers, 
            cancellationToken);
    }

    public Task<ApiResponse<AccreditationLifecycleChangeResponseDto>> ArchiveAccreditationAsync(
        Guid orgId,
        Guid accreditationId,
        string traceId,
        string? ifMatch = null,
        CancellationToken cancellationToken = default)
    {
        var endpoint = $"/api/v1/compliance/organizations/{orgId}/accreditations/{accreditationId}/archive";
        
        // Include If-Match header for optimistic concurrency control
        Dictionary<string, string>? headers = null;
        if (!string.IsNullOrWhiteSpace(ifMatch))
        {
            headers = new Dictionary<string, string>
            {
                { "If-Match", ifMatch }
            };
        }
        
        return ProxyAndDeserializeAsync<AccreditationLifecycleChangeResponseDto>(
            endpoint, 
            HttpMethod.Patch, 
            null, 
            traceId, 
            headers, 
            cancellationToken);
    }

    public Task<ApiResponse<RiskRecalculationResponseDto>> RecalculateRiskAsync(
        Guid orgId,
        string traceId,
        string? idempotencyKey = null,
        CancellationToken cancellationToken = default)
    {
        var endpoint = $"/api/v1/compliance/organizations/{orgId}/risk/recalculate";
        
        // Include Idempotency-Key header to prevent duplicate job scheduling
        // Downstream service should check if job with this key already exists
        Dictionary<string, string>? headers = null;
        if (!string.IsNullOrWhiteSpace(idempotencyKey))
        {
            headers = new Dictionary<string, string>
            {
                { "Idempotency-Key", idempotencyKey }
            };
        }
        
        return ProxyAndDeserializeAsync<RiskRecalculationResponseDto>(
            endpoint, 
            HttpMethod.Post, 
            null, 
            traceId, 
            headers, 
            cancellationToken);
    }

    /// <summary>
    /// Generic helper to proxy to ComplianceService and deserialize standardized ApiResponse&lt;T&gt;.
    /// Ensures traceId is propagated as X-Correlation-Id header for distributed tracing.
    /// </summary>
    private async Task<ApiResponse<T>> ProxyAndDeserializeAsync<T>(
        string endpoint,
        HttpMethod method,
        object? body,
        string traceId,
        Dictionary<string, string>? additionalHeaders = null,
        CancellationToken cancellationToken = default)
    {
        // Ensure traceId is propagated as X-Correlation-Id header for distributed tracing
        // This allows tracing requests across microservices boundaries
        var headers = additionalHeaders ?? new Dictionary<string, string>();
        if (!headers.ContainsKey("X-Correlation-Id") && !headers.ContainsKey("x-correlation-id"))
        {
            headers["X-Correlation-Id"] = traceId;
        }
        else if (string.IsNullOrWhiteSpace(headers.ContainsKey("X-Correlation-Id") ? headers["X-Correlation-Id"] : headers["x-correlation-id"]))
        {
            // Override empty correlation ID with traceId
            headers["X-Correlation-Id"] = traceId;
        }

        var result = await _proxyService.ProxyToComplianceServiceAsync(
            endpoint: endpoint,
            httpMethod: method,
            requestBody: body,
            additionalHeaders: headers,
            cancellationToken: cancellationToken);

        if (result is ContentResult contentResult)
        {
            var statusCode = contentResult.StatusCode ?? (int)HttpStatusCode.OK;

            try
            {
                var apiResponse = JsonSerializer.Deserialize<ApiResponse<T>>(contentResult.Content ?? string.Empty, JsonOptions);
                if (apiResponse == null)
                {
                    _logger.LogError("Failed to deserialize ApiResponse from ComplianceService. Endpoint: {Endpoint}, Status: {StatusCode}", endpoint, statusCode);
                    return ApiResponse<T>.ErrorResponse(
                        ApiError.InternalServerError("Failed to parse response from ComplianceService."),
                        traceId);
                }

                // Map HTTP status to ApiError.Status when not already populated.
                if (!apiResponse.Success && apiResponse.Error != null && apiResponse.Error.Status == 0)
                {
                    apiResponse.Error.Status = statusCode;
                }

                // If backend incorrectly reports success but non-2xx HTTP, normalize to error.
                if (statusCode >= 400 && apiResponse.Success)
                {
                    apiResponse.Success = false;
                    apiResponse.Error ??= new ApiError
                    {
                        Code = "UPSTREAM_ERROR",
                        Message = "Upstream service reported success with error HTTP status.",
                        Status = statusCode
                    };
                    apiResponse.Data = default;
                }

                apiResponse.TraceId = apiResponse.TraceId ?? traceId;
                return apiResponse;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex,
                    "JSON deserialization error for ComplianceService response. Endpoint: {Endpoint}, Status: {StatusCode}, Content: {Content}",
                    endpoint, statusCode, contentResult.Content);

                return ApiResponse<T>.ErrorResponse(
                    ApiError.InternalServerError("Failed to parse response from ComplianceService."),
                    traceId);
            }
        }

        if (result is ObjectResult objectResult)
        {
            var status = objectResult.StatusCode ?? (int)HttpStatusCode.BadGateway;
            _logger.LogError(
                "ProxyService returned ObjectResult for ComplianceService. Endpoint: {Endpoint}, Status: {Status}, Value: {Value}",
                endpoint, status, objectResult.Value);

            return ApiResponse<T>.ErrorResponse(
                new ApiError
                {
                    Code = "UPSTREAM_UNAVAILABLE",
                    Message = "ComplianceService is unavailable. Please try again later.",
                    Status = status,
                    Details = objectResult.Value?.ToString()
                },
                traceId);
        }

        _logger.LogError("Unexpected response type from ComplianceService proxy. Endpoint: {Endpoint}, Type: {Type}",
            endpoint, result?.GetType().Name ?? "null");

        return ApiResponse<T>.ErrorResponse(
            ApiError.InternalServerError("Unexpected response from ComplianceService."),
            traceId);
    }
}

