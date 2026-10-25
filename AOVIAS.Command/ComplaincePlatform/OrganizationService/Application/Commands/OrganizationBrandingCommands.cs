using OrganizationService.Application.DTOs;
using MediatR;

namespace OrganizationService.Application.Commands;

public record UpdateOrganizationBrandingCommand(Guid OrganizationId, UpdateOrganizationBrandingRequest Request, Guid ActorUserId) : IRequest<OrganizationBrandingDto>;
public record UploadBrandingAssetCommand(Guid OrganizationId, string AssetKey, Stream FileStream, string FileName, string ContentType, long Length, Guid ActorUserId) : IRequest<OrganizationBrandingDto>;
public record RemoveBrandingAssetCommand(Guid OrganizationId, string AssetKey, Guid ActorUserId) : IRequest<OrganizationBrandingDto>;
