using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Gateway.Application.Queries;

public record GetAuditFindingsQuery(Guid JourneyId, string ApiVersion) : IRequest<IActionResult>;
