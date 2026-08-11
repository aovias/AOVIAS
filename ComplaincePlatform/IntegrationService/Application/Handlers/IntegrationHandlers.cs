using System.Security.Claims;
using IntegrationService.Application.Contracts;
using IntegrationService.Application.Integration.ImportExport;
using IntegrationService.Application.Integration.Mapping;
using IntegrationService.Application.Integration.Platform;
using IntegrationService.Application.Integration.Synchronization;
using IntegrationService.Application.Integration.Webhooks;
using IntegrationService.Application.Queries;
using IntegrationService.Infrastructure.Services;
using MediatR;

namespace IntegrationService.Application.Handlers;

public static class IntegrationHandlers
{
    private static Guid UserId(ClaimsPrincipal user) =>
        Guid.TryParse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? user.FindFirst("sub")?.Value, out var id) ? id : Guid.Empty;

    public sealed class ListConnectorsHandler(ConnectorAppService svc) : IRequestHandler<ListConnectorsQuery, IReadOnlyList<ConnectorDto>>
    {
        public Task<IReadOnlyList<ConnectorDto>> Handle(ListConnectorsQuery r, CancellationToken ct) => svc.ListAsync(r.OrganizationId, ct);
    }
    public sealed class GetConnectorHandler(ConnectorAppService svc) : IRequestHandler<GetConnectorQuery, ConnectorDto?>
    {
        public Task<ConnectorDto?> Handle(GetConnectorQuery r, CancellationToken ct) => svc.GetAsync(r.Id, ct);
    }
    public sealed class CreateConnectorHandler(ConnectorAppService svc) : IRequestHandler<CreateConnectorCommand, ConnectorDto>
    {
        public Task<ConnectorDto> Handle(CreateConnectorCommand r, CancellationToken ct) => svc.CreateAsync(r.Request, ct);
    }
    public sealed class UpdateConnectorHandler(ConnectorAppService svc) : IRequestHandler<UpdateConnectorCommand, ConnectorDto?>
    {
        public Task<ConnectorDto?> Handle(UpdateConnectorCommand r, CancellationToken ct) => svc.UpdateAsync(r.Id, r.Request, ct);
    }
    public sealed class ValidateConnectorHandler(ConnectorAppService svc) : IRequestHandler<ValidateConnectorCommand, ConnectorHealthResult>
    {
        public Task<ConnectorHealthResult> Handle(ValidateConnectorCommand r, CancellationToken ct) => svc.ValidateAndConnectAsync(r.Id, ct);
    }
    public sealed class ListSyncJobsHandler(SynchronizationJobService svc) : IRequestHandler<ListSyncJobsQuery, IReadOnlyList<SynchronizationJobDto>>
    {
        public Task<IReadOnlyList<SynchronizationJobDto>> Handle(ListSyncJobsQuery r, CancellationToken ct) => svc.ListAsync(r.OrganizationId, ct);
    }
    public sealed class CreateSyncJobHandler(SynchronizationJobService svc) : IRequestHandler<CreateSyncJobCommand, SynchronizationJobDto>
    {
        public Task<SynchronizationJobDto> Handle(CreateSyncJobCommand r, CancellationToken ct) => svc.CreateAsync(r.Request, ct);
    }
    public sealed class RunSyncHandler(ISynchronizationEngine engine, IHttpContextAccessor http) : IRequestHandler<RunSyncCommand, SynchronizationRunDto>
    {
        public Task<SynchronizationRunDto> Handle(RunSyncCommand r, CancellationToken ct) => engine.RunAsync(r.JobId, r.TraceId, UserId(http.HttpContext!.User), ct);
    }
    public sealed class ListSyncRunsHandler(SynchronizationJobService svc) : IRequestHandler<ListSyncRunsQuery, IReadOnlyList<SynchronizationRunDto>>
    {
        public Task<IReadOnlyList<SynchronizationRunDto>> Handle(ListSyncRunsQuery r, CancellationToken ct) => svc.ListRunsAsync(r.JobId, ct);
    }
    public sealed class ListMappingsHandler(MappingProfileService svc) : IRequestHandler<ListMappingsQuery, IReadOnlyList<MappingProfileDto>>
    {
        public Task<IReadOnlyList<MappingProfileDto>> Handle(ListMappingsQuery r, CancellationToken ct) => svc.ListAsync(r.OrganizationId, ct);
    }
    public sealed class CreateMappingHandler(MappingProfileService svc) : IRequestHandler<CreateMappingCommand, MappingProfileDto>
    {
        public Task<MappingProfileDto> Handle(CreateMappingCommand r, CancellationToken ct) => svc.CreateAsync(r.Request, ct);
    }
    public sealed class ListWebhooksHandler(IWebhookService svc) : IRequestHandler<ListWebhooksQuery, IReadOnlyList<WebhookSubscriptionDto>>
    {
        public Task<IReadOnlyList<WebhookSubscriptionDto>> Handle(ListWebhooksQuery r, CancellationToken ct) => svc.ListAsync(r.OrganizationId, ct);
    }
    public sealed class CreateWebhookHandler(IWebhookService svc) : IRequestHandler<CreateWebhookCommand, WebhookSubscriptionDto>
    {
        public Task<WebhookSubscriptionDto> Handle(CreateWebhookCommand r, CancellationToken ct) => svc.CreateAsync(r.Request, ct);
    }
    public sealed class CreateImportHandler(IImportExportEngine svc) : IRequestHandler<CreateImportCommand, ImportJobDto>
    {
        public Task<ImportJobDto> Handle(CreateImportCommand r, CancellationToken ct) => svc.CreateImportAsync(r.Request, ct);
    }
    public sealed class PreviewImportHandler(IImportExportEngine svc) : IRequestHandler<PreviewImportCommand, ImportJobDto>
    {
        public Task<ImportJobDto> Handle(PreviewImportCommand r, CancellationToken ct) => svc.PreviewImportAsync(r.ImportId, ct);
    }
    public sealed class ExecuteImportHandler(IImportExportEngine svc, IHttpContextAccessor http) : IRequestHandler<ExecuteImportCommand, ImportJobDto>
    {
        public Task<ImportJobDto> Handle(ExecuteImportCommand r, CancellationToken ct) => svc.ExecuteImportAsync(r.ImportId, UserId(http.HttpContext!.User), ct);
    }
    public sealed class RollbackImportHandler(IImportExportEngine svc, IHttpContextAccessor http) : IRequestHandler<RollbackImportCommand, ImportJobDto>
    {
        public Task<ImportJobDto> Handle(RollbackImportCommand r, CancellationToken ct) => svc.RollbackImportAsync(r.ImportId, UserId(http.HttpContext!.User), ct);
    }
    public sealed class CreateExportHandler(IImportExportEngine svc) : IRequestHandler<CreateExportCommand, ExportJobDto>
    {
        public Task<ExportJobDto> Handle(CreateExportCommand r, CancellationToken ct) => svc.CreateExportAsync(r.Request, ct);
    }
    public sealed class ExecuteExportHandler(IImportExportEngine svc) : IRequestHandler<ExecuteExportCommand, ExportJobDto>
    {
        public Task<ExportJobDto> Handle(ExecuteExportCommand r, CancellationToken ct) => svc.ExecuteExportAsync(r.ExportId, ct);
    }
    public sealed class GetHealthHandler(ConnectorHealthService svc) : IRequestHandler<GetConnectorHealthQuery, IReadOnlyList<ConnectorHealthDto>>
    {
        public Task<IReadOnlyList<ConnectorHealthDto>> Handle(GetConnectorHealthQuery r, CancellationToken ct) => svc.GetHealthAsync(r.OrganizationId, ct);
    }
    public sealed class ListAuditHandler(IntegrationAuditService svc) : IRequestHandler<ListAuditQuery, IReadOnlyList<IntegrationAuditDto>>
    {
        public Task<IReadOnlyList<IntegrationAuditDto>> Handle(ListAuditQuery r, CancellationToken ct) => svc.ListAsync(r.OrganizationId, ct: ct);
    }
    public sealed class ListProvidersHandler(IConnectorRegistry registry) : IRequestHandler<ListProviderTypesQuery, IReadOnlyList<string>>
    {
        public Task<IReadOnlyList<string>> Handle(ListProviderTypesQuery r, CancellationToken ct) => Task.FromResult(registry.ListProviderTypes());
    }
}
