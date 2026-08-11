using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Gateway.Application.Queries;

public record ListEmploymentsQuery(string ApiVersion, int Page, int PageSize, Guid? OrganizationId, Guid? HospitalId, Guid? StatusValueId) : IRequest<IActionResult>;
public record SearchEmploymentsQuery(string ApiVersion, string Query, int Page, int PageSize) : IRequest<IActionResult>;
public record GetEmploymentByIdQuery(string ApiVersion, Guid EmploymentId) : IRequest<IActionResult>;
public record GetEmploymentHistoryQuery(string ApiVersion, Guid EmploymentId) : IRequest<IActionResult>;
public record GetEmploymentTimelineQuery(string ApiVersion, Guid EmploymentId) : IRequest<IActionResult>;
public record GetWorkforceLookupsQuery(string ApiVersion, string Category) : IRequest<IActionResult>;
public record GetOrgChartQuery(string ApiVersion, Guid OrganizationId, Guid? HospitalId, Guid? RootEmploymentId) : IRequest<IActionResult>;
public record GetManagerTeamQuery(string ApiVersion, Guid ManagerEmploymentId) : IRequest<IActionResult>;
public record GetPositionsQuery(string ApiVersion, Guid OrganizationId) : IRequest<IActionResult>;
