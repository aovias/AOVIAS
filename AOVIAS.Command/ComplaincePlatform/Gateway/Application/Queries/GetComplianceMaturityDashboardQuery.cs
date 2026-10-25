using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Gateway.Application.Queries;

/// <summary>
/// Query to retrieve the Compliance Maturity Dashboard for the authenticated hospital.
/// Hospital context is resolved from the JWT hospital_id claim inside the handler.
/// </summary>
public record GetComplianceMaturityDashboardQuery(string ApiVersion) : IRequest<IActionResult>;
