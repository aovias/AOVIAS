using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Gateway.Application.Queries;

public sealed record ListComplianceTasksQuery(string ApiVersion, Guid? FrameworkId) : IRequest<IActionResult>;

public sealed record GetComplianceTaskByIdQuery(string ApiVersion, Guid TaskId) : IRequest<IActionResult>;
