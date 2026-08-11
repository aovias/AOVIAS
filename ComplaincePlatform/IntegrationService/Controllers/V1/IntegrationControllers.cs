using IntegrationService.Application.Contracts;
using IntegrationService.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Authorization;
using Shared.Infrastructure.Api;
using Shared.Infrastructure.DTO;

namespace IntegrationService.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/connectors")]
[Authorize]
public class ConnectorsController(IMediator mediator) : ControllerBase
{
    private string TraceId => HttpContext.TraceIdentifier;

    [HttpGet]
    [RequirePermission("Integration.Connector.Read")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ConnectorDto>>>> List([FromQuery] Guid organizationId, CancellationToken ct)
        => Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, await mediator.Send(new ListConnectorsQuery(organizationId), ct), TraceId));

    [HttpGet("providers")]
    [RequirePermission("Integration.Connector.Read")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<string>>>> Providers(CancellationToken ct)
        => Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, await mediator.Send(new ListProviderTypesQuery(), ct), TraceId));

    [HttpGet("{id:guid}")]
    [RequirePermission("Integration.Connector.Read")]
    public async Task<ActionResult<ApiResponse<ConnectorDto>>> Get(Guid id, CancellationToken ct)
    {
        var data = await mediator.Send(new GetConnectorQuery(id), ct);
        if (data is null) return NotFound(ApiResponseFactory.Failure<ConnectorDto>(ApiResponseCode.RESOURCE_NOT_FOUND, TraceId));
        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, data, TraceId));
    }

    [HttpPost]
    [RequirePermission("Integration.Connector.Manage")]
    public async Task<ActionResult<ApiResponse<ConnectorDto>>> Create([FromBody] CreateConnectorRequest request, CancellationToken ct)
        => Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, await mediator.Send(new CreateConnectorCommand(request), ct), TraceId));

    [HttpPut("{id:guid}")]
    [RequirePermission("Integration.Connector.Manage")]
    public async Task<ActionResult<ApiResponse<ConnectorDto>>> Update(Guid id, [FromBody] UpdateConnectorRequest request, CancellationToken ct)
    {
        var data = await mediator.Send(new UpdateConnectorCommand(id, request), ct);
        if (data is null) return NotFound(ApiResponseFactory.Failure<ConnectorDto>(ApiResponseCode.RESOURCE_NOT_FOUND, TraceId));
        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, data, TraceId));
    }

    [HttpPost("{id:guid}/validate")]
    [RequirePermission("Integration.Connector.Manage")]
    public async Task<ActionResult<ApiResponse<ConnectorHealthResult>>> Validate(Guid id, CancellationToken ct)
        => Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, await mediator.Send(new ValidateConnectorCommand(id), ct), TraceId));
}

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/synchronizations")]
[Authorize]
public class SynchronizationsController(IMediator mediator) : ControllerBase
{
    private string TraceId => HttpContext.TraceIdentifier;

    [HttpGet]
    [RequirePermission("Integration.Sync.Read")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<SynchronizationJobDto>>>> List([FromQuery] Guid organizationId, CancellationToken ct)
        => Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, await mediator.Send(new ListSyncJobsQuery(organizationId), ct), TraceId));

    [HttpGet("{jobId:guid}/runs")]
    [RequirePermission("Integration.Sync.Read")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<SynchronizationRunDto>>>> Runs(Guid jobId, CancellationToken ct)
        => Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, await mediator.Send(new ListSyncRunsQuery(jobId), ct), TraceId));

    [HttpPost]
    [RequirePermission("Integration.Sync.Execute")]
    public async Task<ActionResult<ApiResponse<SynchronizationJobDto>>> Create([FromBody] CreateSyncJobRequest request, CancellationToken ct)
        => Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, await mediator.Send(new CreateSyncJobCommand(request), ct), TraceId));

    [HttpPost("run")]
    [RequirePermission("Integration.Sync.Execute")]
    public async Task<ActionResult<ApiResponse<SynchronizationRunDto>>> Run([FromQuery] Guid jobId, CancellationToken ct)
        => Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, await mediator.Send(new RunSyncCommand(jobId, TraceId), ct), TraceId));
}

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/mappings")]
[Authorize]
public class MappingsController(IMediator mediator) : ControllerBase
{
    private string TraceId => HttpContext.TraceIdentifier;

    [HttpGet]
    [RequirePermission("Integration.Mapping.Read")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<MappingProfileDto>>>> List([FromQuery] Guid organizationId, CancellationToken ct)
        => Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, await mediator.Send(new ListMappingsQuery(organizationId), ct), TraceId));

    [HttpPost]
    [RequirePermission("Integration.Mapping.Manage")]
    public async Task<ActionResult<ApiResponse<MappingProfileDto>>> Create([FromBody] CreateMappingProfileRequest request, CancellationToken ct)
        => Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, await mediator.Send(new CreateMappingCommand(request), ct), TraceId));
}

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/webhooks")]
[Authorize]
public class WebhooksController(IMediator mediator, Application.Integration.Webhooks.WebhookService webhookService) : ControllerBase
{
    private string TraceId => HttpContext.TraceIdentifier;

