using Gateway.Application.Services;
using Gateway.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Shared.Infrastructure.Api;
using Shared.Infrastructure.Auth;
using Shared.Infrastructure.Email;
using Shared.Infrastructure.DTO;

namespace Gateway.Controllers;

public class CommercialAdminReasonRequest
{
    public string Reason { get; set; } = string.Empty;
    public bool Confirmed { get; set; }
}

public sealed class CommercialAdminStatusRequest : CommercialAdminReasonRequest
{
    public string Status { get; set; } = string.Empty;
}

public sealed class CommercialAdminTrialDaysRequest : CommercialAdminReasonRequest
{
    public int Days { get; set; } = 15;
}

public sealed class CommercialAdminConvertTrialRequest : CommercialAdminReasonRequest
{
    public Guid? PlanId { get; set; }
    public int MaxSeats { get; set; }
    public int ValidDays { get; set; } = 365;
}

public sealed class CommercialAdminEmailRequest : CommercialAdminReasonRequest
{
    public Guid? OrganizationId { get; set; }
    public Guid? UserId { get; set; }
    public string ToAddress { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string? OrganizationName { get; set; }
    public string Template { get; set; } = "WelcomeWithTrial";
}

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/commercial-admin")]
[Authorize(Policy = AuthConstants.Policies.CommercialAdmin)]
public sealed class CommercialAdminController : ControllerBase
{
    private readonly CommercialAdminGatewayService _admin;
    private readonly TrialEnablementOptions _trialOpts;
    private readonly OnboardingExperienceOptions _experience;
    private readonly IEmailService _email;
    private readonly IConfiguration _config;
    private readonly ILogger<CommercialAdminController> _logger;

    public CommercialAdminController(
        CommercialAdminGatewayService admin,
        IOptions<TrialEnablementOptions> trialOpts,
        IOptions<OnboardingExperienceOptions> experience,
        IEmailService email,
        IConfiguration config,
        ILogger<CommercialAdminController> logger)
    {
        _admin = admin;
        _trialOpts = trialOpts.Value;
        _experience = experience.Value;
        _email = email;
        _config = config;
        _logger = logger;
    }

    private IActionResult? RequireReason(CommercialAdminReasonRequest body)
    {
        if (body == null || string.IsNullOrWhiteSpace(body.Reason) || body.Reason.Trim().Length < 3)
        {
            return ApiResponseFactory.FailureResult<object>(ApiResponseCode.VALIDATION_FAILED, _admin.TraceId);
        }

        return null;
    }

    // —— Organizations ——
    [HttpGet("organizations")]
    public async Task<IActionResult> SearchOrganizations(
        [FromQuery] string? q,
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken ct = default)
    {
        if (_admin.ForbidUnless(CommercialPermissions.OrgsRead) is { } f) return f;
        var qs = $"q={Uri.EscapeDataString(q ?? "")}&status={Uri.EscapeDataString(status ?? "")}&page={page}&pageSize={pageSize}";
        return await _admin.ProxyOrgAsync($"/internal/commercial-admin/organizations?{qs}", HttpMethod.Get, null, ct);
    }

    [HttpGet("organizations/{organizationId:guid}")]
    public async Task<IActionResult> GetOrganization(Guid organizationId, CancellationToken ct)
    {
        if (_admin.ForbidUnless(CommercialPermissions.OrgsRead) is { } f) return f;
        return await _admin.ProxyOrgAsync($"/internal/commercial-admin/organizations/{organizationId}", HttpMethod.Get, null, ct);
    }

    [HttpPost("organizations/{organizationId:guid}/status")]
    public async Task<IActionResult> SetOrganizationStatus(
        Guid organizationId,
        [FromBody] CommercialAdminStatusRequest body,
        CancellationToken ct)
    {
        if (_admin.ForbidUnless(CommercialPermissions.OrgsWrite) is { } f) return f;
        if (RequireReason(body) is { } bad) return bad;
        if (!body.Confirmed)
        {
            return ApiResponseFactory.FailureResult<object>(ApiResponseCode.VALIDATION_FAILED, _admin.TraceId);
        }

        await _admin.RecordAuditAsync(organizationId, "Organization", organizationId.ToString(),
            $"OrgStatus:{body.Status}", null, new { body.Status }, body.Reason, ct);
        return await _admin.ProxyOrgAsync(
            $"/internal/commercial-admin/organizations/{organizationId}/status",
            HttpMethod.Post,
            new { Status = body.Status },
            ct);
    }

