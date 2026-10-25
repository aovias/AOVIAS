using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Gateway.Application.Commands;

public record EnrollHospitalInFrameworkCommand(
    Guid HospitalId,
    Guid FrameworkId,
    string ApiVersion) : IRequest<IActionResult>;