    [HttpGet]
    [RequirePermission("Integration.Webhook.Read")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<WebhookSubscriptionDto>>>> List([FromQuery] Guid organizationId, CancellationToken ct)
        => Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, await mediator.Send(new ListWebhooksQuery(organizationId), ct), TraceId));

    [HttpPost]
    [RequirePermission("Integration.Webhook.Manage")]
    public async Task<ActionResult<ApiResponse<WebhookSubscriptionDto>>> Create([FromBody] CreateWebhookRequest request, CancellationToken ct)
        => Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, await mediator.Send(new CreateWebhookCommand(request), ct), TraceId));

    [HttpPost("inbound/{subscriptionId:guid}")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<object>>> Inbound(
        Guid subscriptionId,
        [FromBody] object body,
        [FromHeader(Name = "X-Praman-Signature")] string? signature,
        [FromHeader(Name = "X-Praman-Timestamp")] string? timestamp,
        [FromHeader(Name = "X-Praman-Idempotency-Key")] string? idempotencyKey,
        CancellationToken ct)
    {
        var payload = System.Text.Json.JsonSerializer.Serialize(body);
        var ok = await webhookService.ProcessInboundAsync(subscriptionId, payload, signature ?? "", timestamp, idempotencyKey, ct);
        if (!ok)
            return Unauthorized(ApiResponseFactory.Failure<object>(ApiResponseCode.UNAUTHORIZED, TraceId));
        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, new { accepted = true }, TraceId));
    }

    [HttpGet("{subscriptionId:guid}/deliveries")]
    [RequirePermission("Integration.Webhook.Read")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<WebhookDeliveryDto>>>> Deliveries(Guid subscriptionId, CancellationToken ct)
        => Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, await webhookService.ListDeliveriesAsync(subscriptionId, ct), TraceId));
}

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/imports")]
[Authorize]
public class ImportsController(IMediator mediator) : ControllerBase
{
    private string TraceId => HttpContext.TraceIdentifier;

    [HttpPost]
    [RequirePermission("Integration.Import.Execute")]
    public async Task<ActionResult<ApiResponse<ImportJobDto>>> Create([FromBody] CreateImportRequest request, CancellationToken ct)
        => Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, await mediator.Send(new CreateImportCommand(request), ct), TraceId));

    [HttpPost("{id:guid}/preview")]
    [RequirePermission("Integration.Import.Execute")]
    public async Task<ActionResult<ApiResponse<ImportJobDto>>> Preview(Guid id, CancellationToken ct)
        => Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, await mediator.Send(new PreviewImportCommand(id), ct), TraceId));

    [HttpPost("{id:guid}/execute")]
    [RequirePermission("Integration.Import.Execute")]
    public async Task<ActionResult<ApiResponse<ImportJobDto>>> Execute(Guid id, CancellationToken ct)
        => Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, await mediator.Send(new ExecuteImportCommand(id), ct), TraceId));

    [HttpPost("{id:guid}/rollback")]
    [RequirePermission("Integration.Import.Execute")]
    public async Task<ActionResult<ApiResponse<ImportJobDto>>> Rollback(Guid id, CancellationToken ct)
    {
        try
        {
            return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, await mediator.Send(new RollbackImportCommand(id), ct), TraceId));
        }
        catch (InvalidOperationException)
        {
            return BadRequest(ApiResponseFactory.Failure<ImportJobDto>(ApiResponseCode.VALIDATION_FAILED, TraceId));
        }
    }
}

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/exports")]
[Authorize]
public class ExportsController(IMediator mediator) : ControllerBase
{
    private string TraceId => HttpContext.TraceIdentifier;

    [HttpPost]
    [RequirePermission("Integration.Export.Execute")]
    public async Task<ActionResult<ApiResponse<ExportJobDto>>> Create([FromBody] CreateExportRequest request, CancellationToken ct)
        => Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, await mediator.Send(new CreateExportCommand(request), ct), TraceId));

    [HttpPost("{id:guid}/execute")]
    [RequirePermission("Integration.Export.Execute")]
    public async Task<ActionResult<ApiResponse<ExportJobDto>>> Execute(Guid id, CancellationToken ct)
        => Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, await mediator.Send(new ExecuteExportCommand(id), ct), TraceId));
}

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/connector-health")]
[Authorize]
public class ConnectorHealthController(IMediator mediator) : ControllerBase
{
    private string TraceId => HttpContext.TraceIdentifier;

    [HttpGet]
    [RequirePermission("Integration.Monitor.Read")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ConnectorHealthDto>>>> Get([FromQuery] Guid organizationId, CancellationToken ct)
        => Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, await mediator.Send(new GetConnectorHealthQuery(organizationId), ct), TraceId));
}

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/integration-audit")]
[Authorize]
public class IntegrationAuditController(IMediator mediator) : ControllerBase
{
    private string TraceId => HttpContext.TraceIdentifier;

    [HttpGet]
    [RequirePermission("Integration.Audit.Read")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<IntegrationAuditDto>>>> List([FromQuery] Guid organizationId, CancellationToken ct)
        => Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, await mediator.Send(new ListAuditQuery(organizationId), ct), TraceId));
}
