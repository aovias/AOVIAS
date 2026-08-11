using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrganizationService.Application.Services;
using OrganizationService.Domain;
using Shared.Infrastructure.Api;
using Shared.Infrastructure.Auth;

namespace OrganizationService.Controllers.Internal;

[ApiController]
[Route("internal/commercial-admin")]
[Authorize(Policy = InternalApiAuthorizationExtensions.InternalOnlyPolicy)]
public sealed class InternalCommercialAdminController : ControllerBase
{
    private readonly OrganizationCommercialAdminAppService _orgs;
    private readonly CommercialAdminAuditAppService _audit;
    private readonly LicenseAppService _licenses;
    private readonly EmailDeliveryLogAppService _emailLogs;
    private readonly TrialLifecycleEngine _lifecycle;
    private readonly ILogger<InternalCommercialAdminController> _logger;

    public InternalCommercialAdminController(
        OrganizationCommercialAdminAppService orgs,
        CommercialAdminAuditAppService audit,
        LicenseAppService licenses,
        EmailDeliveryLogAppService emailLogs,
        TrialLifecycleEngine lifecycle,
        ILogger<InternalCommercialAdminController> logger)
    {
        _orgs = orgs;
        _audit = audit;
        _licenses = licenses;
        _emailLogs = emailLogs;
        _lifecycle = lifecycle;
        _logger = logger;
    }

    [HttpGet("organizations")]
    public async Task<IActionResult> SearchOrganizations(
        [FromQuery] string? q,
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken ct = default)
    {
        var (items, total) = await _orgs.SearchAsync(q, status, page, pageSize, ct);
        return OkSuccess(new { items, total, page, pageSize }, HttpContext.TraceIdentifier);
    }

    [HttpGet("organizations/{organizationId:guid}")]
    public async Task<IActionResult> GetOrganization(Guid organizationId, CancellationToken ct)
    {
        var org = await _orgs.GetAsync(organizationId, ct);
        if (org == null)
        {
            return Fail(ApiResponseCode.RESOURCE_NOT_FOUND, HttpContext.TraceIdentifier);
        }

        return OkSuccess(new
        {
            org.Id,
            org.Name,
            org.PlatformId,
            org.OrganizationCode,
            org.Status,
            org.Type,
            org.Address,
            org.CreatedAt
        }, HttpContext.TraceIdentifier);
    }

    public record SetStatusRequest(string Status);

    [HttpPost("organizations/{organizationId:guid}/status")]
    public async Task<IActionResult> SetStatus(Guid organizationId, [FromBody] SetStatusRequest body, CancellationToken ct)
    {
        try
        {
            var org = await _orgs.SetStatusAsync(organizationId, body.Status, ct);
            return OkSuccess(new { org.Id, org.Status }, HttpContext.TraceIdentifier);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Org status change rejected");
            return Fail(ApiResponseCode.VALIDATION_FAILED, HttpContext.TraceIdentifier);
        }
    }

    [HttpDelete("organizations/{organizationId:guid}/permanent")]
    public async Task<IActionResult> PermanentDelete(Guid organizationId, CancellationToken ct)
    {
        try
        {
            await _orgs.PermanentDeleteAsync(organizationId, ct);
            return OkSuccess(new { deleted = true }, HttpContext.TraceIdentifier);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Permanent delete rejected");
            return Fail(ApiResponseCode.VALIDATION_FAILED, HttpContext.TraceIdentifier);
        }
    }

    public record AuditRequest(
        Guid? OrganizationId,
        Guid? OperatorUserId,
        string? OperatorEmail,
        string TargetType,
        string? TargetId,
        string Action,
        string? PreviousJson,
        string? NewJson,
        string Reason,
        string? CorrelationId);

    [HttpPost("audit")]
    public async Task<IActionResult> RecordAudit([FromBody] AuditRequest body, CancellationToken ct)
    {
        try
        {
            var entity = await _audit.RecordAsync(
                body.OrganizationId,
                body.OperatorUserId,
                body.OperatorEmail,
                body.TargetType,
                body.TargetId,
                body.Action,
                body.PreviousJson,
                body.NewJson,
                body.Reason,
                body.CorrelationId,
                ct);
            return OkSuccess(new { entity.Id, entity.OccurredAtUtc }, HttpContext.TraceIdentifier);
        }
        catch (InvalidOperationException)
        {
            return Fail(ApiResponseCode.VALIDATION_FAILED, HttpContext.TraceIdentifier);
        }
    }

