using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Gateway.Application.Commands;

public record SyncComplianceTasksCommand(Guid JourneyId, string ApiVersion) : IRequest<IActionResult>;