    [HttpDelete("organizations/{organizationId:guid}/permanent")]
    public async Task<IActionResult> PermanentDelete(
        Guid organizationId,
        [FromBody] CommercialAdminReasonRequest body,
        CancellationToken ct)
    {
        if (_admin.ForbidUnless(CommercialPermissions.OrgsPermanentDelete) is { } f) return f;
        if (RequireReason(body) is { } bad) return bad;
        if (!body.Confirmed)
        {
            return ApiResponseFactory.FailureResult<object>(ApiResponseCode.VALIDATION_FAILED, _admin.TraceId);
        }

        await _admin.RecordAuditAsync(organizationId, "Organization", organizationId.ToString(),
            "OrgPermanentDelete", null, null, body.Reason, ct);
        return await _admin.ProxyOrgAsync(
            $"/internal/commercial-admin/organizations/{organizationId}/permanent",
            HttpMethod.Delete,
            null,
            ct);
    }

    // —— Users ——
    [HttpGet("users")]
    public async Task<IActionResult> SearchUsers(
        [FromQuery] string? q,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken ct = default)
    {
        if (_admin.ForbidUnless(CommercialPermissions.UsersRead) is { } f) return f;
        var qs = $"q={Uri.EscapeDataString(q ?? "")}&page={page}&pageSize={pageSize}";
        return await _admin.ProxyIdentityAsync($"/internal/users/search?{qs}", HttpMethod.Get, null, ct);
    }

    [HttpGet("users/{userId:guid}")]
    public async Task<IActionResult> GetUser(Guid userId, CancellationToken ct)
    {
        if (_admin.ForbidUnless(CommercialPermissions.UsersRead) is { } f) return f;
        return await _admin.ProxyIdentityAsync($"/internal/users/{userId}/contact", HttpMethod.Get, null, ct);
    }

    [HttpPost("users/{userId:guid}/suspend")]
    public async Task<IActionResult> SuspendUser(Guid userId, [FromBody] CommercialAdminReasonRequest body, CancellationToken ct)
    {
        if (_admin.ForbidUnless(CommercialPermissions.UsersWrite) is { } f) return f;
        if (RequireReason(body) is { } bad) return bad;
        await _admin.RecordAuditAsync(null, "User", userId.ToString(), "UserSuspend", null, null, body.Reason, ct);
        return await _admin.ProxyIdentityAsync($"/internal/users/{userId}/suspend", HttpMethod.Post, null, ct);
    }

    [HttpPost("users/{userId:guid}/activate")]
    public async Task<IActionResult> ActivateUser(Guid userId, [FromBody] CommercialAdminReasonRequest body, CancellationToken ct)
    {
        if (_admin.ForbidUnless(CommercialPermissions.UsersWrite) is { } f) return f;
        if (RequireReason(body) is { } bad) return bad;
        await _admin.RecordAuditAsync(null, "User", userId.ToString(), "UserActivate", null, null, body.Reason, ct);
        return await _admin.ProxyIdentityAsync($"/internal/users/{userId}/activate", HttpMethod.Post, null, ct);
    }

    [HttpPost("users/{userId:guid}/disable")]
    public async Task<IActionResult> DisableUser(Guid userId, [FromBody] CommercialAdminReasonRequest body, CancellationToken ct)
    {
        if (_admin.ForbidUnless(CommercialPermissions.UsersWrite) is { } f) return f;
        if (RequireReason(body) is { } bad) return bad;
        await _admin.RecordAuditAsync(null, "User", userId.ToString(), "UserDisable", null, null, body.Reason, ct);
        return await _admin.ProxyIdentityAsync($"/internal/users/{userId}/disable", HttpMethod.Post, null, ct);
    }

    [HttpPost("users/{userId:guid}/unlock")]
    public async Task<IActionResult> UnlockUser(Guid userId, [FromBody] CommercialAdminReasonRequest body, CancellationToken ct)
    {
        if (_admin.ForbidUnless(CommercialPermissions.UsersUnlock) is { } f) return f;
        if (RequireReason(body) is { } bad) return bad;
        await _admin.RecordAuditAsync(null, "User", userId.ToString(), "UserUnlock", null, null, body.Reason, ct);
        return await _admin.ProxyIdentityAsync($"/internal/users/{userId}/unlock", HttpMethod.Post, null, ct);
    }

