using Microsoft.EntityFrameworkCore;
using OrganizationService.Infrastructure.Persistence;

namespace OrganizationService.Application.Services;

/// <summary>A configurable value within a configuration category.</summary>
public sealed class ConfigurationValueDto
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;

    /// <summary>True when this row is the tenant's own override rather than the platform default.</summary>
    public bool IsTenantOverride { get; set; }

    /// <summary>Domain-specific attributes as raw JSON. Typed by the consumer, not here.</summary>
    public string? Metadata { get; set; }

    public string? ParentCode { get; set; }
}

/// <summary>
/// An administrator's edit to a single configuration value.
///
/// Every field except <see cref="Code"/> is optional: an omitted field keeps its current value
/// (or the platform default when creating), so a caller renaming a value need not resend its
/// ordering and metadata.
/// </summary>
public sealed class ConfigurationOverrideRequest
{
    public string Code { get; set; } = string.Empty;
    public string? Name { get; set; }
    public int? SortOrder { get; set; }
    public bool? IsActive { get; set; }
    public DateTime? EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public string? Metadata { get; set; }
}

/// <summary>A configuration category and its resolved values for one organization.</summary>
public sealed class ConfigurationCategoryDto
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsTenantConfigurable { get; set; }

    /// <summary>
    /// Changes whenever any contributing row changes. Consumers cache against this rather than
    /// polling — it is what makes the client-side cache safe to hold indefinitely.
    /// </summary>
    public string Version { get; set; } = string.Empty;

    public IReadOnlyList<ConfigurationValueDto> Values { get; set; } = Array.Empty<ConfigurationValueDto>();
}

/// <summary>
/// Resolves platform configuration for an organization.
///
/// <para>
/// **Resolution rule.** A category's values are the platform defaults (<c>OrganizationId IS NULL</c>)
/// overlaid with the organization's own rows, matched on <c>Code</c>. A tenant row wins; where none
/// exists the default shows through. That means an organization that has configured nothing still
/// receives a complete, sensible catalog — no seeding step, and no empty pickers.
/// </para>
///
/// <para>
/// **Why one service for every Category A domain.** Audit types, severities, risk categories and
/// the rest differ only in their data. Giving each its own service would duplicate this resolution
/// and each copy would drift. Domain-specific attributes ride in <see cref="ConfigurationValueDto.Metadata"/>
/// so a new domain needs no schema change and no new code here.
/// </para>
/// </summary>
public sealed class PlatformConfigurationAppService
{
    private readonly OrganizationDbContext _db;

    public PlatformConfigurationAppService(OrganizationDbContext db) => _db = db;

    /// <summary>
    /// Resolve one category for an organization. Returns <c>null</c> when the category does not
    /// exist — callers should treat that as "not configured" and fall back to their own seed rather
    /// than presenting an empty list.
    /// </summary>
    public async Task<ConfigurationCategoryDto?> GetCategoryAsync(
        string categoryCode,
        Guid organizationId,
        CancellationToken ct = default)
    {
        var category = await _db.LookupCategories
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Code == categoryCode, ct);

        if (category is null) return null;

        var now = DateTime.UtcNow;

        // Platform defaults and this tenant's overrides in one round trip; the overlay happens in
        // memory because the row counts here are small (tens, not thousands) and the alternative is
        // a correlated subquery that indexes poorly.
        var rows = await _db.LookupValues
            .AsNoTracking()
            .Where(v => v.CategoryId == category.Id
                        && (v.OrganizationId == null || v.OrganizationId == organizationId)
                        && v.IsActive
                        && (v.EffectiveFrom == null || v.EffectiveFrom <= now)
                        && (v.EffectiveTo == null || v.EffectiveTo >= now))
            .ToListAsync(ct);

        var resolved = rows
            .GroupBy(v => v.Code, StringComparer.OrdinalIgnoreCase)
            // Tenant override first, platform default second — take whichever is present.
            .Select(g => g.OrderByDescending(v => v.OrganizationId.HasValue).First())
            .OrderBy(v => v.SortOrder)
            .ThenBy(v => v.Name, StringComparer.OrdinalIgnoreCase)
            .Select(v => new ConfigurationValueDto
            {
                Code = v.Code,
                Name = v.Name,
                SortOrder = v.SortOrder,
                IsActive = v.IsActive,
                IsTenantOverride = v.OrganizationId.HasValue,
                Metadata = v.Metadata,
            })
            .ToList();

