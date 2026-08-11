using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Gateway.Application.Queries;

public record GetAllFrameworksQuery(string ApiVersion) : IRequest<IActionResult>;