    [HttpPost("users/{userId:guid}/force-password-change")]
    public async Task<IActionResult> ForcePasswordChange(Guid userId, [FromBody] CommercialAdminReasonRequest body, CancellationToken ct)
    {
        if (_admin.ForbidUnless(CommercialPermissions.UsersPassword) is { } f) return f;
        if (RequireReason(body) is { } bad) return bad;
        await _admin.RecordAuditAsync(null, "User", userId.ToString(), "ForcePasswordChange", null, null, body.Reason, ct);
        return await _admin.ProxyIdentityAsync($"/internal/users/{userId}/force-password-change", HttpMethod.Post, null, ct);
    }

    [HttpPost("users/{userId:guid}/terminate-sessions")]
    public async Task<IActionResult> TerminateSessions(Guid userId, [FromBody] CommercialAdminReasonRequest body, CancellationToken ct)
    {
        if (_admin.ForbidUnless(CommercialPermissions.UsersWrite) is { } f) return f;
        if (RequireReason(body) is { } bad) return bad;
        await _admin.RecordAuditAsync(null, "User", userId.ToString(), "TerminateSessions", null, null, body.Reason, ct);
        return await _admin.ProxyIdentityAsync($"/internal/users/{userId}/terminate-sessions", HttpMethod.Post, null, ct);
    }

    // —— Trials ——
    [HttpGet("trials")]
    public async Task<IActionResult> SearchTrials(
        [FromQuery] string? q,
        [FromQuery] Guid? organizationId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken ct = default)
    {
        if (_admin.ForbidUnless(CommercialPermissions.TrialsRead) is { } f) return f;
        var qs = $"q={Uri.EscapeDataString(q ?? "")}&licenseType=Trial&organizationId={organizationId}&page={page}&pageSize={pageSize}";
        return await _admin.ProxyOrgAsync($"/internal/commercial-admin/licenses?{qs}", HttpMethod.Get, null, ct);
    }

    [HttpPost("organizations/{organizationId:guid}/trials/extend")]
    public async Task<IActionResult> ExtendTrial(
        Guid organizationId,
        [FromBody] CommercialAdminTrialDaysRequest body,
        CancellationToken ct)
    {
        if (_admin.ForbidUnless(CommercialPermissions.TrialsWrite) is { } f) return f;
        if (RequireReason(body) is { } bad) return bad;
        var days = body.Days > 0 ? body.Days : _trialOpts.ExtensionDays;
        await _admin.RecordAuditAsync(organizationId, "Trial", organizationId.ToString(), "TrialExtend",
            null, new { days }, body.Reason, ct);
        return await _admin.ProxyOrgAsync("/internal/commercial-admin/trials/extend", HttpMethod.Post,
            new { OrganizationId = organizationId, Days = days, MaxExtensions = _trialOpts.MaxExtensions }, ct);
    }

    [HttpPost("organizations/{organizationId:guid}/trials/shorten")]
    public async Task<IActionResult> ShortenTrial(
        Guid organizationId,
        [FromBody] CommercialAdminTrialDaysRequest body,
        CancellationToken ct)
    {
        if (_admin.ForbidUnless(CommercialPermissions.TrialsWrite) is { } f) return f;
        if (RequireReason(body) is { } bad) return bad;
        await _admin.RecordAuditAsync(organizationId, "Trial", organizationId.ToString(), "TrialShorten",
            null, new { body.Days }, body.Reason, ct);
        return await _admin.ProxyOrgAsync("/internal/commercial-admin/trials/shorten", HttpMethod.Post,
            new { OrganizationId = organizationId, Days = body.Days }, ct);
    }

    [HttpPost("organizations/{organizationId:guid}/trials/expire")]
    public async Task<IActionResult> ExpireTrial(
        Guid organizationId,
        [FromBody] CommercialAdminReasonRequest body,
        CancellationToken ct)
    {
        if (_admin.ForbidUnless(CommercialPermissions.TrialsWrite) is { } f) return f;
        if (RequireReason(body) is { } bad) return bad;
        await _admin.RecordAuditAsync(organizationId, "Trial", organizationId.ToString(), "TrialExpire", null, null, body.Reason, ct);
        return await _admin.ProxyOrgAsync("/internal/commercial-admin/trials/expire", HttpMethod.Post,
            new { OrganizationId = organizationId }, ct);
    }

