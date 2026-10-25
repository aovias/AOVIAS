using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OrganizationService.Application.Contracts;
using OrganizationService.Application.DTOs;
using OrganizationService.Domain.Entities;
using OrganizationService.Infrastructure.Persistence;

namespace OrganizationService.Application.Services;

public interface IOrganizationBrandingAppService
{
    Task<OrganizationBrandingDto?> GetAsync(Guid organizationId, Guid actorUserId, bool canEdit, CancellationToken ct = default);
    Task<OrganizationBrandingDto> UpdateConfigAsync(Guid organizationId, UpdateOrganizationBrandingRequest request, Guid actorUserId, CancellationToken ct = default);
    Task<OrganizationBrandingDto> UploadAssetAsync(Guid organizationId, string assetKey, Stream fileStream, string fileName, string contentType, long length, Guid actorUserId, CancellationToken ct = default);
    Task<OrganizationBrandingDto> RemoveAssetAsync(Guid organizationId, string assetKey, Guid actorUserId, CancellationToken ct = default);
    Task<(Stream Stream, string ContentType)?> OpenAssetAsync(Guid organizationId, string assetKey, CancellationToken ct = default);
}

public sealed class OrganizationBrandingAppService(
    OrganizationDbContext db,
    IFileStorageService fileStorage,
    IOrganizationTimelineService timeline,
    IOptions<OrganizationSettingsOptions> options) : IOrganizationBrandingAppService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public async Task<OrganizationBrandingDto?> GetAsync(Guid organizationId, Guid actorUserId, bool canEdit, CancellationToken ct = default)
    {
        _ = actorUserId;
        var exists = await db.Organizations.AsNoTracking().AnyAsync(o => o.Id == organizationId, ct);
        if (!exists) return null;

        var settings = await db.OrganizationSettings.AsNoTracking()
            .FirstOrDefaultAsync(s => s.OrganizationId == organizationId, ct);
        settings ??= OrganizationSettingsAppService.CreateDefaultSettingsEntity(organizationId);

        return MapBrandingDto(organizationId, settings, canEdit);
    }

    public async Task<OrganizationBrandingDto> UpdateConfigAsync(
        Guid organizationId,
        UpdateOrganizationBrandingRequest request,
        Guid actorUserId,
        CancellationToken ct = default)
    {
        var settings = await EnsureSettingsAsync(organizationId, ct);
        if (!string.IsNullOrWhiteSpace(request.RowVersion))
        {
            settings.RowVersion = Convert.FromBase64String(request.RowVersion);
            db.Entry(settings).Property(x => x.RowVersion).OriginalValue = settings.RowVersion;
        }

        var previous = DeserializeConfig(settings.BrandingConfigJson);
        var next = request.Config ?? new OrganizationBrandingConfigDto();
        settings.BrandingConfigJson = SerializeConfig(next);

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new InvalidOperationException("Branding was modified by another user. Please refresh and try again.");
        }

        await RecordBrandingAuditAsync(
            organizationId,
            actorUserId,
            "BrandingConfigUpdated",
            "Organization branding configuration updated",
            new OrganizationSettingsChangeDto
            {
                Field = "BrandingConfig",
                Previous = SerializeConfig(previous),
                New = settings.BrandingConfigJson,
            },
            ct);

        return MapBrandingDto(organizationId, settings, canEdit: true);
    }

    public async Task<OrganizationBrandingDto> UploadAssetAsync(
        Guid organizationId,
        string assetKey,
        Stream fileStream,
        string fileName,
        string contentType,
        long length,
        Guid actorUserId,
        CancellationToken ct = default)
    {
        ValidateAssetKey(assetKey);
        ValidateImage(fileName, contentType, length);

        var settings = await EnsureSettingsAsync(organizationId, ct);
        var assets = DeserializeAssets(settings.BrandingAssetsJson);
        var previousPath = assets.GetValueOrDefault(assetKey);

        var extension = Path.GetExtension(fileName);
        var storageFileName = $"{assetKey}{extension.ToLowerInvariant()}";
        var relativePath = Path.Combine("App_Data", "org_branding", organizationId.ToString("N"));

        if (!string.IsNullOrWhiteSpace(previousPath))
            await fileStorage.DeleteAsync(previousPath, ct);

        var storagePath = await fileStorage.SaveAsync(fileStream, storageFileName, contentType, relativePath, ct);
        assets[assetKey] = storagePath;
        settings.BrandingAssetsJson = SerializeAssets(assets);
        await db.SaveChangesAsync(ct);

        await RecordBrandingAuditAsync(
            organizationId,
            actorUserId,
            "BrandingAssetUpdated",
            $"Branding asset '{assetKey}' updated",
            new OrganizationSettingsChangeDto { Field = $"BrandingAsset:{assetKey}", Previous = previousPath, New = storagePath },
            ct);

        return MapBrandingDto(organizationId, settings, canEdit: true);
    }

    public async Task<OrganizationBrandingDto> RemoveAssetAsync(Guid organizationId, string assetKey, Guid actorUserId, CancellationToken ct = default)
    {
        ValidateAssetKey(assetKey);
        var settings = await EnsureSettingsAsync(organizationId, ct);
        var assets = DeserializeAssets(settings.BrandingAssetsJson);
        if (!assets.TryGetValue(assetKey, out var previousPath) || string.IsNullOrWhiteSpace(previousPath))
            return MapBrandingDto(organizationId, settings, canEdit: true);

        await fileStorage.DeleteAsync(previousPath, ct);
        assets.Remove(assetKey);
        settings.BrandingAssetsJson = SerializeAssets(assets);
        await db.SaveChangesAsync(ct);

        await RecordBrandingAuditAsync(
            organizationId,
            actorUserId,
            "BrandingAssetRemoved",
            $"Branding asset '{assetKey}' removed",
            new OrganizationSettingsChangeDto { Field = $"BrandingAsset:{assetKey}", Previous = previousPath, New = null },
            ct);

        return MapBrandingDto(organizationId, settings, canEdit: true);
    }

    public async Task<(Stream Stream, string ContentType)?> OpenAssetAsync(Guid organizationId, string assetKey, CancellationToken ct = default)
    {
        ValidateAssetKey(assetKey);
        var settings = await db.OrganizationSettings.AsNoTracking()
            .FirstOrDefaultAsync(s => s.OrganizationId == organizationId, ct);
        if (settings == null) return null;

        var assets = DeserializeAssets(settings.BrandingAssetsJson);
        if (!assets.TryGetValue(assetKey, out var path) || string.IsNullOrWhiteSpace(path))
            return null;

        var stream = await fileStorage.OpenReadAsync(path, ct);
        return (stream, ResolveContentType(path));
    }

    private async Task<OrganizationSettings> EnsureSettingsAsync(Guid organizationId, CancellationToken ct)
    {
        var settings = await db.OrganizationSettings.FirstOrDefaultAsync(s => s.OrganizationId == organizationId, ct);
        if (settings != null) return settings;

        settings = OrganizationSettingsAppService.CreateDefaultSettingsEntity(organizationId);
        db.OrganizationSettings.Add(settings);
        await db.SaveChangesAsync(ct);
        return settings;
    }

    private async Task RecordBrandingAuditAsync(
        Guid organizationId,
        Guid actorUserId,
        string eventType,
        string summary,
        OrganizationSettingsChangeDto change,
        CancellationToken ct)
    {
        await timeline.AppendAsync(eventType, summary, organizationId, "OrganizationBranding", organizationId, actorUserId, new { changes = new[] { change } }, ct);
        db.OrganizationAuditLogs.Add(new OrganizationAuditLog
        {
            Id = Guid.NewGuid(),
            EntityType = "OrganizationBranding",
            EntityId = organizationId,
            Action = eventType,
            ActorUserId = actorUserId,
            OccurredAt = DateTime.UtcNow,
        });
        await db.SaveChangesAsync(ct);
    }

    private OrganizationBrandingDto MapBrandingDto(Guid organizationId, OrganizationSettings settings, bool canEdit)
    {
        var assets = DeserializeAssets(settings.BrandingAssetsJson);
        string? AssetUrl(string key) => assets.ContainsKey(key)
            ? $"/api/v1/organizations/{organizationId}/settings/branding/assets/{key}"
            : null;

        return new OrganizationBrandingDto
        {
            OrganizationId = organizationId,
            CanEdit = canEdit,
            RowVersion = settings.RowVersion.Length == 0 ? null : Convert.ToBase64String(settings.RowVersion),
            Config = DeserializeConfig(settings.BrandingConfigJson),
            Assets = new BrandingAssetUrlsDto
            {
                PrimaryLogoUrl = string.IsNullOrWhiteSpace(settings.LogoStoragePath)
                    ? null
                    : $"/api/v1/organizations/{organizationId}/settings/logo",
                SecondaryLogoUrl = AssetUrl("secondary"),
                FaviconUrl = AssetUrl("favicon"),
                ReportLogoUrl = AssetUrl("report"),
                EmailHeaderLogoUrl = AssetUrl("emailHeader"),
                EmailFooterLogoUrl = AssetUrl("emailFooter"),
                LoginBackgroundUrl = AssetUrl("loginBackground"),
            },
        };
    }

    private void ValidateAssetKey(string assetKey)
    {
        if (!OrganizationSettingsDefaults.BrandingAssetKeys.Contains(assetKey, StringComparer.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Unsupported branding asset key: {assetKey}");
    }

    private void ValidateImage(string fileName, string contentType, long length)
    {
        if (length <= 0) throw new InvalidOperationException("Image file is empty.");
        if (length > options.Value.LogoMaxBytes)
            throw new InvalidOperationException($"Image exceeds maximum size of {options.Value.LogoMaxBytes} bytes.");

        var extension = Path.GetExtension(fileName);
        if (!OrganizationSettingsAppService.AllowedLogoExtensions.Contains(extension))
            throw new InvalidOperationException("Image must be PNG, JPG, or SVG.");

        if (!string.IsNullOrWhiteSpace(contentType) && !OrganizationSettingsAppService.AllowedLogoContentTypes.Contains(contentType))
            throw new InvalidOperationException("Image content type is not supported.");
    }

    internal static Dictionary<string, string> DeserializeAssets(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, string>>(json, JsonOptions)
                ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
        catch
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
    }

    internal static string SerializeAssets(Dictionary<string, string> assets) =>
        JsonSerializer.Serialize(assets, JsonOptions);

    internal static OrganizationBrandingConfigDto DeserializeConfig(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new OrganizationBrandingConfigDto();
        try
        {
            return JsonSerializer.Deserialize<OrganizationBrandingConfigDto>(json, JsonOptions) ?? new OrganizationBrandingConfigDto();
        }
        catch
        {
            return new OrganizationBrandingConfigDto();
        }
    }

    internal static string SerializeConfig(OrganizationBrandingConfigDto config) =>
        JsonSerializer.Serialize(config, JsonOptions);

    private static string ResolveContentType(string path)
    {
        return Path.GetExtension(path).ToLowerInvariant() switch
        {
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".svg" => "image/svg+xml",
            _ => "application/octet-stream",
        };
    }
}
