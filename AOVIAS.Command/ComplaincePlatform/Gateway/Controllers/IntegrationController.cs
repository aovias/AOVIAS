using Gateway.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Authorization;

namespace Gateway.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/integration")]
[Authorize]
public class IntegrationController(IMediator mediator) : ControllerBase
{
    private string V() => HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";

    [HttpGet("connectors")]
    [RequirePermission("Integration.Connector.Read")]
    public Task<IActionResult> ListConnectors([FromQuery] Guid organizationId, CancellationToken ct)
        => mediator.Send(new ListConnectorsQuery(V(), organizationId), ct);

    [HttpGet("connectors/providers")]
    [RequirePermission("Integration.Connector.Read")]
    public Task<IActionResult> ListProviders(CancellationToken ct)
        => mediator.Send(new ListProviderTypesQuery(V()), ct);

    [HttpGet("connectors/{id:guid}")]
    [RequirePermission("Integration.Connector.Read")]
    public Task<IActionResult> GetConnector(Guid id, CancellationToken ct)
        => mediator.Send(new GetConnectorQuery(V(), id), ct);

    [HttpPost("connectors")]
    [RequirePermission("Integration.Connector.Manage")]
    public Task<IActionResult> CreateConnector([FromBody] object body, CancellationToken ct)
        => mediator.Send(new CreateConnectorQuery(V(), body), ct);

    [HttpPut("connectors/{id:guid}")]
    [RequirePermission("Integration.Connector.Manage")]
    public Task<IActionResult> UpdateConnector(Guid id, [FromBody] object body, CancellationToken ct)
        => mediator.Send(new UpdateConnectorQuery(V(), id, body), ct);

    [HttpPost("connectors/{id:guid}/validate")]
    [RequirePermission("Integration.Connector.Manage")]
    public Task<IActionResult> ValidateConnector(Guid id, CancellationToken ct)
        => mediator.Send(new ValidateConnectorQuery(V(), id), ct);

    [HttpGet("synchronizations")]
    [RequirePermission("Integration.Sync.Read")]
    public Task<IActionResult> ListSyncJobs([FromQuery] Guid organizationId, CancellationToken ct)
        => mediator.Send(new ListSyncJobsQuery(V(), organizationId), ct);

    [HttpGet("synchronizations/{jobId:guid}/runs")]
    [RequirePermission("Integration.Sync.Read")]
    public Task<IActionResult> ListSyncRuns(Guid jobId, CancellationToken ct)
        => mediator.Send(new ListSyncRunsQuery(V(), jobId), ct);

    [HttpPost("synchronizations")]
    [RequirePermission("Integration.Sync.Execute")]
    public Task<IActionResult> CreateSyncJob([FromBody] object body, CancellationToken ct)
        => mediator.Send(new CreateSyncJobQuery(V(), body), ct);

    [HttpPost("synchronizations/run")]
    [RequirePermission("Integration.Sync.Execute")]
    public Task<IActionResult> RunSync([FromQuery] Guid jobId, CancellationToken ct)
        => mediator.Send(new RunSyncQuery(V(), jobId), ct);

    [HttpGet("mappings")]
    [RequirePermission("Integration.Mapping.Read")]
    public Task<IActionResult> ListMappings([FromQuery] Guid organizationId, CancellationToken ct)
        => mediator.Send(new ListMappingsQuery(V(), organizationId), ct);

    [HttpPost("mappings")]
    [RequirePermission("Integration.Mapping.Manage")]
    public Task<IActionResult> CreateMapping([FromBody] object body, CancellationToken ct)
        => mediator.Send(new CreateMappingQuery(V(), body), ct);

    [HttpGet("webhooks")]
    [RequirePermission("Integration.Webhook.Read")]
    public Task<IActionResult> ListWebhooks([FromQuery] Guid organizationId, CancellationToken ct)
        => mediator.Send(new ListWebhooksQuery(V(), organizationId), ct);

    [HttpPost("webhooks")]
    [RequirePermission("Integration.Webhook.Manage")]
    public Task<IActionResult> CreateWebhook([FromBody] object body, CancellationToken ct)
        => mediator.Send(new CreateWebhookQuery(V(), body), ct);

    [HttpGet("webhooks/{subscriptionId:guid}/deliveries")]
    [RequirePermission("Integration.Webhook.Read")]
    public Task<IActionResult> ListWebhookDeliveries(Guid subscriptionId, CancellationToken ct)
        => mediator.Send(new ListWebhookDeliveriesQuery(V(), subscriptionId), ct);

    [HttpPost("webhooks/inbound/{subscriptionId:guid}")]
    [AllowAnonymous]
    public Task<IActionResult> InboundWebhook(Guid subscriptionId, [FromBody] object body, CancellationToken ct)
        => mediator.Send(new ProxyInboundWebhookQuery(V(), subscriptionId, body), ct);

    [HttpPost("imports")]
    [RequirePermission("Integration.Import.Execute")]
    public Task<IActionResult> CreateImport([FromBody] object body, CancellationToken ct)
        => mediator.Send(new CreateImportQuery(V(), body), ct);

    [HttpPost("imports/{id:guid}/preview")]
    [RequirePermission("Integration.Import.Execute")]
    public Task<IActionResult> PreviewImport(Guid id, CancellationToken ct)
        => mediator.Send(new PreviewImportQuery(V(), id), ct);

    [HttpPost("imports/{id:guid}/execute")]
    [RequirePermission("Integration.Import.Execute")]
    public Task<IActionResult> ExecuteImport(Guid id, CancellationToken ct)
        => mediator.Send(new ExecuteImportQuery(V(), id), ct);

    [HttpPost("imports/{id:guid}/rollback")]
    [RequirePermission("Integration.Import.Execute")]
    public Task<IActionResult> RollbackImport(Guid id, CancellationToken ct)
        => mediator.Send(new RollbackImportQuery(V(), id), ct);

    [HttpPost("exports")]
    [RequirePermission("Integration.Export.Execute")]
    public Task<IActionResult> CreateExport([FromBody] object body, CancellationToken ct)
        => mediator.Send(new CreateExportQuery(V(), body), ct);

    [HttpPost("exports/{id:guid}/execute")]
    [RequirePermission("Integration.Export.Execute")]
    public Task<IActionResult> ExecuteExport(Guid id, CancellationToken ct)
        => mediator.Send(new ExecuteExportQuery(V(), id), ct);

    [HttpGet("connector-health")]
    [RequirePermission("Integration.Monitor.Read")]
    public Task<IActionResult> ConnectorHealth([FromQuery] Guid organizationId, CancellationToken ct)
        => mediator.Send(new GetConnectorHealthQuery(V(), organizationId), ct);

    [HttpGet("audit")]
    [RequirePermission("Integration.Audit.Read")]
    public Task<IActionResult> Audit([FromQuery] Guid organizationId, CancellationToken ct)
        => mediator.Send(new ListIntegrationAuditQuery(V(), organizationId), ct);
}

public sealed record ProxyInboundWebhookQuery(string ApiVersion, Guid SubscriptionId, object Body) : MediatR.IRequest<IActionResult>;

public sealed class ProxyInboundWebhookHandler(Gateway.Services.IProxyService proxy) : MediatR.IRequestHandler<ProxyInboundWebhookQuery, IActionResult>
{
    public Task<IActionResult> Handle(ProxyInboundWebhookQuery r, CancellationToken ct)
        => proxy.ProxyToIntegrationServiceAsync($"/api/v{r.ApiVersion}/webhooks/inbound/{r.SubscriptionId}", HttpMethod.Post, r.Body, forwardAuthHeader: false, cancellationToken: ct);
}