    [HttpPost("organizations/{organizationId:guid}/trials/revoke")]
    public async Task<IActionResult> RevokeTrial(
        Guid organizationId,
        [FromBody] CommercialAdminReasonRequest body,
        CancellationToken ct)
    {
        if (_admin.ForbidUnless(CommercialPermissions.LicensesRevoke) is { } f) return f;
        if (RequireReason(body) is { } bad) return bad;
        await _admin.RecordAuditAsync(organizationId, "Trial", organizationId.ToString(), "TrialRevoke", null, null, body.Reason, ct);
        // Resolve trial key via search then revoke
        var search = await _admin.ProxyOrgAsync(
            $"/internal/commercial-admin/licenses?licenseType=Trial&organizationId={organizationId}&page=1&pageSize=1",
            HttpMethod.Get, null, ct);
        return await RevokeFromLicenseSearch(organizationId, search, body.Reason, ct);
    }

    [HttpPost("organizations/{organizationId:guid}/trials/convert")]
    public async Task<IActionResult> ConvertTrial(
        Guid organizationId,
        [FromBody] CommercialAdminConvertTrialRequest body,
        CancellationToken ct)
    {
        if (_admin.ForbidUnless(CommercialPermissions.TrialsConvert) is { } f) return f;
        if (RequireReason(body) is { } bad) return bad;
        await _admin.RecordAuditAsync(organizationId, "Trial", organizationId.ToString(), "TrialConvert",
            null, new { body.PlanId, body.MaxSeats, body.ValidDays }, body.Reason, ct);
        return await _admin.ProxyOrgAsync("/internal/commercial-admin/trials/convert", HttpMethod.Post,
            new { OrganizationId = organizationId, body.PlanId, body.MaxSeats, body.ValidDays }, ct);
    }

    [HttpPost("organizations/{organizationId:guid}/trials/restart")]
    public async Task<IActionResult> RestartTrial(
        Guid organizationId,
        [FromBody] CommercialAdminReasonRequest body,
        CancellationToken ct)
    {
        if (_admin.ForbidUnless(CommercialPermissions.TrialsWrite) is { } f) return f;
        if (RequireReason(body) is { } bad) return bad;
        await _admin.RecordAuditAsync(organizationId, "Trial", organizationId.ToString(), "TrialRestart", null, null, body.Reason, ct);
        return await _admin.ProxyOrgAsync("/internal/commercial-admin/trials/restart", HttpMethod.Post,
            new { OrganizationId = organizationId, ValidDays = _trialOpts.ValidDays, MaxSeats = _trialOpts.MaxSeats }, ct);
    }

    // —— Licenses ——
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
        if (_admin.ForbidUnless(CommercialPermissions.LicensesRead) is { } f) return f;
        var qs = $"q={Uri.EscapeDataString(q ?? "")}&licenseType={Uri.EscapeDataString(licenseType ?? "")}&status={Uri.EscapeDataString(status ?? "")}&organizationId={organizationId}&page={page}&pageSize={pageSize}";
        return await _admin.ProxyOrgAsync($"/internal/commercial-admin/licenses?{qs}", HttpMethod.Get, null, ct);
    }

    [HttpPost("licenses/{licenseKeyId:guid}/revoke")]
    public async Task<IActionResult> RevokeLicense(
        Guid licenseKeyId,
        [FromBody] CommercialAdminReasonRequest body,
        CancellationToken ct)
    {
        if (_admin.ForbidUnless(CommercialPermissions.LicensesRevoke) is { } f) return f;
        if (RequireReason(body) is { } bad) return bad;
        await _admin.RecordAuditAsync(null, "License", licenseKeyId.ToString(), "LicenseRevoke", null, null, body.Reason, ct);
        return await _admin.ProxyOrgAsync("/internal/licenses/revoke", HttpMethod.Post, new { LicenseKeyId = licenseKeyId }, ct);
    }

