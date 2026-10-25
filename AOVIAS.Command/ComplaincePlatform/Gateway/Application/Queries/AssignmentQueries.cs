using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Gateway.Application.Queries;

public record GetDeficiencyAssignmentSuggestionQuery(Guid DeficiencyId, string ApiVersion) : IRequest<IActionResult>;
public record AutoAssignDeficiencyCommand(Guid DeficiencyId, bool Force, string ApiVersion) : IRequest<IActionResult>;
public record GetFindingAssignmentSuggestionQuery(Guid FindingId, Guid OrganizationId, Guid HospitalId, string ApiVersion) : IRequest<IActionResult>;
public record GetAssignmentRoutingMetricsQuery(Guid OrganizationId, string ApiVersion) : IRequest<IActionResult>;
public record GetDepartmentLabelMappingsQuery(string ApiVersion) : IRequest<IActionResult>;
public record ListDepartmentOwnershipQuery(Guid OrganizationId, Guid? HospitalId, string ApiVersion) : IRequest<IActionResult>;
public record UpsertDepartmentOwnershipCommand(object Body, string ApiVersion) : IRequest<IActionResult>;
