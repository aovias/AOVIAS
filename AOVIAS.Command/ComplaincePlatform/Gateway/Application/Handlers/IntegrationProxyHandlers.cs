using Gateway.Application.Queries;
using Gateway.Services;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Gateway.Application.Handlers;

public static class IntegrationProxyHandlers
{
    public sealed class ListConnectorsHandler(IProxyService proxy) : IRequestHandler<ListConnectorsQuery, IActionResult>
    {
        public Task<IActionResult> Handle(ListConnectorsQuery r, CancellationToken ct)
            => proxy.ProxyToIntegrationServiceAsync($"/api/v{r.ApiVersion}/connectors?organizationId={r.OrganizationId}", HttpMethod.Get, cancellationToken: ct);
    }

    public sealed class ListProviderTypesHandler(IProxyService proxy) : IRequestHandler<ListProviderTypesQuery, IActionResult>
    {
        public Task<IActionResult> Handle(ListProviderTypesQuery r, CancellationToken ct)
            => proxy.ProxyToIntegrationServiceAsync($"/api/v{r.ApiVersion}/connectors/providers", HttpMethod.Get, cancellationToken: ct);
    }

    public sealed class GetConnectorHandler(IProxyService proxy) : IRequestHandler<GetConnectorQuery, IActionResult>
    {
        public Task<IActionResult> Handle(GetConnectorQuery r, CancellationToken ct)
            => proxy.ProxyToIntegrationServiceAsync($"/api/v{r.ApiVersion}/connectors/{r.Id}", HttpMethod.Get, cancellationToken: ct);
    }

    public sealed class CreateConnectorHandler(IProxyService proxy) : IRequestHandler<CreateConnectorQuery, IActionResult>
    {
        public Task<IActionResult> Handle(CreateConnectorQuery r, CancellationToken ct)
            => proxy.ProxyToIntegrationServiceAsync($"/api/v{r.ApiVersion}/connectors", HttpMethod.Post, r.Body, cancellationToken: ct);
    }

    public sealed class UpdateConnectorHandler(IProxyService proxy) : IRequestHandler<UpdateConnectorQuery, IActionResult>
    {
        public Task<IActionResult> Handle(UpdateConnectorQuery r, CancellationToken ct)
            => proxy.ProxyToIntegrationServiceAsync($"/api/v{r.ApiVersion}/connectors/{r.Id}", HttpMethod.Put, r.Body, cancellationToken: ct);
    }

    public sealed class ValidateConnectorHandler(IProxyService proxy) : IRequestHandler<ValidateConnectorQuery, IActionResult>
    {
        public Task<IActionResult> Handle(ValidateConnectorQuery r, CancellationToken ct)
            => proxy.ProxyToIntegrationServiceAsync($"/api/v{r.ApiVersion}/connectors/{r.Id}/validate", HttpMethod.Post, cancellationToken: ct);
    }

    public sealed class ListSyncJobsHandler(IProxyService proxy) : IRequestHandler<ListSyncJobsQuery, IActionResult>
    {
        public Task<IActionResult> Handle(ListSyncJobsQuery r, CancellationToken ct)
            => proxy.ProxyToIntegrationServiceAsync($"/api/v{r.ApiVersion}/synchronizations?organizationId={r.OrganizationId}", HttpMethod.Get, cancellationToken: ct);
    }

    public sealed class ListSyncRunsHandler(IProxyService proxy) : IRequestHandler<ListSyncRunsQuery, IActionResult>
    {
        public Task<IActionResult> Handle(ListSyncRunsQuery r, CancellationToken ct)
            => proxy.ProxyToIntegrationServiceAsync($"/api/v{r.ApiVersion}/synchronizations/{r.JobId}/runs", HttpMethod.Get, cancellationToken: ct);
    }

    public sealed class CreateSyncJobHandler(IProxyService proxy) : IRequestHandler<CreateSyncJobQuery, IActionResult>
    {
        public Task<IActionResult> Handle(CreateSyncJobQuery r, CancellationToken ct)
            => proxy.ProxyToIntegrationServiceAsync($"/api/v{r.ApiVersion}/synchronizations", HttpMethod.Post, r.Body, cancellationToken: ct);
    }

    public sealed class RunSyncHandler(IProxyService proxy) : IRequestHandler<RunSyncQuery, IActionResult>
    {
        public Task<IActionResult> Handle(RunSyncQuery r, CancellationToken ct)
            => proxy.ProxyToIntegrationServiceAsync($"/api/v{r.ApiVersion}/synchronizations/run?jobId={r.JobId}", HttpMethod.Post, cancellationToken: ct);
    }

    public sealed class ListMappingsHandler(IProxyService proxy) : IRequestHandler<ListMappingsQuery, IActionResult>
    {
        public Task<IActionResult> Handle(ListMappingsQuery r, CancellationToken ct)
            => proxy.ProxyToIntegrationServiceAsync($"/api/v{r.ApiVersion}/mappings?organizationId={r.OrganizationId}", HttpMethod.Get, cancellationToken: ct);
    }

