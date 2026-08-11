using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Gateway.Application.Queries;

public record GetOrganizationSettingsProxyQuery(string ApiVersion, Guid OrganizationId) : IRequest<IActionResult>;

public record GetOrganizationLogoProxyQuery(string ApiVersion, Guid OrganizationId) : IRequest<IActionResult>;

public record GetOrganizationBrandingProxyQuery(string ApiVersion, Guid OrganizationId) : IRequest<IActionResult>;

public record GetOrganizationBrandingAssetProxyQuery(string ApiVersion, Guid OrganizationId, string AssetKey) : IRequest<IActionResult>;
