using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Gateway.Application.Queries;

public record ListOeOwnershipQuery(Guid HospitalId, Guid? FrameworkId, string ApiVersion) : IRequest<IActionResult>;

public record UpsertOeOwnershipCommand(Guid HospitalId, object Body, string ApiVersion) : IRequest<IActionResult>;

public record ClearOeOwnershipCommand(Guid HospitalId, Guid ObjectiveElementId, string ApiVersion)
    : IRequest<IActionResult>;