        return new ConfigurationCategoryDto
        {
            Code = category.Code,
            Name = category.Name,
            Description = category.Description,
            IsTenantConfigurable = category.IsTenantConfigurable,
            Version = ComputeVersion(resolved),
            Values = resolved,
        };
    }

    /// <summary>
    /// Create or update this organization's override of a value.
    ///
    /// <para>
    /// Writes only ever touch tenant rows. A platform default is never modified from here, so one
    /// organization's configuration cannot alter what another sees — and "restore default" stays
    /// possible because the original row is still there.
    /// </para>
    /// </summary>
    public async Task<bool> UpsertOverrideAsync(
        string categoryCode,
        Guid organizationId,
        ConfigurationOverrideRequest request,
        CancellationToken ct = default)
    {
        if (organizationId == Guid.Empty || string.IsNullOrWhiteSpace(request.Code)) return false;

        var category = await _db.LookupCategories
            .FirstOrDefaultAsync(c => c.Code == categoryCode, ct);

        if (category is null || !category.IsTenantConfigurable) return false;

        var existing = await _db.LookupValues.FirstOrDefaultAsync(
            v => v.CategoryId == category.Id
                 && v.OrganizationId == organizationId
                 && v.Code == request.Code, ct);

        // Fall back to the platform default for any field the caller left unset, so a partial edit
        // (renaming a value, say) does not silently blank its ordering or metadata.
        var platformDefault = await _db.LookupValues.AsNoTracking().FirstOrDefaultAsync(
            v => v.CategoryId == category.Id && v.OrganizationId == null && v.Code == request.Code, ct);

        if (existing is null)
        {
            _db.LookupValues.Add(new Domain.Entities.OrganizationLookupValue
            {
                Id = Guid.NewGuid(),
                CategoryId = category.Id,
                OrganizationId = organizationId,
                Code = request.Code,
                Name = request.Name ?? platformDefault?.Name ?? request.Code,
                SortOrder = request.SortOrder ?? platformDefault?.SortOrder ?? 0,
                IsActive = request.IsActive ?? true,
                EffectiveFrom = request.EffectiveFrom,
                EffectiveTo = request.EffectiveTo,
                Metadata = request.Metadata ?? platformDefault?.Metadata,
            });
        }
        else
        {
            existing.Name = request.Name ?? existing.Name;
            existing.SortOrder = request.SortOrder ?? existing.SortOrder;
            existing.IsActive = request.IsActive ?? existing.IsActive;
            existing.EffectiveFrom = request.EffectiveFrom;
            existing.EffectiveTo = request.EffectiveTo;
            existing.Metadata = request.Metadata ?? existing.Metadata;
        }

        await _db.SaveChangesAsync(ct);
        return true;
    }

    /// <summary>
    /// Remove this organization's override so the platform default applies again.
    ///
    /// Returns <c>false</c> when there was no override — an idempotent no-op rather than an error,
    /// since "restore default" on an already-default value is a reasonable thing to ask for.
    /// </summary>
    public async Task<bool> RemoveOverrideAsync(
        string categoryCode,
        Guid organizationId,
        string valueCode,
        CancellationToken ct = default)
    {
        var category = await _db.LookupCategories.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Code == categoryCode, ct);

        if (category is null) return false;

        var existing = await _db.LookupValues.FirstOrDefaultAsync(
            v => v.CategoryId == category.Id
                 && v.OrganizationId == organizationId
                 && v.Code == valueCode, ct);

        if (existing is null) return false;

        _db.LookupValues.Remove(existing);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    /// <summary>Every category code the platform knows about, for administration screens.</summary>
    public async Task<IReadOnlyList<string>> GetCategoryCodesAsync(CancellationToken ct = default) =>
        await _db.LookupCategories
            .AsNoTracking()
            .OrderBy(c => c.Code)
            .Select(c => c.Code)
            .ToListAsync(ct);

    /// <summary>
    /// Stable hash of the resolved set. Deliberately derived from the *resolved* values rather than
    /// row versions, so a tenant override that produces an identical result does not invalidate a
    /// client cache unnecessarily.
    /// </summary>
    private static string ComputeVersion(IEnumerable<ConfigurationValueDto> values)
    {
        var material = string.Join('|', values.Select(v =>
            $"{v.Code}:{v.Name}:{v.SortOrder}:{v.IsActive}:{v.Metadata}"));

        var hash = System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(material));

        return Convert.ToHexString(hash)[..16];
    }
}
