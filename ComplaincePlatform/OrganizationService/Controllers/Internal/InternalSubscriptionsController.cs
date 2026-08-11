using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrganizationService.Application.Services;
using Shared.Infrastructure.Api;
using Shared.Infrastructure.Auth;

namespace OrganizationService.Controllers.Internal;

[ApiController]
[Route("internal/subscriptions")]
[Authorize(Policy = InternalApiAuthorizationExtensions.InternalOnlyPolicy)]
public class InternalSubscriptionsController : ControllerBase
{
    private readonly SubscriptionAppService _subscriptionService;
    private readonly ILogger<InternalSubscriptionsController> _logger;

    public InternalSubscriptionsController(
        SubscriptionAppService subscriptionService,
        ILogger<InternalSubscriptionsController> logger)
    {
        _subscriptionService = subscriptionService;
        _logger = logger;
    }

    [HttpGet("current")]
    public async Task<IActionResult> GetCurrent([FromQuery] Guid organizationId, CancellationToken cancellationToken)
    {
        var traceId = HttpContext.TraceIdentifier;

        if (organizationId == Guid.Empty)
        {
            var failureValidation = ApiResponseFactory.Failure<object>(
                ApiResponseCode.VALIDATION_FAILED,
                traceId);

            var metaValidation = ApiResponseCodes.Registry[ApiResponseCode.VALIDATION_FAILED];
            return StatusCode(metaValidation.HttpStatus, failureValidation);
        }

        var current = await _subscriptionService.GetCurrentCatalogueOrLicenseMirrorAsync(organizationId, cancellationToken);
        if (current == null)
        {
            var failureNotFound = ApiResponseFactory.Failure<object>(
                ApiResponseCode.RESOURCE_NOT_FOUND,
                traceId);

            var metaNotFound = ApiResponseCodes.Registry[ApiResponseCode.RESOURCE_NOT_FOUND];
            return StatusCode(metaNotFound.HttpStatus, failureNotFound);
        }

        var response = ApiResponseFactory.Success(
            ApiResponseCode.SUCCESS,
            current,
            traceId);

        var meta = ApiResponseCodes.Registry[ApiResponseCode.SUCCESS];
        return StatusCode(meta.HttpStatus, response);
    }

    public record AssignSubscriptionRequest(Guid OrganizationId, Guid SubscriptionPlanId, bool IsTrial);

    [HttpPost]
    public async Task<IActionResult> Assign([FromBody] AssignSubscriptionRequest request, CancellationToken cancellationToken)
    {
        var traceId = HttpContext.TraceIdentifier;

        if (request.OrganizationId == Guid.Empty || request.SubscriptionPlanId == Guid.Empty)
        {
            var failureValidation = ApiResponseFactory.Failure<object>(
                ApiResponseCode.VALIDATION_FAILED,
                traceId);

            var metaValidation = ApiResponseCodes.Registry[ApiResponseCode.VALIDATION_FAILED];
            return StatusCode(metaValidation.HttpStatus, failureValidation);
        }

        try
        {
            var sub = await _subscriptionService.AssignSubscriptionAsync(
                request.OrganizationId,
                request.SubscriptionPlanId,
                request.IsTrial,
                cancellationToken);

            _logger.LogInformation("Assigned subscription {SubscriptionId} (plan {PlanId}) to organization {OrgId}",
                sub.Id, sub.SubscriptionPlanId, sub.OrganizationId);

            var response = ApiResponseFactory.Success(
                ApiResponseCode.SUCCESS,
                (object)sub,
                traceId);

            var meta = ApiResponseCodes.Registry[ApiResponseCode.SUCCESS];
            return StatusCode(meta.HttpStatus, response);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to assign subscription for organization {OrgId}", request.OrganizationId);

            var failureValidation = ApiResponseFactory.Failure<object>(
                ApiResponseCode.VALIDATION_FAILED,
                traceId);

            var metaValidation = ApiResponseCodes.Registry[ApiResponseCode.VALIDATION_FAILED];
            return StatusCode(metaValidation.HttpStatus, failureValidation);
        }
    }

    public record UpgradeSubscriptionRequest(Guid OrganizationId, Guid TargetPlanId);

    [HttpPatch("upgrade")]
    public async Task<IActionResult> Upgrade([FromBody] UpgradeSubscriptionRequest request, CancellationToken cancellationToken)
    {
        var traceId = HttpContext.TraceIdentifier;

        if (request.OrganizationId == Guid.Empty || request.TargetPlanId == Guid.Empty)
        {
            var failureValidation = ApiResponseFactory.Failure<object>(
                ApiResponseCode.VALIDATION_FAILED,
                traceId);

            var metaValidation = ApiResponseCodes.Registry[ApiResponseCode.VALIDATION_FAILED];
            return StatusCode(metaValidation.HttpStatus, failureValidation);
        }

        try
        {
            var sub = await _subscriptionService.UpgradeSubscriptionAsync(
                request.OrganizationId,
                request.TargetPlanId,
                cancellationToken);

            _logger.LogInformation("Upgraded subscription for organization {OrgId} to plan {PlanId}",
                sub.OrganizationId, sub.SubscriptionPlanId);

            var response = ApiResponseFactory.Success(
                ApiResponseCode.SUCCESS,
                (object)sub,
                traceId);

            var meta = ApiResponseCodes.Registry[ApiResponseCode.SUCCESS];
            return StatusCode(meta.HttpStatus, response);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to upgrade subscription for organization {OrgId}", request.OrganizationId);

            var failureValidation = ApiResponseFactory.Failure<object>(
                ApiResponseCode.VALIDATION_FAILED,
                traceId);

            var metaValidation = ApiResponseCodes.Registry[ApiResponseCode.VALIDATION_FAILED];
            return StatusCode(metaValidation.HttpStatus, failureValidation);
        }
    }
}

