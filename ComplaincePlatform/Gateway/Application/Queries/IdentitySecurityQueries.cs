using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Gateway.Application.Queries;

public sealed record ListSecurityEventsQuery(string ApiVersion, int Page, int PageSize, Guid? UserId) : IRequest<IActionResult>;
public sealed record GetSecurityDashboardQuery(string ApiVersion) : IRequest<IActionResult>;
public sealed record GetUserRiskQuery(string ApiVersion, Guid? UserId) : IRequest<IActionResult>;
public sealed record ListTrustedDevicesQuery(string ApiVersion, Guid? UserId) : IRequest<IActionResult>;
public sealed record GetSecurityTimelineQuery(string ApiVersion, Guid? UserId, int Take) : IRequest<IActionResult>;
