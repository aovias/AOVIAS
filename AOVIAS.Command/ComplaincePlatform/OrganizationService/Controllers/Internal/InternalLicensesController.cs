using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrganizationService.Application.Services;
using OrganizationService.Domain.Entities;
using Shared.Infrastructure.Api;
using Shared.Infrastructure.Auth;

namespace OrganizationService.Controllers.Internal;

[ApiController]
[Route("internal/licenses")]
[Authorize(Policy = InternalApiAuthorizationExtensions.InternalOnlyPolicy)]
public sealed class InternalLicensesController : ControllerBase
{
    private readonly LicenseAppService _licenses;
    private readonly ILogger<InternalLicensesController> _logger;

    public InternalLicensesController(LicenseAppService licenses, ILogger<InternalLicensesController> logger)
    {
        _licenses = licenses;
        _logger = logger;
    }

    public record IssueRequest(
        Guid OrganizationId,
        Guid? SubscriptionPlanId,
        string LicenseType,
        int MaxSeats,
        int? ValidDays,
        int GracePeriodDays,
        List<string>? Entitlements,
        string? CustomerName);

    [HttpPost("issue")]
    public async Task<IActionResult> Issue([FromBody] IssueRequest request, CancellationToken cancellationToken)
    {
        var traceId = HttpContext.TraceIdentifier;
        if (request.OrganizationId == Guid.Empty)
        {
            return Fail(ApiResponseCode.VALIDATION_FAILED, traceId);
        }

        try
        {
            var type = Enum.TryParse<LicenseType>(request.LicenseType, true, out var t) ? t : LicenseType.Subscription;
            var key = await _licenses.IssueAsync(
                request.OrganizationId,
                request.SubscriptionPlanId,
                type,
                request.MaxSeats <= 0 ? 10 : request.MaxSeats,
                request.ValidDays,
                request.GracePeriodDays,
                request.Entitlements,
                request.CustomerName,
                cancellationToken);

            return OkSuccess(new
            {
                key.Id,
                key.KeyCode,
                Status = key.Status.ToString(),
                key.ExpiresAtUtc,
                key.MaxSeats,
                reused = false
            }, traceId);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "License issue rejected");
            return Fail(ApiResponseCode.VALIDATION_FAILED, traceId);
        }
    }

    public record TrialGetOrIssueRequest(
        Guid OrganizationId,
        Guid? SubscriptionPlanId,
        int MaxSeats,
        int? ValidDays,
        int GracePeriodDays,
        List<string>? Entitlements,
        string? CustomerName);

    /// <summary>Idempotent trial bootstrap for SaaS signup (CRP Phase 2).</summary>
    [HttpPost("trial/get-or-issue")]
    public async Task<IActionResult> GetOrIssueTrial([FromBody] TrialGetOrIssueRequest request, CancellationToken cancellationToken)
    {
        var traceId = HttpContext.TraceIdentifier;
        if (request.OrganizationId == Guid.Empty)
        {
            return Fail(ApiResponseCode.VALIDATION_FAILED, traceId);
        }

        var (key, reused) = await _licenses.GetOrIssueTrialAsync(
            request.OrganizationId,
            request.MaxSeats <= 0 ? 10 : request.MaxSeats,
            request.ValidDays,
            request.GracePeriodDays,
            request.Entitlements,
            request.CustomerName,
            request.SubscriptionPlanId,
            cancellationToken);

        return OkSuccess(new
        {
            key.Id,
            key.KeyCode,
            Status = key.Status.ToString(),
            key.ExpiresAtUtc,
            key.MaxSeats,
            LicenseType = key.LicenseType.ToString(),
            reused
        }, traceId);
    }

    public record RevokeRequest(Guid LicenseKeyId);

    [HttpPost("revoke")]
    public async Task<IActionResult> Revoke([FromBody] RevokeRequest request, CancellationToken cancellationToken)
    {
        var traceId = HttpContext.TraceIdentifier;
        try
        {
            await _licenses.RevokeAsync(request.LicenseKeyId, cancellationToken);
            return OkSuccess(new { request.LicenseKeyId, status = "Revoked" }, traceId);
        }
        catch (InvalidOperationException)
        {
            return Fail(ApiResponseCode.RESOURCE_NOT_FOUND, traceId);
        }
    }

    [HttpPost("seats/release")]
    public async Task<IActionResult> ReleaseSeat([FromBody] SeatRequest request, CancellationToken cancellationToken)
    {
        var traceId = HttpContext.TraceIdentifier;
        await _licenses.ReleaseSeatAsync(request.OrganizationId, request.UserId, cancellationToken);
        return OkSuccess(new { request.OrganizationId, request.UserId, released = true }, traceId);
    }

    public record ActivateRequest(
        string KeyCode,
        Guid OrganizationId,
        string MachineFingerprint,
        string? ActivatedBy,
        string? OfflinePayload,
        bool Offline);

    [HttpPost("activate")]
    public async Task<IActionResult> Activate([FromBody] ActivateRequest request, CancellationToken cancellationToken)
    {
        var traceId = HttpContext.TraceIdentifier;
        try
        {
            LicenseStatusDto status;
            if (request.Offline)
            {
                status = await _licenses.ActivateOfflineAsync(
                    request.KeyCode,
                    request.OrganizationId,
                    request.MachineFingerprint ?? "offline",
                    request.OfflinePayload ?? string.Empty,
                    request.ActivatedBy,
                    cancellationToken);
            }
            else
            {
                status = await _licenses.ActivateOnlineAsync(
                    request.KeyCode,
                    request.OrganizationId,
                    request.MachineFingerprint ?? Environment.MachineName,
                    request.ActivatedBy,
                    cancellationToken);
            }

            return OkSuccess(status, traceId);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "License activation failed");
            return Fail(ApiResponseCode.VALIDATION_FAILED, traceId);
        }
    }

    [HttpGet("status")]
    public async Task<IActionResult> Status([FromQuery] Guid organizationId, CancellationToken cancellationToken)
    {
        var traceId = HttpContext.TraceIdentifier;
        if (organizationId == Guid.Empty)
        {
            return Fail(ApiResponseCode.VALIDATION_FAILED, traceId);
        }

        var status = await _licenses.ValidateAsync(organizationId, cancellationToken);
        return OkSuccess(status, traceId);
    }

    public record HeartbeatRequest(
        Guid OrganizationId,
        string MachineFingerprint,
        string? ProductVersion,
        int SeatUsage);

    [HttpPost("heartbeat")]
    public async Task<IActionResult> Heartbeat([FromBody] HeartbeatRequest request, CancellationToken cancellationToken)
    {
        var traceId = HttpContext.TraceIdentifier;
        try
        {
            var beat = await _licenses.HeartbeatAsync(
                request.OrganizationId,
                request.MachineFingerprint ?? Environment.MachineName,
                request.ProductVersion,
                request.SeatUsage,
                cancellationToken);
            return OkSuccess(beat, traceId);
        }
        catch (InvalidOperationException)
        {
            return Fail(ApiResponseCode.RESOURCE_NOT_FOUND, traceId);
        }
    }

    public record SeatRequest(Guid OrganizationId, Guid UserId, string? UserEmail);

    [HttpPost("seats/assign")]
    public async Task<IActionResult> AssignSeat([FromBody] SeatRequest request, CancellationToken cancellationToken)
    {
        var traceId = HttpContext.TraceIdentifier;
        try
        {
            var seat = await _licenses.AssignSeatAsync(request.OrganizationId, request.UserId, request.UserEmail, cancellationToken);
            return OkSuccess(seat, traceId);
        }
        catch (InvalidOperationException)
        {
            return Fail(ApiResponseCode.VALIDATION_FAILED, traceId);
        }
    }

    [HttpGet("entitlements/check")]
    public async Task<IActionResult> CheckEntitlement(
        [FromQuery] Guid organizationId,
        [FromQuery] string featureCode,
        CancellationToken cancellationToken)
    {
        var traceId = HttpContext.TraceIdentifier;
        var eval = await _licenses.EvaluateEntitlementAsync(organizationId, featureCode ?? string.Empty, cancellationToken);
        return OkSuccess(new
        {
            organizationId,
            featureCode,
            allowed = eval.Allowed,
            decision = eval.Decision.ToString(),
            kind = eval.Kind.ToString(),
            limit = eval.Limit,
            message = eval.Message
        }, traceId);
    }

    [HttpGet("entitlements")]
    public async Task<IActionResult> ListEntitlements(
        [FromQuery] Guid organizationId,
        CancellationToken cancellationToken)
    {
        var traceId = HttpContext.TraceIdentifier;
        if (organizationId == Guid.Empty)
        {
            return Fail(ApiResponseCode.VALIDATION_FAILED, traceId);
        }

        var items = await _licenses.ListEntitlementsAsync(organizationId, cancellationToken);
        return OkSuccess(items, traceId);
    }

    [HttpPost("entitlements/evaluate")]
    public async Task<IActionResult> EvaluateBatch(
        [FromBody] EvaluateBatchRequest request,
        CancellationToken cancellationToken)
    {
        var traceId = HttpContext.TraceIdentifier;
        if (request.OrganizationId == Guid.Empty)
        {
            return Fail(ApiResponseCode.VALIDATION_FAILED, traceId);
        }

        var codes = request.FeatureCodes ?? new List<string>();
        var results = new List<object>();
        foreach (var code in codes)
        {
            var eval = await _licenses.EvaluateEntitlementAsync(request.OrganizationId, code, cancellationToken);
            results.Add(new
            {
                featureCode = eval.Code,
                allowed = eval.Allowed,
                decision = eval.Decision.ToString(),
                kind = eval.Kind.ToString(),
                limit = eval.Limit,
                message = eval.Message
            });
        }

        return OkSuccess(results, traceId);
    }

    public record EvaluateBatchRequest(Guid OrganizationId, List<string>? FeatureCodes);

    [HttpGet("summary")]
    public async Task<IActionResult> Summary([FromQuery] Guid organizationId, CancellationToken cancellationToken)
    {
        var traceId = HttpContext.TraceIdentifier;
        if (organizationId == Guid.Empty)
        {
            return Fail(ApiResponseCode.VALIDATION_FAILED, traceId);
        }

        var summary = await _licenses.GetLicensingSummaryAsync(organizationId, cancellationToken);
        return OkSuccess(summary, traceId);
    }

    [HttpPost("catalog/backfill")]
    public async Task<IActionResult> CatalogBackfill(CancellationToken cancellationToken)
    {
        var traceId = HttpContext.TraceIdentifier;
        var result = await _licenses.RunCatalogBackfillAsync(cancellationToken);
        return OkSuccess(result, traceId);
    }

    [HttpPost("catalog/sync")]
    public async Task<IActionResult> CatalogSync([FromQuery] Guid organizationId, CancellationToken cancellationToken)
    {
        var traceId = HttpContext.TraceIdentifier;
        if (organizationId == Guid.Empty)
        {
            return Fail(ApiResponseCode.VALIDATION_FAILED, traceId);
        }

        var key = await _licenses.FindOperationalCommercialKeyAsync(organizationId, cancellationToken);
        if (key == null)
        {
            return Fail(ApiResponseCode.RESOURCE_NOT_FOUND, traceId);
        }

        await _licenses.SyncCatalogFromLicenseAsync(key, cancellationToken);
        var summary = await _licenses.GetLicensingSummaryAsync(organizationId, cancellationToken);
        return OkSuccess(summary, traceId);
    }

    public record LicenseIdRequest(Guid LicenseKeyId);

    [HttpPost("suspend")]
    public async Task<IActionResult> Suspend([FromBody] LicenseIdRequest request, CancellationToken cancellationToken)
    {
        var traceId = HttpContext.TraceIdentifier;
        try
        {
            var key = await _licenses.SuspendAsync(request.LicenseKeyId, cancellationToken);
            return OkSuccess(new { key.Id, Status = key.Status.ToString() }, traceId);
        }
        catch (InvalidOperationException)
        {
            return Fail(ApiResponseCode.RESOURCE_NOT_FOUND, traceId);
        }
    }

    [HttpPost("archive")]
    public async Task<IActionResult> Archive([FromBody] LicenseIdRequest request, CancellationToken cancellationToken)
    {
        var traceId = HttpContext.TraceIdentifier;
        try
        {
            var key = await _licenses.ArchiveAsync(request.LicenseKeyId, cancellationToken);
            return OkSuccess(new { key.Id, Status = key.Status.ToString() }, traceId);
        }
        catch (InvalidOperationException)
        {
            return Fail(ApiResponseCode.RESOURCE_NOT_FOUND, traceId);
        }
    }

    [HttpGet("activations")]
    public async Task<IActionResult> Activations([FromQuery] Guid organizationId, CancellationToken cancellationToken)
    {
        var traceId = HttpContext.TraceIdentifier;
        var items = await _licenses.GetActivationHistoryAsync(organizationId, cancellationToken);
        return OkSuccess(items, traceId);
    }

    [HttpGet("usage")]
    public async Task<IActionResult> Usage([FromQuery] Guid organizationId, CancellationToken cancellationToken)
    {
        var traceId = HttpContext.TraceIdentifier;
        var usage = await _licenses.GetUsageAnalyticsAsync(organizationId, cancellationToken);
        return OkSuccess(usage, traceId);
    }

    private static IActionResult OkSuccess(object data, string traceId)
    {
        var response = ApiResponseFactory.Success(ApiResponseCode.SUCCESS, data, traceId);
        var meta = ApiResponseCodes.Registry[ApiResponseCode.SUCCESS];
        return new ObjectResult(response) { StatusCode = meta.HttpStatus };
    }

    private static IActionResult Fail(ApiResponseCode code, string traceId)
    {
        var failure = ApiResponseFactory.Failure<object>(code, traceId);
        var meta = ApiResponseCodes.Registry[code];
        return new ObjectResult(failure) { StatusCode = meta.HttpStatus };
    }
}
