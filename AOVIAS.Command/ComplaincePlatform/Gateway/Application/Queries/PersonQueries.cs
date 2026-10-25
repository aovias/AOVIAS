using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Gateway.Application.Queries;

public record ListPersonsQuery(string ApiVersion, int Page, int PageSize, string? Status) : IRequest<IActionResult>;
public record SearchPersonsQuery(string ApiVersion, string Query, int Page, int PageSize) : IRequest<IActionResult>;
public record GetPersonByIdQuery(string ApiVersion, Guid PersonId) : IRequest<IActionResult>;
public record GetPersonContactsQuery(string ApiVersion, Guid PersonId) : IRequest<IActionResult>;
public record GetPersonAddressesQuery(string ApiVersion, Guid PersonId) : IRequest<IActionResult>;
public record GetPersonTimelineQuery(string ApiVersion, Guid PersonId) : IRequest<IActionResult>;
public record GetPersonLookupsQuery(string ApiVersion, string Category) : IRequest<IActionResult>;
