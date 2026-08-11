using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Gateway.Application.Queries;

public record GetHospitalFrameworksQuery(Guid HospitalId, string ApiVersion) : IRequest<IActionResult>;
