using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Gateway.Application.Queries;

public record ListProfessionalProfilesQuery(string ApiVersion, int Page, int PageSize, Guid? CategoryValueId, Guid? SpecialtyValueId) : IRequest<IActionResult>;
public record SearchProfessionalProfilesQuery(string ApiVersion, string Query, int Page, int PageSize) : IRequest<IActionResult>;
public record GetProfessionalProfileByIdQuery(string ApiVersion, Guid ProfileId) : IRequest<IActionResult>;
public record GetProfessionalTimelineQuery(string ApiVersion, Guid ProfileId) : IRequest<IActionResult>;
public record GetProfessionalRenewalsQuery(string ApiVersion, int WithinDays, Guid? OrganizationId) : IRequest<IActionResult>;
public record GetProfessionalLookupsQuery(string ApiVersion, string Category) : IRequest<IActionResult>;
