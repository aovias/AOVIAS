using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Gateway.Application.Queries;

public record ListSlaPoliciesQuery(Guid OrganizationId, Guid? HospitalId, string ApiVersion) : IRequest<IActionResult>;
public record CreateSlaPolicyCommand(object Body, string ApiVersion) : IRequest<IActionResult>;
public record GetDeficiencySlaQuery(Guid DeficiencyId, string ApiVersion) : IRequest<IActionResult>;
public record GetDeficiencyEscalationsQuery(Guid DeficiencyId, string ApiVersion) : IRequest<IActionResult>;
public record AcknowledgeEscalationCommand(Guid DeficiencyId, object Body, string ApiVersion) : IRequest<IActionResult>;
public record GetSlaMetricsQuery(Guid OrganizationId, string ApiVersion) : IRequest<IActionResult>;
