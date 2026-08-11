using CommunicationService.Application.Commands;
using CommunicationService.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Communication;
using Shared.Infrastructure.Api;
using Shared.Infrastructure.Auth;
using Shared.Infrastructure.DTO;
using System.Security.Claims;

namespace CommunicationService.Controllers.V1;

public static class CommunicationControllerHelpers
{
    public static Guid? GetPersonId(ClaimsPrincipal user)
    {
        var claim = user.FindFirstValue(AuthConstants.Claims.PersonId);
        return Guid.TryParse(claim, out var id) ? id : null;
    }
}

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/notifications")]
[Authorize]
public class NotificationsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var personId = CommunicationControllerHelpers.GetPersonId(User);
        if (!personId.HasValue) return NotFound(ApiResponseFactory.Failure<IReadOnlyList<Application.Contracts.NotificationDto>>(ApiResponseCode.RESOURCE_NOT_FOUND, HttpContext.TraceIdentifier));
        var data = await mediator.Send(new ListNotificationsQuery(personId.Value, page, pageSize), ct);
        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, data, HttpContext.TraceIdentifier));
    }

    [HttpGet("unread-count")]
    public async Task<IActionResult> UnreadCount(CancellationToken ct = default)
    {
        var personId = CommunicationControllerHelpers.GetPersonId(User);
        var count = personId.HasValue ? await mediator.Send(new GetUnreadCountQuery(personId.Value), ct) : 0;
        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, count, HttpContext.TraceIdentifier));
    }

    [HttpPost("{id:guid}/read")]
    public async Task<IActionResult> MarkRead(Guid id, CancellationToken ct = default)
    {
        var personId = CommunicationControllerHelpers.GetPersonId(User);
        if (!personId.HasValue) return NotFound();
        await mediator.Send(new MarkNotificationReadCommand(personId.Value, id), ct);
        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, new { }, HttpContext.TraceIdentifier));
    }

    [HttpPost("read-all")]
    public async Task<IActionResult> MarkAllRead(CancellationToken ct = default)
    {
        var personId = CommunicationControllerHelpers.GetPersonId(User);
        if (!personId.HasValue) return NotFound();
        await mediator.Send(new MarkAllNotificationsReadCommand(personId.Value), ct);
        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, new { }, HttpContext.TraceIdentifier));
    }

    [HttpPost("send")]
    public async Task<IActionResult> Send([FromBody] Application.Contracts.SendCommunicationRequest body, CancellationToken ct = default)
    {
        if (!User.IsInRole(ApplicationRoles.SUPER_ADMIN) && !User.IsInRole(ApplicationRoles.ORG_ADMIN))
            return Forbid();
        var data = await mediator.Send(new SendCommunicationCommand(body), ct);
        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, data, HttpContext.TraceIdentifier));
    }
}

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/preferences")]
[Authorize]
public class PreferencesController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct = default)
    {
        var personId = CommunicationControllerHelpers.GetPersonId(User);
        if (!personId.HasValue) return NotFound();
        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, await mediator.Send(new ListPreferencesQuery(personId.Value), ct), HttpContext.TraceIdentifier));
    }

    [HttpPut]
    public async Task<IActionResult> Put([FromBody] Application.Contracts.UpdatePreferenceRequest body, CancellationToken ct = default)
    {
        var personId = CommunicationControllerHelpers.GetPersonId(User);
        if (!personId.HasValue) return NotFound();
        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, await mediator.Send(new UpdatePreferenceCommand(personId.Value, body), ct), HttpContext.TraceIdentifier));
    }
}

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/templates")]
[Authorize]
public class TemplatesController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct = default)
        => Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, await mediator.Send(new ListTemplatesQuery(), ct), HttpContext.TraceIdentifier));

    [HttpPost]
    public async Task<IActionResult> Upsert([FromBody] Application.Contracts.UpsertTemplateRequest body, CancellationToken ct = default)
        => Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, await mediator.Send(new UpsertTemplateCommand(body), ct), HttpContext.TraceIdentifier));
}

