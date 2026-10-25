using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Gateway.Application.Commands;

public record UpdateOrganizationSettingsProxyCommand(string ApiVersion, Guid OrganizationId, object RequestBody) : IRequest<IActionResult>;

public record UploadOrganizationLogoProxyCommand(string ApiVersion, Guid OrganizationId, IFormFile File) : IRequest<IActionResult>;

public record RemoveOrganizationLogoProxyCommand(string ApiVersion, Guid OrganizationId) : IRequest<IActionResult>;

public record UpdateOrganizationBrandingProxyCommand(string ApiVersion, Guid OrganizationId, object RequestBody) : IRequest<IActionResult>;

public record UploadBrandingAssetProxyCommand(string ApiVersion, Guid OrganizationId, string AssetKey, IFormFile File) : IRequest<IActionResult>;

public record RemoveBrandingAssetProxyCommand(string ApiVersion, Guid OrganizationId, string AssetKey) : IRequest<IActionResult>;
