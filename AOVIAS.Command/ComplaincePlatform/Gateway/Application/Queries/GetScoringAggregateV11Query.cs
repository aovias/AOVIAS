using MediatR;
using Microsoft.AspNetCore.Mvc;
using Shared.Infrastructure.Domain.Enums;

namespace Gateway.Application.Queries;

public record GetScoringAggregateV11Query(
    Guid HospitalId,
    Guid FrameworkId,
    Guid JourneyId,
    string ApiVersion,
    OperationalMode OperationalMode) : IRequest<IActionResult>;
