using Microsoft.AspNetCore.Mvc;
using Shared.Infrastructure.Execution;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Gateway.Services;

/// <summary>
/// Implementation of IProxyService for proxying requests to backend microservices.
/// </summary>
public class ProxyService : IProxyService
{
    private static readonly JsonSerializerOptions ProxyRequestJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false) },
    };

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<ProxyService> _logger;

    public ProxyService(
        IHttpClientFactory httpClientFactory,
        IHttpContextAccessor httpContextAccessor,
        ILogger<ProxyService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task<IActionResult> ProxyToIdentityServiceAsync(
        string endpoint,
        HttpMethod httpMethod,
        object? requestBody = null,
        CancellationToken cancellationToken = default)
    {
        return await ProxyRequestAsync(
            clientName: "IdentityService",
            endpoint: endpoint,
            httpMethod: httpMethod,
            requestBody: requestBody,
            forwardAuthHeader: false,
            cancellationToken: cancellationToken);
    }

    public async Task<IActionResult> ProxyToComplianceServiceAsync(
        string endpoint,
        HttpMethod httpMethod,
        object? requestBody = null,
        Dictionary<string, string>? additionalHeaders = null,
        CancellationToken cancellationToken = default)
    {
        return await ProxyRequestAsync(
            clientName: "ComplianceService",
            endpoint: endpoint,
            httpMethod: httpMethod,
            requestBody: requestBody,
            forwardAuthHeader: true,
            additionalHeaders: additionalHeaders,
            cancellationToken: cancellationToken);
    }

    public Task<IActionResult> ProxyToComplianceServiceRawAsync(
        string endpoint,
        HttpMethod httpMethod,
        HttpContent? content,
        Dictionary<string, string>? additionalHeaders = null,
        CancellationToken cancellationToken = default)
    {
        return ProxyRequestRawAsync(
            clientName: "ComplianceService",
            endpoint: endpoint,
            httpMethod: httpMethod,
            content: content,
            forwardAuthHeader: true,
            additionalHeaders: additionalHeaders,
            cancellationToken: cancellationToken);
    }

    public async Task<IActionResult> ProxyToOrganizationServiceAsync(
        string endpoint,
        HttpMethod httpMethod,
        object? requestBody = null,
        CancellationToken cancellationToken = default)
    {
        return await ProxyRequestAsync(
            clientName: "OrganizationClient",
            endpoint: endpoint,
            httpMethod: httpMethod,
            requestBody: requestBody,
            forwardAuthHeader: true,
            cancellationToken: cancellationToken);
    }

    public Task<IActionResult> ProxyToOrganizationServiceRawAsync(
        string endpoint,
        HttpMethod httpMethod,
        HttpContent? content,
        CancellationToken cancellationToken = default)
    {
        return ProxyRequestRawAsync(
            clientName: "OrganizationClient",
            endpoint: endpoint,
            httpMethod: httpMethod,
            content: content,
            forwardAuthHeader: true,
            additionalHeaders: null,
            cancellationToken: cancellationToken);
    }

    public async Task<IActionResult> ProxyOrganizationServiceStreamGetAsync(string endpoint, CancellationToken cancellationToken = default)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            var meta = Shared.Infrastructure.Api.ApiResponseCodes.Registry[Shared.Infrastructure.Api.ApiResponseCode.INTERNAL_ERROR];
            var responseEnvelope = Shared.Infrastructure.Api.ApiResponseFactory.Failure<object>(
                Shared.Infrastructure.Api.ApiResponseCode.INTERNAL_ERROR,
                (string?)null);
            return new ObjectResult(responseEnvelope) { StatusCode = meta.HttpStatus };
        }

        var correlationId = GetOrCreateCorrelationId(null);
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var httpClient = _httpClientFactory.CreateClient("OrganizationClient");
            using var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
            request.Headers.Add("X-Correlation-Id", correlationId);
            request.Headers.Add("X-Correlation-ID", correlationId);

            var authHeader = httpContext.Request.Headers.Authorization.FirstOrDefault();
            if (!string.IsNullOrEmpty(authHeader))
            {
                request.Headers.Authorization = AuthenticationHeaderValue.Parse(authHeader);
            }

            AttachExecutionContext(request, httpContext, correlationId);

            using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            httpContext.Response.StatusCode = (int)response.StatusCode;

            var respCt = response.Content.Headers.ContentType;
            if (respCt != null)
            {
                httpContext.Response.ContentType = respCt.ToString();
            }

            await response.Content.CopyToAsync(httpContext.Response.Body, cancellationToken);
            stopwatch.Stop();
            _logger.LogInformation("Organization stream proxy {Endpoint} completed in {ElapsedMs}ms with status {StatusCode}", endpoint, stopwatch.ElapsedMilliseconds, (int)response.StatusCode);
            return new EmptyResult();
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Organization stream proxy failed for {Endpoint}", endpoint);
            var meta = Shared.Infrastructure.Api.ApiResponseCodes.Registry[Shared.Infrastructure.Api.ApiResponseCode.INTERNAL_ERROR];
            var responseEnvelope = Shared.Infrastructure.Api.ApiResponseFactory.Failure<object>(
                Shared.Infrastructure.Api.ApiResponseCode.INTERNAL_ERROR,
                correlationId);
            return new ObjectResult(responseEnvelope) { StatusCode = meta.HttpStatus };
        }
    }

    public async Task<IActionResult> ProxyToAIServiceAsync(
        string endpoint,
        HttpMethod httpMethod,
        object? requestBody = null,
        Dictionary<string, string>? additionalHeaders = null,
        CancellationToken cancellationToken = default)
    {
        return await ProxyRequestAsync(
            clientName: "AIService",
            endpoint: endpoint,
            httpMethod: httpMethod,
            requestBody: requestBody,
            forwardAuthHeader: true,
            additionalHeaders: additionalHeaders,
            cancellationToken: cancellationToken);
    }

    public async Task<IActionResult> ProxyToAuditServiceAsync(
        string endpoint,
        HttpMethod httpMethod,
        object? requestBody = null,
        CancellationToken cancellationToken = default)
    {
        return await ProxyRequestAsync(
            clientName: "AuditService",
            endpoint: endpoint,
            httpMethod: httpMethod,
            requestBody: requestBody,
            forwardAuthHeader: true,
            cancellationToken: cancellationToken);
    }

    public async Task<IActionResult> ProxyToKnowledgeBaseServiceAsync(
        string endpoint,
        HttpMethod httpMethod,
        object? requestBody = null,
        Dictionary<string, string>? additionalHeaders = null,
        CancellationToken cancellationToken = default)
    {
        return await ProxyRequestAsync(
            clientName: "KnowledgeBaseService",
            endpoint: endpoint,
            httpMethod: httpMethod,
            requestBody: requestBody,
            forwardAuthHeader: true,
            additionalHeaders: additionalHeaders,
            cancellationToken: cancellationToken);
    }

    public async Task<IActionResult> ProxyToPersonServiceAsync(
        string endpoint,
        HttpMethod httpMethod,
        object? requestBody = null,
        Dictionary<string, string>? additionalHeaders = null,
        CancellationToken cancellationToken = default)
    {
        return await ProxyRequestAsync(
            clientName: "PersonService",
            endpoint: endpoint,
            httpMethod: httpMethod,
            requestBody: requestBody,
            forwardAuthHeader: true,
            additionalHeaders: additionalHeaders,
            cancellationToken: cancellationToken);
    }

    public async Task<IActionResult> ProxyToWorkforceServiceAsync(
        string endpoint,
        HttpMethod httpMethod,
        object? requestBody = null,
        Dictionary<string, string>? additionalHeaders = null,
        CancellationToken cancellationToken = default)
    {
        return await ProxyRequestAsync(
            clientName: "WorkforceService",
            endpoint: endpoint,
            httpMethod: httpMethod,
            requestBody: requestBody,
            forwardAuthHeader: true,
            additionalHeaders: additionalHeaders,
            cancellationToken: cancellationToken);
    }

    public async Task<IActionResult> ProxyToProfessionalServiceAsync(
        string endpoint,
        HttpMethod httpMethod,
        object? requestBody = null,
        Dictionary<string, string>? additionalHeaders = null,
        CancellationToken cancellationToken = default)
    {
        return await ProxyRequestAsync(
            clientName: "ProfessionalService",
            endpoint: endpoint,
            httpMethod: httpMethod,
            requestBody: requestBody,
            forwardAuthHeader: true,
            additionalHeaders: additionalHeaders,
            cancellationToken: cancellationToken);
    }

    public async Task<IActionResult> ProxyToAccessGovernanceServiceAsync(
        string endpoint,
        HttpMethod httpMethod,
        object? requestBody = null,
        Dictionary<string, string>? additionalHeaders = null,
        CancellationToken cancellationToken = default)
    {
        return await ProxyRequestAsync(
            clientName: "AccessGovernanceService",
            endpoint: endpoint,
            httpMethod: httpMethod,
            requestBody: requestBody,
            forwardAuthHeader: true,
            additionalHeaders: additionalHeaders,
            cancellationToken: cancellationToken);
    }

    public async Task<IActionResult> ProxyToGovernanceServiceAsync(
        string endpoint,
        HttpMethod httpMethod,
        object? requestBody = null,
        Dictionary<string, string>? additionalHeaders = null,
        CancellationToken cancellationToken = default)
    {
        return await ProxyRequestAsync(
            clientName: "GovernanceService",
            endpoint: endpoint,
            httpMethod: httpMethod,
            requestBody: requestBody,
            forwardAuthHeader: true,
            additionalHeaders: additionalHeaders,
            cancellationToken: cancellationToken);
    }

    public async Task<IActionResult> ProxyToCommunicationServiceAsync(
        string endpoint,
        HttpMethod httpMethod,
        object? requestBody = null,
        Dictionary<string, string>? additionalHeaders = null,
        CancellationToken cancellationToken = default)
    {
        return await ProxyRequestAsync(
            clientName: "CommunicationService",
            endpoint: endpoint,
            httpMethod: httpMethod,
            requestBody: requestBody,
            forwardAuthHeader: true,
            additionalHeaders: additionalHeaders,
            cancellationToken: cancellationToken);
    }

    public async Task<IActionResult> ProxyToWorkforceIntelligenceServiceAsync(
        string endpoint,
        HttpMethod httpMethod,
        object? requestBody = null,
        Dictionary<string, string>? additionalHeaders = null,
        CancellationToken cancellationToken = default)
    {
        return await ProxyRequestAsync(
            clientName: "WorkforceIntelligenceService",
            endpoint: endpoint,
            httpMethod: httpMethod,
            requestBody: requestBody,
            forwardAuthHeader: true,
            additionalHeaders: additionalHeaders,
            cancellationToken: cancellationToken);
    }

    public async Task<IActionResult> ProxyToEnterpriseDirectoryServiceAsync(
        string endpoint,
        HttpMethod httpMethod,
        object? requestBody = null,
        Dictionary<string, string>? additionalHeaders = null,
        CancellationToken cancellationToken = default)
    {
        return await ProxyRequestAsync(
            clientName: "EnterpriseDirectoryService",
            endpoint: endpoint,
            httpMethod: httpMethod,
            requestBody: requestBody,
            forwardAuthHeader: true,
            additionalHeaders: additionalHeaders,
            cancellationToken: cancellationToken);
    }

    public async Task<IActionResult> ProxyToIntegrationServiceAsync(
        string endpoint,
        HttpMethod httpMethod,
        object? requestBody = null,
        Dictionary<string, string>? additionalHeaders = null,
        bool forwardAuthHeader = true,
        CancellationToken cancellationToken = default)
    {
        return await ProxyRequestAsync(
            clientName: "IntegrationService",
            endpoint: endpoint,
            httpMethod: httpMethod,
            requestBody: requestBody,
            forwardAuthHeader: forwardAuthHeader,
            additionalHeaders: additionalHeaders,
            cancellationToken: cancellationToken);
    }

    public async Task<IActionResult> ProxyRequestAsync(
        string clientName,
        string endpoint,
        HttpMethod httpMethod,
        object? requestBody = null,
        bool forwardAuthHeader = false,
        Dictionary<string, string>? additionalHeaders = null,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        
        // Priority order for correlation ID:
        // 1. X-Correlation-Id from additionalHeaders (explicitly provided, e.g., from traceId)
        // 2. X-Correlation-Id from incoming request headers
        // 3. HttpContext.TraceIdentifier (ASP.NET Core trace ID)
        // 4. Generate new GUID as fallback
        var correlationId = GetOrCreateCorrelationId(additionalHeaders);
        
        try
        {
            _logger.LogInformation(
                "[CorrelationId: {CorrelationId}] Proxying {Method} {Endpoint} to {ServiceName}",
                correlationId, httpMethod, endpoint, clientName);

            var httpClient = _httpClientFactory.CreateClient(clientName);
            
            var request = new HttpRequestMessage(httpMethod, endpoint);

            // Get HTTP context once
            var httpContext = _httpContextAccessor.HttpContext;

            // Add correlation-id header to outgoing request
            // This ensures distributed tracing works across microservices
            request.Headers.Add("X-Correlation-Id", correlationId);
            
            // Also set X-Correlation-ID (alternative header name for compatibility)
            request.Headers.Add("X-Correlation-ID", correlationId);

            // Add request body if provided (for POST, PUT, PATCH, and DELETE with body)
            if (requestBody != null && (httpMethod == HttpMethod.Post || 
                                       httpMethod == HttpMethod.Put || 
                                       httpMethod == HttpMethod.Patch ||
                                       httpMethod == HttpMethod.Delete))
            {
                var requestJson = SerializeProxyRequestBody(requestBody);
                request.Content = new StringContent(
                    requestJson,
                    Encoding.UTF8,
                    "application/json");
            }

            // Forward Authorization header if requested
            if (forwardAuthHeader && httpContext?.Request?.Headers != null)
            {
                var authHeader = httpContext.Request.Headers["Authorization"].FirstOrDefault();
                if (!string.IsNullOrEmpty(authHeader))
                {
                    request.Headers.Authorization = AuthenticationHeaderValue.Parse(authHeader);
                }
            }

            AttachExecutionContext(request, httpContext, correlationId);

            // Add additional headers if provided
            // Note: X-Correlation-Id is already set above, so we skip it here to avoid duplicates
            if (additionalHeaders != null)
            {
                foreach (var header in additionalHeaders)
                {
                    // Skip X-Correlation-Id variants as they're already set above
                    if (string.Equals(header.Key, "X-Correlation-Id", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(header.Key, "X-Correlation-ID", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }
                    AddForwardedHeader(request.Headers, header.Key, header.Value);
                }
            }

            var proxyOperation = string.Equals(clientName, "IdentityService", StringComparison.Ordinal)
                ? "IdentityProxy"
                : "BackendProxy";
            var refreshTokenFlow = endpoint.Contains("/refresh", StringComparison.OrdinalIgnoreCase);

            HttpResponseMessage response;
            try
            {
                response = await httpClient.SendAsync(request, cancellationToken);
            }
            catch (HttpRequestException ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, 
                    "[CorrelationId: {CorrelationId}] [Operation: {Operation}] [RefreshTokenFlow: {RefreshTokenFlow}] {ServiceName} is unreachable. Endpoint: {Endpoint}, BaseAddress: {BaseAddress}, Error: {Error}, Elapsed: {ElapsedMs}ms", 
                    correlationId, proxyOperation, refreshTokenFlow, clientName, endpoint, httpClient.BaseAddress, ex.Message, stopwatch.ElapsedMilliseconds);

                var meta = Shared.Infrastructure.Api.ApiResponseCodes.Registry[Shared.Infrastructure.Api.ApiResponseCode.SERVICE_UNAVAILABLE];
                var responseEnvelope = Shared.Infrastructure.Api.ApiResponseFactory.Failure<object>(
                    Shared.Infrastructure.Api.ApiResponseCode.SERVICE_UNAVAILABLE,
                    correlationId);

                return new ObjectResult(responseEnvelope)
                {
                    StatusCode = meta.HttpStatus
                };
            }
            catch (TaskCanceledException ex)
            {
                stopwatch.Stop();
                if (!cancellationToken.IsCancellationRequested)
                {
                    _logger.LogError(ex,
                        "HttpClient timeout calling {ServiceName}. [CorrelationId: {CorrelationId}] [Operation: {Operation}] [RefreshTokenFlow: {RefreshTokenFlow}] Endpoint: {Endpoint}, Elapsed: {ElapsedMs}ms",
                        clientName, correlationId, proxyOperation, refreshTokenFlow, endpoint, stopwatch.ElapsedMilliseconds);
                }
                else
                {
                    _logger.LogWarning(ex,
                        "Request canceled by client for {ServiceName}. [CorrelationId: {CorrelationId}] [Operation: {Operation}] Endpoint: {Endpoint}, Elapsed: {ElapsedMs}ms",
                        clientName, correlationId, proxyOperation, endpoint, stopwatch.ElapsedMilliseconds);
                }

                var meta = Shared.Infrastructure.Api.ApiResponseCodes.Registry[Shared.Infrastructure.Api.ApiResponseCode.SERVICE_TIMEOUT];
                var responseEnvelope = Shared.Infrastructure.Api.ApiResponseFactory.Failure<object>(
                    Shared.Infrastructure.Api.ApiResponseCode.SERVICE_TIMEOUT,
                    correlationId);

                return new ObjectResult(responseEnvelope)
                {
                    StatusCode = meta.HttpStatus
                };
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, 
                    "[CorrelationId: {CorrelationId}] [Operation: {Operation}] [RefreshTokenFlow: {RefreshTokenFlow}] Unexpected error calling {ServiceName}. Endpoint: {Endpoint}, Elapsed: {ElapsedMs}ms", 
                    correlationId, proxyOperation, refreshTokenFlow, clientName, endpoint, stopwatch.ElapsedMilliseconds);

                var meta = Shared.Infrastructure.Api.ApiResponseCodes.Registry[Shared.Infrastructure.Api.ApiResponseCode.SERVICE_COMMUNICATION_ERROR];
                var responseEnvelope = Shared.Infrastructure.Api.ApiResponseFactory.Failure<object>(
                    Shared.Infrastructure.Api.ApiResponseCode.SERVICE_COMMUNICATION_ERROR,
                    correlationId);

                return new ObjectResult(responseEnvelope)
                {
                    StatusCode = meta.HttpStatus
                };
            }

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var contentType = response.Content.Headers.ContentType?.MediaType ?? "application/json";

            stopwatch.Stop();
            _logger.LogInformation(
                "[CorrelationId: {CorrelationId}] {Method} {Endpoint} to {ServiceName} completed in {ElapsedMs}ms with status {StatusCode}",
                correlationId, httpMethod, endpoint, clientName, stopwatch.ElapsedMilliseconds, (int)response.StatusCode);

            // Return the same status code and content from the backend service
            return new ContentResult
            {
                Content = responseContent,
                ContentType = contentType,
                StatusCode = (int)response.StatusCode
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, 
                "[CorrelationId: {CorrelationId}] Unexpected error in proxy service. Service: {ServiceName}, Endpoint: {Endpoint}, Elapsed: {ElapsedMs}ms", 
                correlationId, clientName, endpoint, stopwatch.ElapsedMilliseconds);

            var meta = Shared.Infrastructure.Api.ApiResponseCodes.Registry[Shared.Infrastructure.Api.ApiResponseCode.INTERNAL_ERROR];
            var responseEnvelope = Shared.Infrastructure.Api.ApiResponseFactory.Failure<object>(
                Shared.Infrastructure.Api.ApiResponseCode.INTERNAL_ERROR,
                correlationId);

            return new ObjectResult(responseEnvelope)
            {
                StatusCode = meta.HttpStatus
            };
        }
    }

    /// <summary>
    /// Propagates the caller's tenant to a downstream service as execution-context headers.
    ///
    /// <para>
    /// Internal ComplianceService routes authenticate with an API key and carry no user principal,
    /// so <c>IUserContextService</c> finds no <c>organization_id</c> claim there and tenant scope
    /// resolves to nothing. That is why query filters could not be made mandatory: they would have
    /// matched zero rows on every internal call. Sending the tenant explicitly — derived from the
    /// caller's validated token, never from client input — is what makes fail-closed filtering
    /// safe. See ADR-002.
    /// </para>
    ///
    /// <para>
    /// Values are only ever written here; the edge strips inbound
    /// <see cref="ExecutionContextHeaders"/> so a client cannot assert its own tenant.
    /// </para>
    /// </summary>
    private static void AttachExecutionContext(
        HttpRequestMessage request,
        HttpContext? httpContext,
        string correlationId)
    {
        if (httpContext?.User?.Identity?.IsAuthenticated != true)
        {
            return;
        }

        var organizationId = httpContext.User.FindFirst("organization_id")?.Value
                             ?? httpContext.User.FindFirst("organizationId")?.Value
                             ?? httpContext.User.FindFirst("org_id")?.Value;

        if (!Guid.TryParse(organizationId, out var orgId) || orgId == Guid.Empty)
        {
            return;
        }

        request.Headers.Remove(ExecutionContextHeaders.OrganizationId);
        request.Headers.Add(ExecutionContextHeaders.OrganizationId, orgId.ToString());

        var hospitalId = httpContext.User.FindFirst("hospital_id")?.Value;
        if (Guid.TryParse(hospitalId, out var hospId) && hospId != Guid.Empty)
        {
            request.Headers.Remove(ExecutionContextHeaders.HospitalId);
            request.Headers.Add(ExecutionContextHeaders.HospitalId, hospId.ToString());
        }

        request.Headers.Remove(ExecutionContextHeaders.ExecutionMode);
        request.Headers.Add(ExecutionContextHeaders.ExecutionMode, nameof(ExecutionMode.InternalApi));
        request.Headers.Remove(ExecutionContextHeaders.Source);
        request.Headers.Add(ExecutionContextHeaders.Source, "Gateway");
        request.Headers.Remove(ExecutionContextHeaders.RequestId);
        request.Headers.Add(ExecutionContextHeaders.RequestId, correlationId);
    }

    /// <summary>
    /// Gets or creates a correlation ID for distributed tracing.
    /// Priority order:
    /// 1. X-Correlation-Id from additionalHeaders (explicitly provided traceId)
    /// 2. X-Correlation-Id from incoming request headers
    /// 3. HttpContext.TraceIdentifier (ASP.NET Core trace ID)
    /// 4. Generate new GUID as fallback
    /// </summary>
    private string GetOrCreateCorrelationId(Dictionary<string, string>? additionalHeaders = null)
    {
        // Priority 1: Check additionalHeaders first (explicitly provided traceId)
        if (additionalHeaders != null)
        {
            if (additionalHeaders.TryGetValue("X-Correlation-Id", out var headerCorrelationId) && !string.IsNullOrWhiteSpace(headerCorrelationId))
            {
                return headerCorrelationId;
            }
            if (additionalHeaders.TryGetValue("x-correlation-id", out var lowerCorrelationId) && !string.IsNullOrWhiteSpace(lowerCorrelationId))
            {
                return lowerCorrelationId;
            }
        }

        // Priority 2: Check incoming request headers
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext?.Request?.Headers != null)
        {
            var incomingCorrelationId = httpContext.Request.Headers["X-Correlation-Id"].FirstOrDefault()
                                     ?? httpContext.Request.Headers["X-Correlation-ID"].FirstOrDefault()
                                     ?? httpContext.Request.Headers["x-correlation-id"].FirstOrDefault();
            if (!string.IsNullOrEmpty(incomingCorrelationId))
            {
                return incomingCorrelationId;
            }
        }

        // Priority 3: Use ASP.NET Core TraceIdentifier (from HttpContext.TraceIdentifier)
        if (httpContext?.TraceIdentifier != null && !string.IsNullOrWhiteSpace(httpContext.TraceIdentifier))
        {
            return httpContext.TraceIdentifier;
        }

        // Priority 4: Generate new correlation ID as fallback
        return Guid.NewGuid().ToString();
    }

    /// <summary>
    /// Forwards a header to an outgoing <see cref="HttpRequestMessage"/>.
    /// Uses <see cref="HttpHeaders.TryAddWithoutValidation"/> for conditional headers that carry
    /// opaque values (e.g. base64 row versions) which are not valid <c>entity-tag</c> tokens for
    /// <see cref="HttpHeaders"/> strict parsing but are accepted by the Compliance API.
    /// </summary>
    private static void AddForwardedHeader(HttpRequestHeaders headers, string name, string value)
    {
        if (string.Equals(name, "If-Match", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(name, "If-None-Match", StringComparison.OrdinalIgnoreCase))
        {
            headers.TryAddWithoutValidation(name, value);
            return;
        }

        headers.Add(name, value);
    }

    private async Task<IActionResult> ProxyRequestRawAsync(
        string clientName,
        string endpoint,
        HttpMethod httpMethod,
        HttpContent? content,
        bool forwardAuthHeader,
        Dictionary<string, string>? additionalHeaders,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var correlationId = GetOrCreateCorrelationId(additionalHeaders);

        try
        {
            _logger.LogInformation(
                "[CorrelationId: {CorrelationId}] Proxying raw body {Method} {Endpoint} to {ServiceName}",
                correlationId, httpMethod, endpoint, clientName);

            var httpClient = _httpClientFactory.CreateClient(clientName);
            var request = new HttpRequestMessage(httpMethod, endpoint);
            request.Headers.Add("X-Correlation-Id", correlationId);
            request.Headers.Add("X-Correlation-ID", correlationId);

            if (content != null &&
                (httpMethod == HttpMethod.Post ||
                 httpMethod == HttpMethod.Put ||
                 httpMethod == HttpMethod.Patch))
            {
                request.Content = content;
            }

            var httpContext = _httpContextAccessor.HttpContext;
            if (forwardAuthHeader && httpContext?.Request?.Headers != null)
            {
                var authHeader = httpContext.Request.Headers["Authorization"].FirstOrDefault();
                if (!string.IsNullOrEmpty(authHeader))
                {
                    request.Headers.Authorization = AuthenticationHeaderValue.Parse(authHeader);
                }
            }

            AttachExecutionContext(request, httpContext, correlationId);

            if (additionalHeaders != null)
            {
                foreach (var header in additionalHeaders)
                {
                    if (string.Equals(header.Key, "X-Correlation-Id", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(header.Key, "X-Correlation-ID", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    AddForwardedHeader(request.Headers, header.Key, header.Value);
                }
            }

            HttpResponseMessage response;
            try
            {
                response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            }
            catch (HttpRequestException ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex,
                    "[CorrelationId: {CorrelationId}] {ServiceName} unreachable (raw). Endpoint: {Endpoint}",
                    correlationId, clientName, endpoint);

                var meta = Shared.Infrastructure.Api.ApiResponseCodes.Registry[Shared.Infrastructure.Api.ApiResponseCode.SERVICE_UNAVAILABLE];
                var responseEnvelope = Shared.Infrastructure.Api.ApiResponseFactory.Failure<object>(
                    Shared.Infrastructure.Api.ApiResponseCode.SERVICE_UNAVAILABLE,
                    correlationId);
                return new ObjectResult(responseEnvelope) { StatusCode = meta.HttpStatus };
            }
            catch (TaskCanceledException ex)
            {
                stopwatch.Stop();
                if (!cancellationToken.IsCancellationRequested)
                {
                    _logger.LogError(ex,
                        "HttpClient timeout calling {ServiceName}. [CorrelationId: {CorrelationId}] Endpoint: {Endpoint}, Elapsed: {ElapsedMs}ms",
                        clientName, correlationId, endpoint, stopwatch.ElapsedMilliseconds);
                }
                else
                {
                    _logger.LogWarning(ex,
                        "Request canceled by client for {ServiceName}. [CorrelationId: {CorrelationId}] Endpoint: {Endpoint}, Elapsed: {ElapsedMs}ms",
                        clientName, correlationId, endpoint, stopwatch.ElapsedMilliseconds);
                }

                var meta = Shared.Infrastructure.Api.ApiResponseCodes.Registry[Shared.Infrastructure.Api.ApiResponseCode.SERVICE_TIMEOUT];
                var responseEnvelope = Shared.Infrastructure.Api.ApiResponseFactory.Failure<object>(
                    Shared.Infrastructure.Api.ApiResponseCode.SERVICE_TIMEOUT,
                    correlationId);
                return new ObjectResult(responseEnvelope) { StatusCode = meta.HttpStatus };
            }

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var contentType = response.Content.Headers.ContentType?.MediaType ?? "application/json";
            stopwatch.Stop();
            _logger.LogInformation(
                "[CorrelationId: {CorrelationId}] Raw {Method} {Endpoint} completed in {ElapsedMs}ms with status {StatusCode}",
                correlationId, httpMethod, endpoint, stopwatch.ElapsedMilliseconds, (int)response.StatusCode);

            return new ContentResult
            {
                Content = responseContent,
                ContentType = contentType,
                StatusCode = (int)response.StatusCode
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex,
                "[CorrelationId: {CorrelationId}] Unexpected error in raw proxy. Service: {ServiceName}, Endpoint: {Endpoint}",
                correlationId, clientName, endpoint);

            var meta = Shared.Infrastructure.Api.ApiResponseCodes.Registry[Shared.Infrastructure.Api.ApiResponseCode.INTERNAL_ERROR];
            var responseEnvelope = Shared.Infrastructure.Api.ApiResponseFactory.Failure<object>(
                Shared.Infrastructure.Api.ApiResponseCode.INTERNAL_ERROR,
                correlationId);
            return new ObjectResult(responseEnvelope) { StatusCode = meta.HttpStatus };
        }
    }

    public async Task<IActionResult> ProxyComplianceServiceStreamGetAsync(string endpoint, CancellationToken cancellationToken = default)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            var meta = Shared.Infrastructure.Api.ApiResponseCodes.Registry[Shared.Infrastructure.Api.ApiResponseCode.INTERNAL_ERROR];
            var responseEnvelope = Shared.Infrastructure.Api.ApiResponseFactory.Failure<object>(
                Shared.Infrastructure.Api.ApiResponseCode.INTERNAL_ERROR,
                (string?)null);
            return new ObjectResult(responseEnvelope) { StatusCode = meta.HttpStatus };
        }

        var correlationId = GetOrCreateCorrelationId(null);
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var httpClient = _httpClientFactory.CreateClient("ComplianceService");
            using var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
            request.Headers.Add("X-Correlation-Id", correlationId);
            request.Headers.Add("X-Correlation-ID", correlationId);

            var authHeader = httpContext.Request.Headers.Authorization.FirstOrDefault();
            if (!string.IsNullOrEmpty(authHeader))
            {
                request.Headers.Authorization = AuthenticationHeaderValue.Parse(authHeader);
            }

            AttachExecutionContext(request, httpContext, correlationId);

            using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            httpContext.Response.StatusCode = (int)response.StatusCode;

            var respCt = response.Content.Headers.ContentType;
            if (respCt != null)
            {
                httpContext.Response.ContentType = respCt.ToString();
            }

            var cd = response.Content.Headers.ContentDisposition;
            if (cd != null)
            {
                httpContext.Response.Headers.Append("Content-Disposition", cd.ToString());
            }

            await response.Content.CopyToAsync(httpContext.Response.Body, cancellationToken);
            stopwatch.Stop();
            _logger.LogInformation(
                "[CorrelationId: {CorrelationId}] Stream GET {Endpoint} completed in {ElapsedMs}ms status {StatusCode}",
                correlationId, endpoint, stopwatch.ElapsedMilliseconds, (int)response.StatusCode);
            return new EmptyResult();
        }
        catch (HttpRequestException ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "[CorrelationId: {CorrelationId}] Compliance stream GET unreachable: {Endpoint}", correlationId, endpoint);
            var meta = Shared.Infrastructure.Api.ApiResponseCodes.Registry[Shared.Infrastructure.Api.ApiResponseCode.SERVICE_UNAVAILABLE];
            var responseEnvelope = Shared.Infrastructure.Api.ApiResponseFactory.Failure<object>(
                Shared.Infrastructure.Api.ApiResponseCode.SERVICE_UNAVAILABLE,
                correlationId);
            return new ObjectResult(responseEnvelope) { StatusCode = meta.HttpStatus };
        }
        catch (TaskCanceledException ex)
        {
            stopwatch.Stop();
            const string complianceServiceName = "ComplianceService";
            if (!cancellationToken.IsCancellationRequested)
            {
                _logger.LogError(ex,
                    "HttpClient timeout calling {ServiceName}. [CorrelationId: {CorrelationId}] Stream GET Endpoint: {Endpoint}, Elapsed: {ElapsedMs}ms",
                    complianceServiceName, correlationId, endpoint, stopwatch.ElapsedMilliseconds);
            }
            else
            {
                _logger.LogWarning(ex,
                    "Request canceled by client for {ServiceName}. [CorrelationId: {CorrelationId}] Stream GET Endpoint: {Endpoint}, Elapsed: {ElapsedMs}ms",
                    complianceServiceName, correlationId, endpoint, stopwatch.ElapsedMilliseconds);
            }

            var metaTimeout = Shared.Infrastructure.Api.ApiResponseCodes.Registry[Shared.Infrastructure.Api.ApiResponseCode.SERVICE_TIMEOUT];
            var responseEnvelopeTimeout = Shared.Infrastructure.Api.ApiResponseFactory.Failure<object>(
                Shared.Infrastructure.Api.ApiResponseCode.SERVICE_TIMEOUT,
                correlationId);
            return new ObjectResult(responseEnvelopeTimeout) { StatusCode = metaTimeout.HttpStatus };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "[CorrelationId: {CorrelationId}] Stream GET failed: {Endpoint}", correlationId, endpoint);
            var meta = Shared.Infrastructure.Api.ApiResponseCodes.Registry[Shared.Infrastructure.Api.ApiResponseCode.INTERNAL_ERROR];
            var responseEnvelope = Shared.Infrastructure.Api.ApiResponseFactory.Failure<object>(
                Shared.Infrastructure.Api.ApiResponseCode.INTERNAL_ERROR,
                correlationId);
            return new ObjectResult(responseEnvelope) { StatusCode = meta.HttpStatus };
        }
    }

    /// <summary>
    /// Proxy controllers often read the raw request body as a string and pass it through.
    /// Re-serializing that string would JSON-encode it again and break downstream model binding.
    /// </summary>
    private static string SerializeProxyRequestBody(object requestBody)
    {
        if (requestBody is string rawJson)
        {
            return string.IsNullOrWhiteSpace(rawJson) ? "{}" : rawJson;
        }

        if (requestBody is JsonElement jsonElement)
        {
            return jsonElement.GetRawText();
        }

        return JsonSerializer.Serialize(requestBody, ProxyRequestJsonOptions);
    }
}