    [HttpGet("audit")]
    public async Task<IActionResult> SearchAudit(
        [FromQuery] Guid? organizationId,
        [FromQuery] string? targetType,
        [FromQuery] string? action,
        [FromQuery] DateTime? fromUtc,
        [FromQuery] DateTime? toUtc,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var (items, total) = await _audit.SearchAsync(organizationId, targetType, action, fromUtc, toUtc, page, pageSize, ct);
        return OkSuccess(new { items, total, page, pageSize }, HttpContext.TraceIdentifier);
    }

    [HttpGet("licenses")]
    public async Task<IActionResult> SearchLicenses(
        [FromQuery] string? q,
        [FromQuery] string? licenseType,
        [FromQuery] string? status,
        [FromQuery] Guid? organizationId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken ct = default)
    {
        var (items, total) = await _licenses.SearchAsync(q, licenseType, status, organizationId, page, pageSize, ct);
        return OkSuccess(new { items, total, page, pageSize }, HttpContext.TraceIdentifier);
    }

    public record TrialDaysRequest(Guid OrganizationId, int Days, int MaxExtensions = 2);

    [HttpPost("trials/extend")]
    public async Task<IActionResult> ExtendTrial([FromBody] TrialDaysRequest body, CancellationToken ct)
    {
        try
        {
            var key = await _licenses.ExtendTrialAsync(body.OrganizationId, body.Days, body.MaxExtensions, ct);
            return OkSuccess(new { key.Id, key.ExpiresAtUtc, key.ExtensionCount, Status = key.Status.ToString() }, HttpContext.TraceIdentifier);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Extend trial rejected");
            return Fail(ApiResponseCode.VALIDATION_FAILED, HttpContext.TraceIdentifier);
        }
    }

    [HttpPost("trials/shorten")]
    public async Task<IActionResult> ShortenTrial([FromBody] TrialDaysRequest body, CancellationToken ct)
    {
        try
        {
            var key = await _licenses.ShortenTrialAsync(body.OrganizationId, body.Days, ct);
            return OkSuccess(new { key.Id, key.ExpiresAtUtc }, HttpContext.TraceIdentifier);
        }
        catch (InvalidOperationException)
        {
            return Fail(ApiResponseCode.VALIDATION_FAILED, HttpContext.TraceIdentifier);
        }
    }

    public record OrgIdRequest(Guid OrganizationId);

    [HttpPost("trials/expire")]
    public async Task<IActionResult> ExpireTrial([FromBody] OrgIdRequest body, CancellationToken ct)
    {
        try
        {
            var key = await _licenses.ExpireTrialAsync(body.OrganizationId, ct);
            return OkSuccess(new { key.Id, Status = key.Status.ToString() }, HttpContext.TraceIdentifier);
        }
        catch (InvalidOperationException)
        {
            return Fail(ApiResponseCode.VALIDATION_FAILED, HttpContext.TraceIdentifier);
        }
    }

    public record RestartTrialRequest(Guid OrganizationId, int ValidDays = 30, int MaxSeats = 10);

    [HttpPost("trials/restart")]
    public async Task<IActionResult> RestartTrial([FromBody] RestartTrialRequest body, CancellationToken ct)
    {
        try
        {
            var key = await _licenses.RestartTrialAsync(body.OrganizationId, body.ValidDays, body.MaxSeats, ct);
            return OkSuccess(new { key.Id, key.KeyCode, key.ExpiresAtUtc }, HttpContext.TraceIdentifier);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Restart trial rejected");
            return Fail(ApiResponseCode.VALIDATION_FAILED, HttpContext.TraceIdentifier);
        }
    }

    public record ConvertTrialRequest(Guid OrganizationId, Guid? PlanId, int MaxSeats = 0, int ValidDays = 365);

    [HttpPost("trials/convert")]
    public async Task<IActionResult> ConvertTrial([FromBody] ConvertTrialRequest body, CancellationToken ct)
    {
        try
        {
            var key = await _licenses.ConvertTrialToSubscriptionAsync(body.OrganizationId, body.PlanId, body.MaxSeats, body.ValidDays, ct);
            return OkSuccess(new { key.Id, LicenseType = key.LicenseType.ToString(), Status = key.Status.ToString(), key.ExpiresAtUtc }, HttpContext.TraceIdentifier);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Convert trial rejected");
            return Fail(ApiResponseCode.VALIDATION_FAILED, HttpContext.TraceIdentifier);
        }
    }

