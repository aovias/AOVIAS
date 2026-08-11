using MediatR;
using OrganizationService.Application.DTOs;

namespace OrganizationService.Application.Queries;

public record GetOrganizationSettingsQuery(Guid OrganizationId, Guid ActorUserId, bool CanEdit) : IRequest<OrganizationSettingsDto?>;
public record ListOrganizationAuditLogsQuery(Guid OrganizationId, DateTime? From, DateTime? To, int Take) : IRequest<IReadOnlyList<OrganizationAuditLogDto>>;