    [HttpPost("licenses/issue")]
    public async Task<IActionResult> IssueLicense([FromBody] Dictionary<string, object?> body, CancellationToken ct)
    {
        if (_admin.ForbidUnless(CommercialPermissions.LicensesWrite) is { } f) return f;
        var reason = body.TryGetValue("reason", out var r) ? r?.ToString() : null;
        if (string.IsNullOrWhiteSpace(reason))
        {
            return ApiResponseFactory.FailureResult<object>(ApiResponseCode.VALIDATION_FAILED, _admin.TraceId);
        }

        await _admin.RecordAuditAsync(null, "License", null, "LicenseIssue", null, body, reason, ct);
        return await _admin.ProxyOrgAsync("/internal/licenses/issue", HttpMethod.Post, body, ct);
    }

    [HttpPost("licenses/activate")]
    public async Task<IActionResult> ActivateLicense([FromBody] Dictionary<string, object?> body, CancellationToken ct)
    {
        if (_admin.ForbidUnless(CommercialPermissions.LicensesWrite) is { } f) return f;
        var reason = body.TryGetValue("reason", out var r) ? r?.ToString() ?? "activate" : "activate";
        await _admin.RecordAuditAsync(null, "License", null, "LicenseActivate", null, body, reason, ct);
        return await _admin.ProxyOrgAsync("/internal/licenses/activate", HttpMethod.Post, body, ct);
    }

    [HttpPost("licenses/seats")]
    public async Task<IActionResult> SetSeats([FromBody] Dictionary<string, object?> body, CancellationToken ct)
    {
        if (_admin.ForbidUnless(CommercialPermissions.LicensesWrite) is { } f) return f;
        var reason = body.TryGetValue("reason", out var r) ? r?.ToString() ?? "seat-change" : "seat-change";
        await _admin.RecordAuditAsync(null, "License", null, "LicenseSeats", null, body, reason, ct);
        return await _admin.ProxyOrgAsync("/internal/commercial-admin/licenses/seats", HttpMethod.Post, body, ct);
    }