    public sealed class CreateMappingHandler(IProxyService proxy) : IRequestHandler<CreateMappingQuery, IActionResult>
    {
        public Task<IActionResult> Handle(CreateMappingQuery r, CancellationToken ct)
            => proxy.ProxyToIntegrationServiceAsync($"/api/v{r.ApiVersion}/mappings", HttpMethod.Post, r.Body, cancellationToken: ct);
    }

    public sealed class ListWebhooksHandler(IProxyService proxy) : IRequestHandler<ListWebhooksQuery, IActionResult>
    {
        public Task<IActionResult> Handle(ListWebhooksQuery r, CancellationToken ct)
            => proxy.ProxyToIntegrationServiceAsync($"/api/v{r.ApiVersion}/webhooks?organizationId={r.OrganizationId}", HttpMethod.Get, cancellationToken: ct);
    }

    public sealed class CreateWebhookHandler(IProxyService proxy) : IRequestHandler<CreateWebhookQuery, IActionResult>
    {
        public Task<IActionResult> Handle(CreateWebhookQuery r, CancellationToken ct)
            => proxy.ProxyToIntegrationServiceAsync($"/api/v{r.ApiVersion}/webhooks", HttpMethod.Post, r.Body, cancellationToken: ct);
    }

    public sealed class ListWebhookDeliveriesHandler(IProxyService proxy) : IRequestHandler<ListWebhookDeliveriesQuery, IActionResult>
    {
        public Task<IActionResult> Handle(ListWebhookDeliveriesQuery r, CancellationToken ct)
            => proxy.ProxyToIntegrationServiceAsync($"/api/v{r.ApiVersion}/webhooks/{r.SubscriptionId}/deliveries", HttpMethod.Get, cancellationToken: ct);
    }

    public sealed class CreateImportHandler(IProxyService proxy) : IRequestHandler<CreateImportQuery, IActionResult>
    {
        public Task<IActionResult> Handle(CreateImportQuery r, CancellationToken ct)
            => proxy.ProxyToIntegrationServiceAsync($"/api/v{r.ApiVersion}/imports", HttpMethod.Post, r.Body, cancellationToken: ct);
    }

    public sealed class PreviewImportHandler(IProxyService proxy) : IRequestHandler<PreviewImportQuery, IActionResult>
    {
        public Task<IActionResult> Handle(PreviewImportQuery r, CancellationToken ct)
            => proxy.ProxyToIntegrationServiceAsync($"/api/v{r.ApiVersion}/imports/{r.Id}/preview", HttpMethod.Post, cancellationToken: ct);
    }

    public sealed class ExecuteImportHandler(IProxyService proxy) : IRequestHandler<ExecuteImportQuery, IActionResult>
    {
        public Task<IActionResult> Handle(ExecuteImportQuery r, CancellationToken ct)
            => proxy.ProxyToIntegrationServiceAsync($"/api/v{r.ApiVersion}/imports/{r.Id}/execute", HttpMethod.Post, cancellationToken: ct);
    }

    public sealed class RollbackImportHandler(IProxyService proxy) : IRequestHandler<RollbackImportQuery, IActionResult>
    {
        public Task<IActionResult> Handle(RollbackImportQuery r, CancellationToken ct)
            => proxy.ProxyToIntegrationServiceAsync($"/api/v{r.ApiVersion}/imports/{r.Id}/rollback", HttpMethod.Post, cancellationToken: ct);
    }

    public sealed class CreateExportHandler(IProxyService proxy) : IRequestHandler<CreateExportQuery, IActionResult>
    {
        public Task<IActionResult> Handle(CreateExportQuery r, CancellationToken ct)
            => proxy.ProxyToIntegrationServiceAsync($"/api/v{r.ApiVersion}/exports", HttpMethod.Post, r.Body, cancellationToken: ct);
    }

    public sealed class ExecuteExportHandler(IProxyService proxy) : IRequestHandler<ExecuteExportQuery, IActionResult>
    {
        public Task<IActionResult> Handle(ExecuteExportQuery r, CancellationToken ct)
            => proxy.ProxyToIntegrationServiceAsync($"/api/v{r.ApiVersion}/exports/{r.Id}/execute", HttpMethod.Post, cancellationToken: ct);
    }

    public sealed class GetConnectorHealthHandler(IProxyService proxy) : IRequestHandler<GetConnectorHealthQuery, IActionResult>
    {
        public Task<IActionResult> Handle(GetConnectorHealthQuery r, CancellationToken ct)
            => proxy.ProxyToIntegrationServiceAsync($"/api/v{r.ApiVersion}/connector-health?organizationId={r.OrganizationId}", HttpMethod.Get, cancellationToken: ct);
    }

    public sealed class ListIntegrationAuditHandler(IProxyService proxy) : IRequestHandler<ListIntegrationAuditQuery, IActionResult>
    {
        public Task<IActionResult> Handle(ListIntegrationAuditQuery r, CancellationToken ct)
            => proxy.ProxyToIntegrationServiceAsync($"/api/v{r.ApiVersion}/integration-audit?organizationId={r.OrganizationId}", HttpMethod.Get, cancellationToken: ct);
    }
}
