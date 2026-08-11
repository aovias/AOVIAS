using OrganizationService.Application.DTOs;
using MediatR;

namespace OrganizationService.Application.Queries;

public record GetOrganizationBrandingQuery(Guid OrganizationId, Guid ActorUserId, bool CanEdit) : IRequest<OrganizationBrandingDto?>;
