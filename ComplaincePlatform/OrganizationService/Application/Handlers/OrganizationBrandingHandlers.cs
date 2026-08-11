using MediatR;
using OrganizationService.Application.Commands;
using OrganizationService.Application.DTOs;
using OrganizationService.Application.Queries;
using OrganizationService.Application.Services;

namespace OrganizationService.Application.Handlers;

public class GetOrganizationBrandingHandler(IOrganizationBrandingAppService svc)
    : IRequestHandler<GetOrganizationBrandingQuery, OrganizationBrandingDto?>
{
    public Task<OrganizationBrandingDto?> Handle(GetOrganizationBrandingQuery request, CancellationToken ct)
        => svc.GetAsync(request.OrganizationId, request.ActorUserId, request.CanEdit, ct);
}

public class UpdateOrganizationBrandingHandler(IOrganizationBrandingAppService svc)
    : IRequestHandler<UpdateOrganizationBrandingCommand, OrganizationBrandingDto>
{
    public Task<OrganizationBrandingDto> Handle(UpdateOrganizationBrandingCommand request, CancellationToken ct)
        => svc.UpdateConfigAsync(request.OrganizationId, request.Request, request.ActorUserId, ct);
}

public class UploadBrandingAssetHandler(IOrganizationBrandingAppService svc)
    : IRequestHandler<UploadBrandingAssetCommand, OrganizationBrandingDto>
{
    public Task<OrganizationBrandingDto> Handle(UploadBrandingAssetCommand request, CancellationToken ct)
        => svc.UploadAssetAsync(request.OrganizationId, request.AssetKey, request.FileStream, request.FileName, request.ContentType, request.Length, request.ActorUserId, ct);
}

public class RemoveBrandingAssetHandler(IOrganizationBrandingAppService svc)
    : IRequestHandler<RemoveBrandingAssetCommand, OrganizationBrandingDto>
{
    public Task<OrganizationBrandingDto> Handle(RemoveBrandingAssetCommand request, CancellationToken ct)
        => svc.RemoveAssetAsync(request.OrganizationId, request.AssetKey, request.ActorUserId, ct);
}
