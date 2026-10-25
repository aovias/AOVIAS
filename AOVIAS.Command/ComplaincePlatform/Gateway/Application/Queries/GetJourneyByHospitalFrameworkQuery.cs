using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Gateway.Application.Queries;

/// <summary>
/// Loads a journey by hospital + framework (ComplianceService), with hospital_id JWT validation.
/// </summary>
public record GetJourneyByHospitalFrameworkQuery(Guid HospitalId, Guid FrameworkId, string ApiVersion) : IRequest<IActionResult>;
