using Gateway.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Infrastructure.Auth;

namespace Gateway.Controllers;

/// <summary>
/// Proxies External Auditor Workspace endpoints to ComplianceService (Sprint 14).
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/auditor")]
[Authorize(Policy = AuthConstants.Policies.ComplianceScope)]
public sealed class AuditorV1ProxyController : ControllerBase
{
    private readonly IProxyService _proxy;

    public AuditorV1ProxyController(IProxyService proxy) => _proxy = proxy;

    [HttpGet("dashboard")]
    public Task<IActionResult> Dashboard(CancellationToken ct) =>
        ProxyGet("dashboard", ct);

    [HttpGet("sessions")]
    public Task<IActionResult> Sessions(CancellationToken ct) =>
        ProxyGet("sessions", ct);

    [HttpPost("sessions")]
    public Task<IActionResult> CreateSession([FromBody] object body, CancellationToken ct) =>
        ProxyPost("sessions", body, ct);

    [HttpGet("session/{id:guid}")]
    public Task<IActionResult> GetSession(Guid id, CancellationToken ct) =>
        ProxyGet($"session/{id}", ct);

    [HttpPost("sessions/{id:guid}/close")]
    public Task<IActionResult> CloseSession(Guid id, [FromBody] object? body, CancellationToken ct) =>
        ProxyPost($"sessions/{id}/close", body ?? new { }, ct);

    [HttpPost("simulation")]
    public Task<IActionResult> Simulation([FromBody] object? body, CancellationToken ct) =>
        ProxyPost("simulation", body ?? new { }, ct);

    [HttpPost("observations")]
    public Task<IActionResult> CreateObservation([FromBody] object body, CancellationToken ct) =>
        ProxyPost("observations", body, ct);

    [HttpGet("traceability/{objectiveElementId:guid}")]
    public Task<IActionResult> Traceability(Guid objectiveElementId, CancellationToken ct) =>
        ProxyGet($"traceability/{objectiveElementId}", ct);

    [HttpPost("comments")]
    public Task<IActionResult> CreateComment([FromBody] object body, CancellationToken ct) =>
        ProxyPost("comments", body, ct);

    [HttpGet("report/{sessionId:guid}")]
    public Task<IActionResult> Report(Guid sessionId, CancellationToken ct) =>
        ProxyGet($"report/{sessionId}", ct);

    [HttpGet("assessment-context")]
    public Task<IActionResult> AssessmentContext(CancellationToken ct)
    {
        var v = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";
        var qs = Request.QueryString.HasValue ? Request.QueryString.Value : string.Empty;
        return _proxy.ProxyToComplianceServiceAsync($"/api/v{v}/auditor/assessment-context{qs}", HttpMethod.Get, null, null, ct);
    }

    private Task<IActionResult> ProxyGet(string suffix, CancellationToken ct)
    {
        var v = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";
        return _proxy.ProxyToComplianceServiceAsync($"/api/v{v}/auditor/{suffix}", HttpMethod.Get, null, null, ct);
    }

    private Task<IActionResult> ProxyPost(string suffix, object body, CancellationToken ct)
    {
        var v = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";
        return _proxy.ProxyToComplianceServiceAsync($"/api/v{v}/auditor/{suffix}", HttpMethod.Post, body, null, ct);
    }
}
