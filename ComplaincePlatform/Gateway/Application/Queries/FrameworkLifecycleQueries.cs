using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Gateway.Application.Queries;

public record GetComplianceFrameworkCatalogQuery(bool IncludeInactive, string ApiVersion) : IRequest<IActionResult>;

public record PublishFrameworkCommand(Guid FrameworkId, string ApiVersion) : IRequest<IActionResult>;

public record ActivateFrameworkCommand(Guid FrameworkId, string ApiVersion) : IRequest<IActionResult>;

public record RetireFrameworkCommand(Guid FrameworkId, string ApiVersion) : IRequest<IActionResult>;

public record ArchiveFrameworkCommand(Guid FrameworkId, string ApiVersion) : IRequest<IActionResult>;
