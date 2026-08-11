using Microsoft.AspNetCore.Mvc;

namespace Gateway.Services;

/// <summary>
/// Service for proxying requests to backend microservices.
/// </summary>
public interface IProxyService
{
    /// <summary>
    /// Proxies a request to the Identity Service.
    /// </summary>
    /// <param name="endpoint">The endpoint path (e.g., "/api/v1/auth/login")</param>
    /// <param name="httpMethod">The HTTP method (GET, POST, PUT, DELETE, etc.)</param>
    /// <param name="requestBody">The request body object (will be serialized to JSON)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>IActionResult with the proxied response</returns>
    Task<IActionResult> ProxyToIdentityServiceAsync(
        string endpoint,
        HttpMethod httpMethod,
        object? requestBody = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Proxies a request to the Compliance Service.
    /// </summary>
    /// <param name="endpoint">The endpoint path</param>
    /// <param name="httpMethod">The HTTP method</param>
    /// <param name="requestBody">The request body object (will be serialized to JSON)</param>
    /// <param name="additionalHeaders">Additional headers to include in the request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>IActionResult with the proxied response</returns>
    Task<IActionResult> ProxyToComplianceServiceAsync(
        string endpoint,
        HttpMethod httpMethod,
        object? requestBody = null,
        Dictionary<string, string>? additionalHeaders = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Proxies to Compliance Service with a raw HTTP body (e.g. multipart/form-data) without JSON serialization.
    /// </summary>
    Task<IActionResult> ProxyToComplianceServiceRawAsync(
        string endpoint,
        HttpMethod httpMethod,
        HttpContent? content,
        Dictionary<string, string>? additionalHeaders = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Proxies a GET to Compliance and streams the response body to the client without buffering the full payload (e.g. file download).
    /// </summary>
    Task<IActionResult> ProxyComplianceServiceStreamGetAsync(
        string endpoint,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Proxies a request to the Organization Service.
    /// </summary>
    /// <param name="endpoint">The endpoint path</param>
    /// <param name="httpMethod">The HTTP method</param>
    /// <param name="requestBody">The request body object (will be serialized to JSON)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>IActionResult with the proxied response</returns>
    Task<IActionResult> ProxyToOrganizationServiceAsync(
        string endpoint,
        HttpMethod httpMethod,
        object? requestBody = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Proxies to Organization Service with a raw HTTP body (e.g. multipart/form-data).
    /// </summary>
    Task<IActionResult> ProxyToOrganizationServiceRawAsync(
        string endpoint,
        HttpMethod httpMethod,
        HttpContent? content,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Proxies a GET to Organization Service and streams the response (e.g. logo file).
    /// </summary>
    Task<IActionResult> ProxyOrganizationServiceStreamGetAsync(
        string endpoint,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Proxies a request to the AI Service.
    /// </summary>
    /// <param name="endpoint">The endpoint path</param>
    /// <param name="httpMethod">The HTTP method</param>
    /// <param name="requestBody">The request body object (will be serialized to JSON)</param>
    /// <param name="additionalHeaders">Additional headers to include in the request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>IActionResult with the proxied response</returns>
    Task<IActionResult> ProxyToAIServiceAsync(
        string endpoint,
        HttpMethod httpMethod,
        object? requestBody = null,
        Dictionary<string, string>? additionalHeaders = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Proxies a request to the Audit Service.
    /// </summary>
    /// <param name="endpoint">The endpoint path</param>
    /// <param name="httpMethod">The HTTP method</param>
    /// <param name="requestBody">The request body object (will be serialized to JSON)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>IActionResult with the proxied response</returns>
    Task<IActionResult> ProxyToAuditServiceAsync(
        string endpoint,
        HttpMethod httpMethod,
        object? requestBody = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Proxies a request to the KnowledgeBase Service (PRAMAN cognitive engine).
    /// </summary>
    Task<IActionResult> ProxyToKnowledgeBaseServiceAsync(
        string endpoint,
        HttpMethod httpMethod,
        object? requestBody = null,
        Dictionary<string, string>? additionalHeaders = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Proxies a request to the Person Service.
    /// </summary>
    Task<IActionResult> ProxyToPersonServiceAsync(
        string endpoint,
        HttpMethod httpMethod,
        object? requestBody = null,
        Dictionary<string, string>? additionalHeaders = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Proxies a request to the Workforce Service.
    /// </summary>
    Task<IActionResult> ProxyToWorkforceServiceAsync(
        string endpoint,
        HttpMethod httpMethod,
        object? requestBody = null,
        Dictionary<string, string>? additionalHeaders = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Proxies a request to the Professional Service.
    /// </summary>
    Task<IActionResult> ProxyToProfessionalServiceAsync(
        string endpoint,
        HttpMethod httpMethod,
        object? requestBody = null,
        Dictionary<string, string>? additionalHeaders = null,
        CancellationToken cancellationToken = default);

    Task<IActionResult> ProxyToAccessGovernanceServiceAsync(
        string endpoint,
        HttpMethod httpMethod,
        object? requestBody = null,
        Dictionary<string, string>? additionalHeaders = null,
        CancellationToken cancellationToken = default);

    Task<IActionResult> ProxyToGovernanceServiceAsync(
        string endpoint,
        HttpMethod httpMethod,
        object? requestBody = null,
        Dictionary<string, string>? additionalHeaders = null,
        CancellationToken cancellationToken = default);

    Task<IActionResult> ProxyToCommunicationServiceAsync(
        string endpoint,
        HttpMethod httpMethod,
        object? requestBody = null,
        Dictionary<string, string>? additionalHeaders = null,
        CancellationToken cancellationToken = default);

    Task<IActionResult> ProxyToWorkforceIntelligenceServiceAsync(
        string endpoint,
        HttpMethod httpMethod,
        object? requestBody = null,
        Dictionary<string, string>? additionalHeaders = null,
        CancellationToken cancellationToken = default);

    Task<IActionResult> ProxyToEnterpriseDirectoryServiceAsync(
        string endpoint,
        HttpMethod httpMethod,
        object? requestBody = null,
        Dictionary<string, string>? additionalHeaders = null,
        CancellationToken cancellationToken = default);

    Task<IActionResult> ProxyToIntegrationServiceAsync(
        string endpoint,
        HttpMethod httpMethod,
        object? requestBody = null,
        Dictionary<string, string>? additionalHeaders = null,
        bool forwardAuthHeader = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generic method to proxy a request to any service by client name.
    /// </summary>
    /// <param name="clientName">The HttpClient name registered in DI</param>
    /// <param name="endpoint">The endpoint path</param>
    /// <param name="httpMethod">The HTTP method</param>
    /// <param name="requestBody">The request body object (will be serialized to JSON)</param>
    /// <param name="forwardAuthHeader">Whether to forward the Authorization header from the current request</param>
    /// <param name="additionalHeaders">Additional headers to include in the request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>IActionResult with the proxied response</returns>
    Task<IActionResult> ProxyRequestAsync(
        string clientName,
        string endpoint,
        HttpMethod httpMethod,
        object? requestBody = null,
        bool forwardAuthHeader = false,
        Dictionary<string, string>? additionalHeaders = null,
        CancellationToken cancellationToken = default);
}
