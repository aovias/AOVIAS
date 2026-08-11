using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrganizationService.Application.Services;
using Shared.Infrastructure.Api;
using Shared.Infrastructure.Auth;

namespace OrganizationService.Controllers.Internal;

/// <summary>
/// Enterprise Configuration API (Stage 5B.1).
///
/// <para>
/// The authoritative source for configurable business metadata. One endpoint serves every
/// Category A domain — audit types, severities, risk categories and the rest differ only in their
/// data, so a per-domain endpoint would duplicate resolution logic and let the copies drift.
/// </para>
///
/// <para>
/// <c>organizationId</c> is supplied by the Gateway from validated token claims, never by a client
/// (Wave 2.2B, risk R-02).
/// </para>
/// </summary>
[ApiController]
[Route("internal/v1/configuration")]
[Authorize(Policy = InternalApiAuthorizationExtensions.InternalOnlyPolicy)]
public sealed class InternalConfigurationController : ControllerBase
{
    private readonly PlatformConfigurationAppService _configuration;

    public InternalConfigurationController(PlatformConfigurationAppService configuration)
        => _configuration = configuration;

    /// <summary>GET /internal/v1/configuration/categories</summary>
    [HttpGet("categories")]
    public async Task<IActionResult> GetCategories(CancellationToken ct)
    {
        var codes = await _configuration.GetCategoryCodesAsync(ct);
        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, codes, HttpContext.TraceIdentifier));
    }

    /// <summary>
    /// GET /internal/v1/configuration/categories/{code}?organizationId=…
    ///
    /// Returns platform defaults overlaid with the organization's overrides. A missing category
    /// yields 404 so the caller can fall back to its own seed — distinct from an empty catalog,
    /// which would mean "configured to have nothing".
    /// </summary>
    [HttpGet("categories/{code}")]
    public async Task<IActionResult> GetCategory(
        string code,
        [FromQuery] Guid organizationId,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return BadRequest(ApiResponseFactory.Failure<object>(
                ApiResponseCode.VALIDATION_FAILED, HttpContext.TraceIdentifier));
        }

        var category = await _configuration.GetCategoryAsync(code, organizationId, ct);
        if (category is null)
        {
            return NotFound(ApiResponseFactory.Failure<object>(
                ApiResponseCode.RESOURCE_NOT_FOUND, HttpContext.TraceIdentifier));
        }

        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, category, HttpContext.TraceIdentifier));
    }

    /// <summary>
    /// PUT /internal/v1/configuration/categories/{code}/values?organizationId=…
    ///
    /// Creates or updates the organization's override. Platform defaults are never modified.
    /// </summary>
    [HttpPut("categories/{code}/values")]
    public async Task<IActionResult> UpsertOverride(
        string code,
        [FromQuery] Guid organizationId,
        [FromBody] ConfigurationOverrideRequest request,
        CancellationToken ct)
    {
        if (organizationId == Guid.Empty || request is null || string.IsNullOrWhiteSpace(request.Code))
        {
            return BadRequest(ApiResponseFactory.Failure<object>(
                ApiResponseCode.VALIDATION_FAILED, HttpContext.TraceIdentifier));
        }

        var applied = await _configuration.UpsertOverrideAsync(code, organizationId, request, ct);
        if (!applied)
        {
            // Either the category does not exist or it is not tenant-configurable. Both mean the
            // caller may not write here.
            return NotFound(ApiResponseFactory.Failure<object>(
                ApiResponseCode.RESOURCE_NOT_FOUND, HttpContext.TraceIdentifier));
        }

        var refreshed = await _configuration.GetCategoryAsync(code, organizationId, ct);
        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, refreshed, HttpContext.TraceIdentifier));
    }

    /// <summary>
    /// DELETE /internal/v1/configuration/categories/{code}/values/{valueCode}?organizationId=…
    ///
    /// Restores the platform default by removing the organization's override.
    /// </summary>
    [HttpDelete("categories/{code}/values/{valueCode}")]
    public async Task<IActionResult> RemoveOverride(
        string code,
        string valueCode,
        [FromQuery] Guid organizationId,
        CancellationToken ct)
    {
        await _configuration.RemoveOverrideAsync(code, organizationId, valueCode, ct);

        // Returns the resolved category either way: "restore default" on a value that is already
        // the default is a legitimate request, not an error.
        var refreshed = await _configuration.GetCategoryAsync(code, organizationId, ct);
        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, refreshed, HttpContext.TraceIdentifier));
    }
}
