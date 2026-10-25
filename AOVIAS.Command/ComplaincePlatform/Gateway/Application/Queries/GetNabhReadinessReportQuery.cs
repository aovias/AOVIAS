using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Gateway.Application.Queries;

public record GetNabhReadinessReportQuery(string ApiVersion, Guid HospitalId) : IRequest<IActionResult>;
