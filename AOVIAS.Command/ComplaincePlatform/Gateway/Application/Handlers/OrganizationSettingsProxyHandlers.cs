using Gateway.Application.Commands;
using Gateway.Application.Queries;
using Gateway.Services;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Gateway.Application.Handlers;

public class OrganizationSettingsProxyHandlers
{
    public class GetOrganizationSettingsHandler(IProxyService proxy) : IRequestHandler<GetOrganizationSettingsProxyQuery, IActionResult>
    {
        public Task<IActionResult> Handle(GetOrganizationSettingsProxyQuery request, CancellationToken ct)
            => proxy.ProxyToOrganizationServiceAsync(
                $"/api/v{request.ApiVersion}/organizations/{request.OrganizationId}/settings",
                HttpMethod.Get,
                cancellationToken: ct);
    }

    public class UpdateOrganizationSettingsHandler(IProxyService proxy) : IRequestHandler<UpdateOrganizationSettingsProxyCommand, IActionResult>
    {
        public Task<IActionResult> Handle(UpdateOrganizationSettingsProxyCommand request, CancellationToken ct)
            => proxy.ProxyToOrganizationServiceAsync(
                $"/api/v{request.ApiVersion}/organizations/{request.OrganizationId}/settings",
                HttpMethod.Put,
                request.RequestBody,
                cancellationToken: ct);
    }

    public class UploadOrganizationLogoHandler(IProxyService proxy) : IRequestHandler<UploadOrganizationLogoProxyCommand, IActionResult>
    {
        public async Task<IActionResult> Handle(UploadOrganizationLogoProxyCommand request, CancellationToken ct)
        {
            await using var stream = request.File.OpenReadStream();
            using var content = new MultipartFormDataContent();
            var fileContent = new StreamContent(stream);
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(request.File.ContentType);
            content.Add(fileContent, "file", request.File.FileName);

            return await proxy.ProxyToOrganizationServiceRawAsync(
                $"/api/v{request.ApiVersion}/organizations/{request.OrganizationId}/settings/logo",
                HttpMethod.Post,
                content,
                cancellationToken: ct);
        }
    }

    public class RemoveOrganizationLogoHandler(IProxyService proxy) : IRequestHandler<RemoveOrganizationLogoProxyCommand, IActionResult>
    {
        public Task<IActionResult> Handle(RemoveOrganizationLogoProxyCommand request, CancellationToken ct)
            => proxy.ProxyToOrganizationServiceAsync(
                $"/api/v{request.ApiVersion}/organizations/{request.OrganizationId}/settings/logo",
                HttpMethod.Delete,
                cancellationToken: ct);
    }

    public class GetOrganizationLogoHandler(IProxyService proxy) : IRequestHandler<GetOrganizationLogoProxyQuery, IActionResult>
    {
        public Task<IActionResult> Handle(GetOrganizationLogoProxyQuery request, CancellationToken ct)
            => proxy.ProxyOrganizationServiceStreamGetAsync(
                $"/api/v{request.ApiVersion}/organizations/{request.OrganizationId}/settings/logo",
                cancellationToken: ct);
    }

    public class GetOrganizationBrandingHandler(IProxyService proxy) : IRequestHandler<GetOrganizationBrandingProxyQuery, IActionResult>
    {
        public Task<IActionResult> Handle(GetOrganizationBrandingProxyQuery request, CancellationToken ct)
            => proxy.ProxyToOrganizationServiceAsync(
                $"/api/v{request.ApiVersion}/organizations/{request.OrganizationId}/settings/branding",
                HttpMethod.Get,
                cancellationToken: ct);
    }

    public class UpdateOrganizationBrandingHandler(IProxyService proxy) : IRequestHandler<UpdateOrganizationBrandingProxyCommand, IActionResult>
    {
        public Task<IActionResult> Handle(UpdateOrganizationBrandingProxyCommand request, CancellationToken ct)
            => proxy.ProxyToOrganizationServiceAsync(
                $"/api/v{request.ApiVersion}/organizations/{request.OrganizationId}/settings/branding",
                HttpMethod.Put,
                request.RequestBody,
                cancellationToken: ct);
    }

    public class UploadBrandingAssetHandler(IProxyService proxy) : IRequestHandler<UploadBrandingAssetProxyCommand, IActionResult>
    {
        public async Task<IActionResult> Handle(UploadBrandingAssetProxyCommand request, CancellationToken ct)
        {
            await using var stream = request.File.OpenReadStream();
            using var content = new MultipartFormDataContent();
            var fileContent = new StreamContent(stream);
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(request.File.ContentType);
            content.Add(fileContent, "file", request.File.FileName);

            return await proxy.ProxyToOrganizationServiceRawAsync(
                $"/api/v{request.ApiVersion}/organizations/{request.OrganizationId}/settings/branding/assets/{Uri.EscapeDataString(request.AssetKey)}",
                HttpMethod.Post,
                content,
                cancellationToken: ct);
        }
    }

    public class RemoveBrandingAssetHandler(IProxyService proxy) : IRequestHandler<RemoveBrandingAssetProxyCommand, IActionResult>
    {
        public Task<IActionResult> Handle(RemoveBrandingAssetProxyCommand request, CancellationToken ct)
            => proxy.ProxyToOrganizationServiceAsync(
                $"/api/v{request.ApiVersion}/organizations/{request.OrganizationId}/settings/branding/assets/{Uri.EscapeDataString(request.AssetKey)}",
                HttpMethod.Delete,
                cancellationToken: ct);
    }

    public class GetOrganizationBrandingAssetHandler(IProxyService proxy) : IRequestHandler<GetOrganizationBrandingAssetProxyQuery, IActionResult>
    {
        public Task<IActionResult> Handle(GetOrganizationBrandingAssetProxyQuery request, CancellationToken ct)
            => proxy.ProxyOrganizationServiceStreamGetAsync(
                $"/api/v{request.ApiVersion}/organizations/{request.OrganizationId}/settings/branding/assets/{Uri.EscapeDataString(request.AssetKey)}",
                cancellationToken: ct);
    }
}