    public record SetSeatsRequest(Guid LicenseKeyId, int MaxSeats);

    [HttpPost("licenses/seats")]
    public async Task<IActionResult> SetSeats([FromBody] SetSeatsRequest body, CancellationToken ct)
    {
        try
        {
            var key = await _licenses.SetMaxSeatsAsync(body.LicenseKeyId, body.MaxSeats, ct);
            return OkSuccess(new { key.Id, key.MaxSeats, key.UsedSeats }, HttpContext.TraceIdentifier);
        }
        catch (InvalidOperationException)
        {
            return Fail(ApiResponseCode.VALIDATION_FAILED, HttpContext.TraceIdentifier);
        }
    }

    public record EmailLogRequest(
        Guid? OrganizationId,
        Guid? UserId,
        string TemplateKey,
        string ToAddress,
        string? Subject,
        string Status,
        string? Error,
        string? CorrelationId);

    [HttpPost("email-logs")]
    public async Task<IActionResult> RecordEmailLog([FromBody] EmailLogRequest body, CancellationToken ct)
    {
        var entity = await _emailLogs.RecordAsync(
            body.OrganizationId, body.UserId, body.TemplateKey, body.ToAddress,
            body.Subject, body.Status, body.Error, body.CorrelationId, ct);
        return OkSuccess(new { entity.Id }, HttpContext.TraceIdentifier);
    }

    [HttpGet("email-logs")]
    public async Task<IActionResult> SearchEmailLogs(
        [FromQuery] Guid? organizationId,
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var (items, total) = await _emailLogs.SearchAsync(organizationId, status, page, pageSize, ct);
        return OkSuccess(new { items, total, page, pageSize }, HttpContext.TraceIdentifier);
    }

    [HttpGet("health-summary")]
    public async Task<IActionResult> HealthSummary(CancellationToken ct)
    {
        var activeOrgs = await _orgs.SearchAsync(null, OrganizationStatuses.Active, 1, 1, ct);
        var suspended = await _orgs.SearchAsync(null, OrganizationStatuses.Suspended, 1, 1, ct);
        var trials = await _licenses.SearchAsync(null, "Trial", null, null, 1, 1, ct);
        var expired = await _licenses.SearchAsync(null, null, "Expired", null, 1, 1, ct);
        return OkSuccess(new
        {
            activeOrganizations = activeOrgs.Total,
            suspendedOrganizations = suspended.Total,
            trialLicenses = trials.Total,
            expiredLicenses = expired.Total,
            checkedAtUtc = DateTime.UtcNow
        }, HttpContext.TraceIdentifier);
    }

    public record LifecycleRunRequest(string? Trigger, string? CorrelationId);

    [HttpPost("trials/lifecycle/run")]
    public async Task<IActionResult> RunLifecycle([FromBody] LifecycleRunRequest? body, CancellationToken ct)
    {
        var trigger = string.IsNullOrWhiteSpace(body?.Trigger) ? "Manual" : body!.Trigger!.Trim();
        var result = await _lifecycle.EvaluateAllAsync(trigger, body?.CorrelationId, ct);
        return OkSuccess(result, HttpContext.TraceIdentifier);
    }

    [HttpGet("trials/lifecycle/health")]
    public async Task<IActionResult> LifecycleHealth(CancellationToken ct)
    {
        var summary = await _lifecycle.GetHealthSummaryAsync(ct);
        return OkSuccess(summary, HttpContext.TraceIdentifier);
    }

    private IActionResult OkSuccess(object data, string traceId)
    {
        var response = ApiResponseFactory.Success(ApiResponseCode.SUCCESS, data, traceId);
        return StatusCode(ApiResponseCodes.Registry[ApiResponseCode.SUCCESS].HttpStatus, response);
    }

    private IActionResult Fail(ApiResponseCode code, string traceId)
    {
        var failure = ApiResponseFactory.Failure<object>(code, traceId);
        return StatusCode(ApiResponseCodes.Registry[code].HttpStatus, failure);
    }
}
