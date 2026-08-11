using Gateway.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gateway.Controllers;

/// <summary>
/// Thin proxy for Phase 4 Sprint 2 Training &amp; Competency lite (ProfessionalService).
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/training")]
[Authorize]
public sealed class TrainingV1ProxyController : ControllerBase
{
    private readonly IProxyService _proxy;

    public TrainingV1ProxyController(IProxyService proxy) => _proxy = proxy;

    [HttpGet("programs")]
    public Task<IActionResult> ListPrograms(CancellationToken ct) => ProxyGet("programs", ct);

    [HttpGet("programs/matrix")]
    public Task<IActionResult> Matrix(CancellationToken ct) => ProxyGet("programs/matrix", ct);

    [HttpGet("programs/{id:guid}")]
    public Task<IActionResult> GetProgram(Guid id, CancellationToken ct) => ProxyGet($"programs/{id}", ct);

    [HttpPost("programs")]
    public Task<IActionResult> CreateProgram([FromBody] object body, CancellationToken ct) =>
        ProxyPost("programs", body, ct);

    [HttpPut("programs/{id:guid}")]
    public Task<IActionResult> UpdateProgram(Guid id, [FromBody] object body, CancellationToken ct) =>
        ProxyPut($"programs/{id}", body, ct);

    [HttpDelete("programs/{id:guid}")]
    public Task<IActionResult> DeleteProgram(Guid id, CancellationToken ct) =>
        ProxyDelete($"programs/{id}", ct);

    [HttpGet("sessions")]
    public Task<IActionResult> ListSessions(CancellationToken ct) => ProxyGet("sessions", ct);

    [HttpGet("sessions/{id:guid}")]
    public Task<IActionResult> GetSession(Guid id, CancellationToken ct) => ProxyGet($"sessions/{id}", ct);

    [HttpPost("sessions")]
    public Task<IActionResult> CreateSession([FromBody] object body, CancellationToken ct) =>
        ProxyPost("sessions", body, ct);

    [HttpPut("sessions/{id:guid}")]
    public Task<IActionResult> UpdateSession(Guid id, [FromBody] object body, CancellationToken ct) =>
        ProxyPut($"sessions/{id}", body, ct);

    [HttpDelete("sessions/{id:guid}")]
    public Task<IActionResult> DeleteSession(Guid id, CancellationToken ct) =>
        ProxyDelete($"sessions/{id}", ct);

    [HttpGet("sessions/{id:guid}/attendances")]
    public Task<IActionResult> ListAttendances(Guid id, CancellationToken ct) =>
        ProxyGet($"sessions/{id}/attendances", ct);

    [HttpPost("sessions/{id:guid}/participants")]
    public Task<IActionResult> AssignParticipants(Guid id, [FromBody] object body, CancellationToken ct) =>
        ProxyPost($"sessions/{id}/participants", body, ct);

    [HttpPost("sessions/attendances/{attendanceId:guid}/record")]
    public Task<IActionResult> RecordAttendance(Guid attendanceId, [FromBody] object body, CancellationToken ct) =>
        ProxyPost($"sessions/attendances/{attendanceId}/record", body, ct);

    [HttpPost("sessions/attendances/{attendanceId:guid}/assessment")]
    public Task<IActionResult> RecordAssessment(Guid attendanceId, [FromBody] object body, CancellationToken ct) =>
        ProxyPost($"sessions/attendances/{attendanceId}/assessment", body, ct);

    [HttpPost("sessions/attendances/{attendanceId:guid}/certificate")]
    public Task<IActionResult> SetCertificate(Guid attendanceId, [FromBody] object body, CancellationToken ct) =>
        ProxyPost($"sessions/attendances/{attendanceId}/certificate", body, ct);

    private Task<IActionResult> ProxyGet(string relative, CancellationToken ct)
    {
        var v = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";
        var qs = Request.QueryString.HasValue ? Request.QueryString.Value : string.Empty;
        return _proxy.ProxyToProfessionalServiceAsync($"/api/v{v}/training/{relative}{qs}", HttpMethod.Get, cancellationToken: ct);
    }

    private Task<IActionResult> ProxyPost(string relative, object? body, CancellationToken ct)
    {
        var v = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";
        return _proxy.ProxyToProfessionalServiceAsync($"/api/v{v}/training/{relative}", HttpMethod.Post, body, cancellationToken: ct);
    }

    private Task<IActionResult> ProxyPut(string relative, object? body, CancellationToken ct)
    {
        var v = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";
        return _proxy.ProxyToProfessionalServiceAsync($"/api/v{v}/training/{relative}", HttpMethod.Put, body, cancellationToken: ct);
    }

    private Task<IActionResult> ProxyDelete(string relative, CancellationToken ct)
    {
        var v = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";
        return _proxy.ProxyToProfessionalServiceAsync($"/api/v{v}/training/{relative}", HttpMethod.Delete, cancellationToken: ct);
    }
}
