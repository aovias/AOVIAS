using Gateway.Application.Commands;
using Gateway.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gateway.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/notifications")]
[Authorize]
public class NotificationsController(IMediator mediator) : ControllerBase
{
    private string V() => HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";

    [HttpGet]
    public Task<IActionResult> List([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
        => mediator.Send(new ListNotificationsQuery(V(), page, pageSize), ct);

    [HttpGet("unread-count")]
    public Task<IActionResult> UnreadCount(CancellationToken ct = default)
        => mediator.Send(new GetUnreadCountQuery(V()), ct);

    [HttpPost("{id:guid}/read")]
    public Task<IActionResult> MarkRead(Guid id, CancellationToken ct = default)
        => mediator.Send(new MarkNotificationReadCommand(V(), id), ct);

    [HttpPost("read-all")]
    public Task<IActionResult> MarkAllRead(CancellationToken ct = default)
        => mediator.Send(new MarkAllNotificationsReadCommand(V()), ct);

    [HttpPost("send")]
    public Task<IActionResult> Send([FromBody] object body, CancellationToken ct = default)
        => mediator.Send(new SendNotificationCommand(V(), body), ct);
}

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/preferences")]
[Authorize]
public class CommunicationPreferencesController(IMediator mediator) : ControllerBase
{
    private string V() => HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";

    [HttpGet]
    public Task<IActionResult> Get(CancellationToken ct = default) => mediator.Send(new ListPreferencesQuery(V()), ct);

    [HttpPut]
    public Task<IActionResult> Put([FromBody] object body, CancellationToken ct = default) => mediator.Send(new UpdatePreferencesCommand(V(), body), ct);
}

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/templates")]
[Authorize]
public class CommunicationTemplatesController(IMediator mediator) : ControllerBase
{
    private string V() => HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";

    [HttpGet]
    public Task<IActionResult> List(CancellationToken ct = default) => mediator.Send(new ListCommunicationTemplatesQuery(V()), ct);

    [HttpPost]
    public Task<IActionResult> Upsert([FromBody] object body, CancellationToken ct = default) => mediator.Send(new UpsertCommunicationTemplateCommand(V(), body), ct);
}

[ApiController]
[ApiVersion("1.0")]
[Authorize]
public class CommunicationHistoryController(IMediator mediator) : ControllerBase
{
    private string V() => HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";

    [HttpGet("api/v{version:apiVersion}/communication/history")]
    public Task<IActionResult> History([FromQuery] Guid? personId, [FromQuery] int take = 100, CancellationToken ct = default)
        => mediator.Send(new GetCommunicationHistoryQuery(V(), personId, take), ct);

    [HttpGet("api/v{version:apiVersion}/delivery-status/{id:guid}")]
    public Task<IActionResult> DeliveryStatus(Guid id, CancellationToken ct = default)
        => mediator.Send(new GetDeliveryStatusQuery(V(), id), ct);

    [HttpGet("api/v{version:apiVersion}/communication/dashboard")]
    public Task<IActionResult> Dashboard(CancellationToken ct = default)
        => mediator.Send(new GetCommunicationDashboardQuery(V()), ct);

    [HttpGet("api/v{version:apiVersion}/communication/channels")]
    public Task<IActionResult> Channels(CancellationToken ct = default)
        => mediator.Send(new ListChannelsQuery(V()), ct);

    [HttpGet("api/v{version:apiVersion}/communication/providers")]
    public Task<IActionResult> Providers(CancellationToken ct = default)
        => mediator.Send(new ListProvidersQuery(V()), ct);

    [HttpGet("api/v{version:apiVersion}/communication/subscriptions")]
    public Task<IActionResult> Subscriptions(CancellationToken ct = default)
        => mediator.Send(new ListSubscriptionsQuery(V()), ct);

    [HttpGet("api/v{version:apiVersion}/communication/delivery-history")]
    public Task<IActionResult> DeliveryHistory([FromQuery] int take = 100, CancellationToken ct = default)
        => mediator.Send(new ListDeliveryHistoryQuery(V(), take), ct);

    [HttpGet("api/v{version:apiVersion}/communication/queues")]
    public Task<IActionResult> Queues(CancellationToken ct = default)
        => mediator.Send(new GetQueueStatusQuery(V()), ct);
}

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/reminders")]
[Authorize]
public class CommunicationRemindersController(IMediator mediator) : ControllerBase
{
    private string V() => HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";

    [HttpGet]
    public Task<IActionResult> List(CancellationToken ct = default) => mediator.Send(new ListReminderRulesQuery(V()), ct);

    [HttpPost]
    public Task<IActionResult> Create([FromBody] object body, CancellationToken ct = default) => mediator.Send(new CreateReminderRuleCommand(V(), body), ct);
}

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/campaigns")]
[Authorize]
public class CommunicationCampaignsController(IMediator mediator) : ControllerBase
{
    private string V() => HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";

    [HttpGet]
    public Task<IActionResult> List(CancellationToken ct = default) => mediator.Send(new ListCampaignsQuery(V()), ct);

    [HttpPost]
    public Task<IActionResult> Create([FromBody] object body, CancellationToken ct = default) => mediator.Send(new CreateCampaignCommand(V(), body), ct);

    [HttpPost("{id:guid}/launch")]
    public Task<IActionResult> Launch(Guid id, CancellationToken ct = default) => mediator.Send(new LaunchCampaignCommand(V(), id), ct);
}

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/availability")]
[Authorize]
public class CommunicationAvailabilityController(IMediator mediator) : ControllerBase
{
    private string V() => HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";

    [HttpGet]
    public Task<IActionResult> Get(CancellationToken ct = default) => mediator.Send(new GetAvailabilityQuery(V()), ct);

    [HttpPut]
    public Task<IActionResult> Put([FromBody] object body, CancellationToken ct = default) => mediator.Send(new UpdateAvailabilityCommand(V(), body), ct);
}