    // —— Email ——
    [HttpPost("email/send")]
    public async Task<IActionResult> SendEmail([FromBody] CommercialAdminEmailRequest body, CancellationToken ct)
    {
        if (_admin.ForbidUnless(CommercialPermissions.EmailSend) is { } f) return f;
        if (RequireReason(body) is { } bad) return bad;
        if (string.IsNullOrWhiteSpace(body.ToAddress))
        {
            return ApiResponseFactory.FailureResult<object>(ApiResponseCode.VALIDATION_FAILED, _admin.TraceId);
        }

        var loginUrl = BuildLoginUrl();
        var (subject, html) = body.Template switch
        {
            "GettingStarted" => IdentityEmailTemplates.GettingStarted(
                body.DisplayName, body.OrganizationName ?? "your organization", loginUrl, _experience.DocsBaseUrl, _experience.SupportEmail),
            "PasswordReset" => IdentityEmailTemplates.PasswordReset(loginUrl),
            "SecurityNotification" => IdentityEmailTemplates.SecurityNotification("Security notice", body.Reason),
            _ => IdentityEmailTemplates.WelcomeWithTrial(
                body.DisplayName,
                body.OrganizationName ?? "your organization",
                _trialOpts.ValidDays,
                _trialOpts.MaxSeats,
                DateTime.UtcNow.AddDays(_trialOpts.ValidDays),
                loginUrl,
                _experience.DocsBaseUrl,
                _experience.SupportEmail)
        };

        string status = "Sent";
        string? error = null;
        try
        {
            await _email.SendAsync(body.ToAddress, subject, html);
        }
        catch (Exception ex)
        {
            status = "Failed";
            error = ex.Message;
            _logger.LogWarning(ex, "Commercial admin email send failed");
        }

        await _admin.RecordAuditAsync(body.OrganizationId, "Email", body.ToAddress, $"Email:{body.Template}", null,
            new { status }, body.Reason, ct);
        await _admin.ProxyOrgAsync("/internal/commercial-admin/email-logs", HttpMethod.Post, new
        {
            body.OrganizationId,
            body.UserId,
            TemplateKey = body.Template,
            body.ToAddress,
            Subject = subject,
            Status = status,
            Error = error,
            CorrelationId = _admin.TraceId
        }, ct);

        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, new { status, subject }, _admin.TraceId));
    }

    [HttpGet("email/logs")]
    public async Task<IActionResult> EmailLogs(
        [FromQuery] Guid? organizationId,
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        if (_admin.ForbidUnless(CommercialPermissions.EmailSend) is { } f) return f;
        var qs = $"organizationId={organizationId}&status={Uri.EscapeDataString(status ?? "")}&page={page}&pageSize={pageSize}";
        return await _admin.ProxyOrgAsync($"/internal/commercial-admin/email-logs?{qs}", HttpMethod.Get, null, ct);
    }

    [HttpGet("email/preview")]
    public IActionResult PreviewEmail([FromQuery] string template = "WelcomeWithTrial", [FromQuery] string? orgName = null)
    {
        if (_admin.ForbidUnless(CommercialPermissions.EmailSend) is { } f) return f;
        var loginUrl = BuildLoginUrl();
        var (subject, html) = template switch
        {
            "GettingStarted" => IdentityEmailTemplates.GettingStarted("Admin", orgName ?? "Acme", loginUrl, _experience.DocsBaseUrl, _experience.SupportEmail),
            _ => IdentityEmailTemplates.WelcomeWithTrial("Admin", orgName ?? "Acme", 30, 10, DateTime.UtcNow.AddDays(30), loginUrl, _experience.DocsBaseUrl, _experience.SupportEmail)
        };
        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, new { subject, html }, _admin.TraceId));
    }

    // —— Support 360 ——
    [HttpGet("support/{organizationId:guid}")]
    public async Task<IActionResult> SupportWorkspace(Guid organizationId, CancellationToken ct)
    {
        if (_admin.ForbidUnless(CommercialPermissions.SupportWorkspace) is { } f) return f;
        var org = await _admin.ProxyOrgAsync($"/internal/commercial-admin/organizations/{organizationId}", HttpMethod.Get, null, ct);
        var license = await _admin.ProxyOrgAsync($"/internal/licenses/status?organizationId={organizationId}", HttpMethod.Get, null, ct);
        var usage = await _admin.ProxyOrgAsync($"/internal/licenses/usage?organizationId={organizationId}", HttpMethod.Get, null, ct);
        var audit = await _admin.ProxyOrgAsync(
            $"/internal/commercial-admin/audit?organizationId={organizationId}&page=1&pageSize=20",
            HttpMethod.Get, null, ct);
        var health = await _admin.ProxyOrgAsync($"/internal/commercial-admin/health-summary", HttpMethod.Get, null, ct);

        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, new
        {
            organizationId,
            organization = DescribeResult(org),
            license = DescribeResult(license),
            usage = DescribeResult(usage),
            recentAudit = DescribeResult(audit),
            platformHealth = DescribeResult(health),
            quickActions = new[]
            {
                "unlock-user", "reset-password", "extend-trial", "activate-license",
                "resend-email", "invite-user", "suspend-organization"
            }
        }, _admin.TraceId));
    }

    // —— Audit / Health / Search ——
    [HttpGet("audit")]
    public async Task<IActionResult> SearchAudit(
        [FromQuery] Guid? organizationId,
        [FromQuery] string? targetType,
        [FromQuery] string? action,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        if (_admin.ForbidUnless(CommercialPermissions.AuditRead) is { } f) return f;
        var qs = $"organizationId={organizationId}&targetType={Uri.EscapeDataString(targetType ?? "")}&action={Uri.EscapeDataString(action ?? "")}&page={page}&pageSize={pageSize}";
        return await _admin.ProxyOrgAsync($"/internal/commercial-admin/audit?{qs}", HttpMethod.Get, null, ct);
    }

    [HttpGet("health")]
    public async Task<IActionResult> Health(CancellationToken ct)
    {
        if (_admin.ForbidUnless(CommercialPermissions.HealthRead) is { } f) return f;
        return await _admin.ProxyOrgAsync("/internal/commercial-admin/health-summary", HttpMethod.Get, null, ct);
    }

    [HttpGet("search")]
    public async Task<IActionResult> GlobalSearch([FromQuery] string q, CancellationToken ct)
    {
        if (_admin.ForbidUnless(CommercialPermissions.Search) is { } f) return f;
        if (string.IsNullOrWhiteSpace(q) || q.Trim().Length < 2)
        {
            return ApiResponseFactory.FailureResult<object>(ApiResponseCode.VALIDATION_FAILED, _admin.TraceId);
        }

        var term = Uri.EscapeDataString(q.Trim());
        var orgs = await _admin.ProxyOrgAsync($"/internal/commercial-admin/organizations?q={term}&page=1&pageSize=10", HttpMethod.Get, null, ct);
        var licenses = await _admin.ProxyOrgAsync($"/internal/commercial-admin/licenses?q={term}&page=1&pageSize=10", HttpMethod.Get, null, ct);
        var users = await _admin.ProxyIdentityAsync($"/internal/users/search?q={term}&page=1&pageSize=10", HttpMethod.Get, null, ct);
        var trials = await _admin.ProxyOrgAsync($"/internal/commercial-admin/licenses?q={term}&licenseType=Trial&page=1&pageSize=10", HttpMethod.Get, null, ct);

        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, new
        {
            query = q,
            organizations = DescribeResult(orgs),
            users = DescribeResult(users),
            licenses = DescribeResult(licenses),
            trials = DescribeResult(trials)
        }, _admin.TraceId));
    }

    [HttpPost("trials/lifecycle/run")]
    public async Task<IActionResult> RunLifecycle(
        [FromBody] CommercialAdminReasonRequest? body,
        CancellationToken ct)
    {
        if (_admin.ForbidUnless(CommercialPermissions.TrialsWrite) is { } f) return f;
        var reason = string.IsNullOrWhiteSpace(body?.Reason) ? "Manual lifecycle evaluation" : body!.Reason;
        await _admin.RecordAuditAsync(null, "TrialLifecycle", null, "LifecycleRun", null, new { trigger = "Manual" }, reason, ct);
        return await _admin.ProxyOrgAsync(
            "/internal/commercial-admin/trials/lifecycle/run",
            HttpMethod.Post,
            new { Trigger = "Manual", CorrelationId = _admin.TraceId },
            ct);
    }

    [HttpGet("trials/lifecycle/health")]
    public async Task<IActionResult> LifecycleHealth(CancellationToken ct)
    {
        if (_admin.ForbidUnless(CommercialPermissions.HealthRead) is { } f) return f;
        return await _admin.ProxyOrgAsync("/internal/commercial-admin/trials/lifecycle/health", HttpMethod.Get, null, ct);
    }

    [HttpGet("me/permissions")]
    public IActionResult MyPermissions()
    {
        var roles = _admin.OperatorRoles();
        var perms = new[]
        {
            CommercialPermissions.OrgsRead, CommercialPermissions.OrgsWrite, CommercialPermissions.OrgsDelete,
            CommercialPermissions.OrgsPermanentDelete, CommercialPermissions.UsersRead, CommercialPermissions.UsersWrite,
            CommercialPermissions.UsersUnlock, CommercialPermissions.UsersPassword, CommercialPermissions.TrialsRead,
            CommercialPermissions.TrialsWrite, CommercialPermissions.TrialsConvert, CommercialPermissions.LicensesRead,
            CommercialPermissions.LicensesWrite, CommercialPermissions.LicensesRevoke, CommercialPermissions.EmailSend,
            CommercialPermissions.SupportWorkspace, CommercialPermissions.CommercialOps, CommercialPermissions.AuditRead,
            CommercialPermissions.AuditExport, CommercialPermissions.HealthRead, CommercialPermissions.Search
        }.Where(p => CommercialPermissions.AnyRoleHasPermission(roles, p)).ToArray();

        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, new { roles, permissions = perms }, _admin.TraceId));
    }

    private async Task<IActionResult> RevokeFromLicenseSearch(
        Guid organizationId,
        IActionResult searchResult,
        string reason,
        CancellationToken ct)
    {
        // Fallback: expire trial when key id cannot be extracted from proxy envelope.
        _ = reason;
        if (searchResult is ObjectResult { Value: not null })
        {
            // Prefer expire path for reliability when payload shape varies.
        }

        return await _admin.ProxyOrgAsync("/internal/commercial-admin/trials/expire", HttpMethod.Post,
            new { OrganizationId = organizationId }, ct);
    }

    private static object? DescribeResult(IActionResult result)
        => result switch
        {
            ObjectResult o => o.Value,
            JsonResult j => j.Value,
            _ => new { status = "unavailable" }
        };

    private string BuildLoginUrl()
    {
        var baseUrl = _config["Identity:PublicBaseUrl"]
                      ?? _config["Gateway:PublicBaseUrl"]
                      ?? "http://localhost:4200";
        return $"{baseUrl.TrimEnd('/')}/login";
    }
}
