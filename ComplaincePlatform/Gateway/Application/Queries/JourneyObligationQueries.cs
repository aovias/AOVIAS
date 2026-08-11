using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Gateway.Application.Queries;

public record GetJourneyObligationOverviewQuery(
    Guid HospitalId,
    Guid FrameworkId,
    string ApiVersion) : IRequest<IActionResult>;

public record GetJourneyObligationsQuery(
    Guid HospitalId,
    Guid FrameworkId,
    string ApiVersion) : IRequest<IActionResult>;

public record GetJourneyObligationDetailQuery(
    Guid ObligationId,
    string ApiVersion) : IRequest<IActionResult>;

public record GetJourneyReadinessQuery(
    Guid HospitalId,
    Guid FrameworkId,
    string ApiVersion) : IRequest<IActionResult>;
