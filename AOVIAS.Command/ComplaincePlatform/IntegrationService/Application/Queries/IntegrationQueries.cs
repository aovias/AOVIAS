using IntegrationService.Application.Contracts;
using MediatR;

namespace IntegrationService.Application.Queries;

public sealed record ListConnectorsQuery(Guid OrganizationId) : IRequest<IReadOnlyList<ConnectorDto>>;
public sealed record GetConnectorQuery(Guid Id) : IRequest<ConnectorDto?>;
public sealed record CreateConnectorCommand(CreateConnectorRequest Request) : IRequest<ConnectorDto>;
public sealed record UpdateConnectorCommand(Guid Id, UpdateConnectorRequest Request) : IRequest<ConnectorDto?>;
public sealed record ValidateConnectorCommand(Guid Id) : IRequest<ConnectorHealthResult>;
public sealed record ListSyncJobsQuery(Guid OrganizationId) : IRequest<IReadOnlyList<SynchronizationJobDto>>;
public sealed record CreateSyncJobCommand(CreateSyncJobRequest Request) : IRequest<SynchronizationJobDto>;
public sealed record RunSyncCommand(Guid JobId, string TraceId) : IRequest<SynchronizationRunDto>;
public sealed record ListSyncRunsQuery(Guid JobId) : IRequest<IReadOnlyList<SynchronizationRunDto>>;
public sealed record ListMappingsQuery(Guid OrganizationId) : IRequest<IReadOnlyList<MappingProfileDto>>;
public sealed record CreateMappingCommand(CreateMappingProfileRequest Request) : IRequest<MappingProfileDto>;
public sealed record ListWebhooksQuery(Guid OrganizationId) : IRequest<IReadOnlyList<WebhookSubscriptionDto>>;
public sealed record CreateWebhookCommand(CreateWebhookRequest Request) : IRequest<WebhookSubscriptionDto>;
public sealed record CreateImportCommand(CreateImportRequest Request) : IRequest<ImportJobDto>;
public sealed record PreviewImportCommand(Guid ImportId) : IRequest<ImportJobDto>;
public sealed record ExecuteImportCommand(Guid ImportId) : IRequest<ImportJobDto>;
public sealed record RollbackImportCommand(Guid ImportId) : IRequest<ImportJobDto>;
public sealed record CreateExportCommand(CreateExportRequest Request) : IRequest<ExportJobDto>;
public sealed record ExecuteExportCommand(Guid ExportId) : IRequest<ExportJobDto>;
public sealed record GetConnectorHealthQuery(Guid OrganizationId) : IRequest<IReadOnlyList<ConnectorHealthDto>>;
public sealed record ListAuditQuery(Guid OrganizationId) : IRequest<IReadOnlyList<IntegrationAuditDto>>;
public sealed record ListProviderTypesQuery : IRequest<IReadOnlyList<string>>;
