using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Gateway.Application.Queries;

public sealed record ListConnectorsQuery(string ApiVersion, Guid OrganizationId) : IRequest<IActionResult>;
public sealed record ListProviderTypesQuery(string ApiVersion) : IRequest<IActionResult>;
public sealed record GetConnectorQuery(string ApiVersion, Guid Id) : IRequest<IActionResult>;
public sealed record CreateConnectorQuery(string ApiVersion, object Body) : IRequest<IActionResult>;
public sealed record UpdateConnectorQuery(string ApiVersion, Guid Id, object Body) : IRequest<IActionResult>;
public sealed record ValidateConnectorQuery(string ApiVersion, Guid Id) : IRequest<IActionResult>;
public sealed record ListSyncJobsQuery(string ApiVersion, Guid OrganizationId) : IRequest<IActionResult>;
public sealed record ListSyncRunsQuery(string ApiVersion, Guid JobId) : IRequest<IActionResult>;
public sealed record CreateSyncJobQuery(string ApiVersion, object Body) : IRequest<IActionResult>;
public sealed record RunSyncQuery(string ApiVersion, Guid JobId) : IRequest<IActionResult>;
public sealed record ListMappingsQuery(string ApiVersion, Guid OrganizationId) : IRequest<IActionResult>;
public sealed record CreateMappingQuery(string ApiVersion, object Body) : IRequest<IActionResult>;
public sealed record ListWebhooksQuery(string ApiVersion, Guid OrganizationId) : IRequest<IActionResult>;
public sealed record CreateWebhookQuery(string ApiVersion, object Body) : IRequest<IActionResult>;
public sealed record ListWebhookDeliveriesQuery(string ApiVersion, Guid SubscriptionId) : IRequest<IActionResult>;
public sealed record CreateImportQuery(string ApiVersion, object Body) : IRequest<IActionResult>;
public sealed record PreviewImportQuery(string ApiVersion, Guid Id) : IRequest<IActionResult>;
public sealed record ExecuteImportQuery(string ApiVersion, Guid Id) : IRequest<IActionResult>;
public sealed record RollbackImportQuery(string ApiVersion, Guid Id) : IRequest<IActionResult>;
public sealed record CreateExportQuery(string ApiVersion, object Body) : IRequest<IActionResult>;
public sealed record ExecuteExportQuery(string ApiVersion, Guid Id) : IRequest<IActionResult>;
public sealed record GetConnectorHealthQuery(string ApiVersion, Guid OrganizationId) : IRequest<IActionResult>;
public sealed record ListIntegrationAuditQuery(string ApiVersion, Guid OrganizationId) : IRequest<IActionResult>;
