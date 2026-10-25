using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Gateway.Application.Queries;

public record GetScoringEnrichedV11Query(
    Guid HospitalId,
    Guid FrameworkId,
    string ApiVersion) : IRequest<IActionResult>;