[ApiController]
[ApiVersion("1.0")]
[Authorize]
public class CommunicationHistoryController(IMediator mediator) : ControllerBase
{
    [HttpGet("api/v{version:apiVersion}/communication/history")]
    public async Task<IActionResult> History([FromQuery] Guid? personId, [FromQuery] int take = 100, CancellationToken ct = default)
    {
        personId ??= CommunicationControllerHelpers.GetPersonId(User);
        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, await mediator.Send(new GetTimelineQuery(personId, take), ct), HttpContext.TraceIdentifier));
    }

    [HttpGet("api/v{version:apiVersion}/delivery-status/{id:guid}")]
    public async Task<IActionResult> DeliveryStatus(Guid id, CancellationToken ct = default)
    {
        var data = await mediator.Send(new GetDeliveryStatusQuery(id), ct);
        if (data == null) return NotFound(ApiResponseFactory.Failure<Application.Contracts.DeliveryStatusDto>(ApiResponseCode.RESOURCE_NOT_FOUND, HttpContext.TraceIdentifier));
        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, data, HttpContext.TraceIdentifier));
    }

    [HttpGet("api/v{version:apiVersion}/communication/dashboard")]
    public async Task<IActionResult> Dashboard(CancellationToken ct = default)
        => Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, await mediator.Send(new GetCommunicationDashboardQuery(), ct), HttpContext.TraceIdentifier));

    [HttpGet("api/v{version:apiVersion}/communication/channels")]
    public async Task<IActionResult> Channels(CancellationToken ct = default)
        => Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, await mediator.Send(new ListChannelsQuery(), ct), HttpContext.TraceIdentifier));

    [HttpGet("api/v{version:apiVersion}/communication/providers")]
    public async Task<IActionResult> Providers(CancellationToken ct = default)
        => Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, await mediator.Send(new ListProvidersQuery(), ct), HttpContext.TraceIdentifier));

    [HttpGet("api/v{version:apiVersion}/communication/subscriptions")]
    public async Task<IActionResult> Subscriptions(CancellationToken ct = default)
        => Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, await mediator.Send(new ListSubscriptionsQuery(), ct), HttpContext.TraceIdentifier));

    [HttpGet("api/v{version:apiVersion}/communication/delivery-history")]
    public async Task<IActionResult> DeliveryHistory([FromQuery] int take = 100, CancellationToken ct = default)
        => Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, await mediator.Send(new ListDeliveryHistoryQuery(take), ct), HttpContext.TraceIdentifier));

    [HttpGet("api/v{version:apiVersion}/communication/queues")]
    public async Task<IActionResult> Queues(CancellationToken ct = default)
        => Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, await mediator.Send(new GetQueueStatusQuery(), ct), HttpContext.TraceIdentifier));
}

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/reminders")]
[Authorize]
public class RemindersController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct = default)
        => Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, await mediator.Send(new ListReminderRulesQuery(), ct), HttpContext.TraceIdentifier));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Application.Contracts.CreateReminderRuleRequest body, CancellationToken ct = default)
        => Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, await mediator.Send(new CreateReminderRuleCommand(body), ct), HttpContext.TraceIdentifier));
}

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/campaigns")]
[Authorize]
public class CampaignsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct = default)
        => Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, await mediator.Send(new ListCampaignsQuery(), ct), HttpContext.TraceIdentifier));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Application.Contracts.CreateCampaignRequest body, CancellationToken ct = default)
        => Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, await mediator.Send(new CreateCampaignCommand(body), ct), HttpContext.TraceIdentifier));

    [HttpPost("{id:guid}/launch")]
    public async Task<IActionResult> Launch(Guid id, CancellationToken ct = default)
    {
        await mediator.Send(new LaunchCampaignCommand(id), ct);
        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, new { }, HttpContext.TraceIdentifier));
    }
}

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/availability")]
[Authorize]
public class AvailabilityController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct = default)
    {
        var personId = CommunicationControllerHelpers.GetPersonId(User);
        if (!personId.HasValue) return NotFound();
        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, await mediator.Send(new GetAvailabilityQuery(personId.Value), ct), HttpContext.TraceIdentifier));
    }

    [HttpPut]
    public async Task<IActionResult> Put([FromBody] Application.Contracts.UpdateAvailabilityRequest body, CancellationToken ct = default)
    {
        var personId = CommunicationControllerHelpers.GetPersonId(User);
        if (!personId.HasValue) return NotFound();
        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, await mediator.Send(new UpdateAvailabilityCommand(personId.Value, body), ct), HttpContext.TraceIdentifier));
    }
}

[ApiController]
[Route("internal/communication")]
[Authorize(Policy = InternalApiAuthorizationExtensions.InternalOnlyPolicy)]
public class InternalCommunicationController(IMediator mediator) : ControllerBase
{
    [HttpPost("send")]
    public async Task<IActionResult> Send([FromBody] CommunicationRequestDto request, CancellationToken ct = default)
    {
        var result = await mediator.Send(new SendCommunicationCommand(new Application.Contracts.SendCommunicationRequest
        {
            ModuleCode = request.ModuleCode,
            EventType = request.EventType,
            PersonId = request.PersonId,
            RecipientEmail = request.RecipientEmail,
            RecipientPhone = request.RecipientPhone,
            TemplateCode = request.TemplateCode,
            Subject = request.Subject,
            Body = request.Body,
            Variables = request.Variables,
            Critical = request.Critical,
            DedupeKey = request.DedupeKey,
            PreferredChannels = request.PreferredChannels
        }), ct);
        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, new { outboundMessageId = result.OutboundMessageId, status = result.Status }, HttpContext.TraceIdentifier));
    }

    /// <summary>
    /// Dev / HTTP fallback when Azure Service Bus is unavailable. Enqueues via event subscription rules.
    /// </summary>
    [HttpPost("events")]
    public async Task<IActionResult> EnqueueEvent(
        [FromBody] Application.Contracts.EnqueueComplianceEventRequest request,
        [FromServices] Application.Contracts.IEventSubscriptionProcessor processor,
        CancellationToken ct = default)
    {
        if (request.PersonId == Guid.Empty)
            return BadRequest(ApiResponseFactory.Failure<object>(ApiResponseCode.VALIDATION_FAILED, HttpContext.TraceIdentifier));

        await processor.ProcessEventAsync(
            request.EventType,
            request.ModuleCode,
            request.PersonId,
            request.Variables,
            request.Critical,
            ct);
        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, new { queued = true, request.EventType }, HttpContext.TraceIdentifier));
    }
}
