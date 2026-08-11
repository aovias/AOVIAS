using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Gateway.Application.Queries;

public record GetAuditFindingsReportQuery(
    string ApiVersion,
    Guid HospitalId,
    Guid? FrameworkId,
    DateTime? From,
    DateTime? To) : IRequest<IActionResult>;
