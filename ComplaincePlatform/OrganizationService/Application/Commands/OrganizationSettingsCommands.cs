using MediatR;
using OrganizationService.Application.DTOs;

namespace OrganizationService.Application.Commands;

public record UpdateOrganizationSettingsCommand(Guid OrganizationId, UpdateOrganizationSettingsRequest Request, Guid ActorUserId) : IRequest<OrganizationSettingsDto>;
public record UploadOrganizationLogoCommand(Guid OrganizationId, Stream FileStream, string FileName, string ContentType, long Length, Guid ActorUserId) : IRequest<OrganizationSettingsDto>;
public record RemoveOrganizationLogoCommand(Guid OrganizationId, Guid ActorUserId) : IRequest<OrganizationSettingsDto>;
