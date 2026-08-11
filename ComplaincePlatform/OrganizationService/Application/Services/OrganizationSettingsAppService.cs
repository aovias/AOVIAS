using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OrganizationService.Application.Contracts;
using OrganizationService.Application.DTOs;
using OrganizationService.Domain.Entities;
using OrganizationService.Infrastructure.Persistence;
using Shared.Infrastructure.Domain;

namespace OrganizationService.Application.Services;

public sealed class OrganizationSettingsOptions
{
    public long LogoMaxBytes { get; set; } = 2 * 1024 * 1024;
}

public sealed class OrganizationSettingsAppService(
    OrganizationDbContext db,
    IFileStorageService fileStorage,
    IOrganizationTimelineService timeline,
    IOptions<OrganizationSettingsOptions> options) : IOrganizationSettingsAppService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    internal static readonly HashSet<string> AllowedLogoExtensions = new(StringComparer.OrdinalIgnoreCase) { ".png", ".jpg", ".jpeg", ".svg" };
    internal static readonly HashSet<string> AllowedLogoContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/png", "image/jpeg", "image/jpg", "image/svg+xml"
    };

    public async Task<OrganizationSettingsDto?> GetAsync(Guid organizationId, Guid actorUserId, bool canEdit, CancellationToken ct = default)
    {
        _ = actorUserId;
        var org = await db.Organizations.AsNoTracking().FirstOrDefaultAsync(o => o.Id == organizationId, ct);
        if (org == null) return null;

        var settings = await db.OrganizationSettings.AsNoTracking()
            .FirstOrDefaultAsync(s => s.OrganizationId == organizationId, ct);
        settings ??= CreateDefaultSettings(organizationId);

        return MapToDto(org, settings, canEdit);
    }

    public async Task<OrganizationSettingsDto> UpdateAsync(
        Guid organizationId,
        UpdateOrganizationSettingsRequest request,
        Guid actorUserId,
        CancellationToken ct = default)
    {
        var org = await db.Organizations.FirstOrDefaultAsync(o => o.Id == organizationId, ct)
            ?? throw new KeyNotFoundException("Organization not found.");

        var settings = await db.OrganizationSettings.FirstOrDefaultAsync(s => s.OrganizationId == organizationId, ct);
        if (settings == null)
        {
            settings = CreateDefaultSettings(organizationId);
            db.OrganizationSettings.Add(settings);
        }

        if (!string.IsNullOrWhiteSpace(request.RowVersion))
        {
            settings.RowVersion = Convert.FromBase64String(request.RowVersion);
            db.Entry(settings).Property(x => x.RowVersion).OriginalValue = settings.RowVersion;
        }

        var normalizedCode = OrganizationCodeHelper.Normalize(request.OrganizationCode);
        if (await db.Organizations.AnyAsync(o => o.Id != organizationId && o.OrganizationCode == normalizedCode, ct))
        {
            throw new InvalidOperationException("Organization code is already in use.");
        }

        var changes = new List<OrganizationSettingsChangeDto>();

        void Track(string field, string? previous, string? current)
        {
            if (string.Equals(previous, current, StringComparison.Ordinal))
                return;
            changes.Add(new OrganizationSettingsChangeDto { Field = field, Previous = previous, New = current });
        }

        Track("OrganizationName", org.Name, request.OrganizationName.Trim());
        Track("OrganizationCode", org.OrganizationCode, normalizedCode);
        Track("TimeZoneId", settings.TimeZoneId, request.TimeZoneId.Trim());
        Track("LanguageCode", settings.LanguageCode, request.LanguageCode.Trim());

        var previousDays = SerializeWorkingDays(ParseWorkingDays(settings.WorkingDaysJson));
        var newDays = SerializeWorkingDays(request.WorkingDays);
        Track("WorkingDays", previousDays, newDays);

        var previousHours = FormatHours(settings.WorkingHoursStart, settings.WorkingHoursEnd);
        var start = ParseTime(request.WorkingHours.Start);
        var end = ParseTime(request.WorkingHours.End);
        if (start >= end)
            throw new InvalidOperationException("Working hours end time must be after start time.");

        var newHours = FormatHours(start, end);
        Track("WorkingHours", previousHours, newHours);

        org.Name = request.OrganizationName.Trim();
        org.OrganizationCode = normalizedCode;
        settings.TimeZoneId = request.TimeZoneId.Trim();
        settings.LanguageCode = request.LanguageCode.Trim();
        settings.WorkingDaysJson = newDays;
        settings.WorkingHoursStart = start;
        settings.WorkingHoursEnd = end;

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new InvalidOperationException("Settings were modified by another user. Please refresh and try again.");
        }

        if (changes.Count > 0)
        {
            await timeline.AppendAsync(
                "OrganizationSettings.Updated",
                "Organization settings updated",
                organizationId,
                "OrganizationSettings",
                organizationId,
                actorUserId,
                new
                {
                    changes,
                    changedBy = actorUserId,
                    changedOn = DateTime.UtcNow
                },
                ct);

            db.OrganizationAuditLogs.Add(new OrganizationAuditLog
            {
                Id = Guid.NewGuid(),
                EntityType = "OrganizationSettings",
                EntityId = organizationId,
                Action = "Updated",
                ActorUserId = actorUserId,
                OccurredAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync(ct);
        }

        return MapToDto(org, settings, canEdit: true);
    }

    public async Task<OrganizationSettingsDto> UploadLogoAsync(
        Guid organizationId,
        Stream fileStream,
        string fileName,
        string contentType,
        long length,
        Guid actorUserId,
        CancellationToken ct = default)
    {
        ValidateLogo(fileName, contentType, length);

        var org = await db.Organizations.FirstOrDefaultAsync(o => o.Id == organizationId, ct)
            ?? throw new KeyNotFoundException("Organization not found.");

        var settings = await EnsureSettingsAsync(organizationId, ct);
        var previousPath = settings.LogoStoragePath;

        var extension = Path.GetExtension(fileName);
        var storageFileName = $"logo{extension.ToLowerInvariant()}";
        var relativePath = Path.Combine("App_Data", "org_logos", organizationId.ToString("N"));

        if (!string.IsNullOrWhiteSpace(previousPath))
        {
            await fileStorage.DeleteAsync(previousPath, ct);
        }

        var storagePath = await fileStorage.SaveAsync(fileStream, storageFileName, contentType, relativePath, ct);
        settings.LogoStoragePath = storagePath;
        await db.SaveChangesAsync(ct);

        await timeline.AppendAsync(
            "OrganizationSettings.LogoUpdated",
            "Organization logo updated",
            organizationId,
            "OrganizationSettings",
            organizationId,
            actorUserId,
            new
            {
                changes = new[]
                {
                    new OrganizationSettingsChangeDto
                    {
                        Field = "Logo",
                        Previous = previousPath,
                        New = storagePath
                    }
                },
                changedBy = actorUserId,
                changedOn = DateTime.UtcNow
            },
            ct);

        db.OrganizationAuditLogs.Add(new OrganizationAuditLog
        {
            Id = Guid.NewGuid(),
            EntityType = "OrganizationSettings",
            EntityId = organizationId,
            Action = "LogoUpdated",
            ActorUserId = actorUserId,
            OccurredAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync(ct);

        return MapToDto(org, settings, canEdit: true);
    }

    public async Task<OrganizationSettingsDto> RemoveLogoAsync(Guid organizationId, Guid actorUserId, CancellationToken ct = default)
    {
        var org = await db.Organizations.FirstOrDefaultAsync(o => o.Id == organizationId, ct)
            ?? throw new KeyNotFoundException("Organization not found.");

        var settings = await EnsureSettingsAsync(organizationId, ct);
        var previousPath = settings.LogoStoragePath;
        if (string.IsNullOrWhiteSpace(previousPath))
            return MapToDto(org, settings, canEdit: true);

        await fileStorage.DeleteAsync(previousPath, ct);
        settings.LogoStoragePath = null;
        await db.SaveChangesAsync(ct);

        await timeline.AppendAsync(
            "OrganizationSettings.LogoRemoved",
            "Organization logo removed",
            organizationId,
            "OrganizationSettings",
            organizationId,
            actorUserId,
            new
            {
                changes = new[]
                {
                    new OrganizationSettingsChangeDto { Field = "Logo", Previous = previousPath, New = null }
                },
                changedBy = actorUserId,
                changedOn = DateTime.UtcNow
            },
            ct);

        return MapToDto(org, settings, canEdit: true);
    }

    public async Task<(Stream Stream, string ContentType)?> OpenLogoAsync(Guid organizationId, CancellationToken ct = default)
    {
        var settings = await db.OrganizationSettings.AsNoTracking()
            .FirstOrDefaultAsync(s => s.OrganizationId == organizationId, ct);
        if (settings?.LogoStoragePath == null) return null;

        var stream = await fileStorage.OpenReadAsync(settings.LogoStoragePath, ct);
        var extension = Path.GetExtension(settings.LogoStoragePath).ToLowerInvariant();
        var contentType = extension switch
        {
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".svg" => "image/svg+xml",
            _ => "application/octet-stream"
        };
        return (stream, contentType);
    }

    private async Task<OrganizationSettings> EnsureSettingsAsync(Guid organizationId, CancellationToken ct)
    {
        var settings = await db.OrganizationSettings.FirstOrDefaultAsync(s => s.OrganizationId == organizationId, ct);
        if (settings != null) return settings;

        settings = CreateDefaultSettings(organizationId);
        db.OrganizationSettings.Add(settings);
        await db.SaveChangesAsync(ct);
        return settings;
    }

    private void ValidateLogo(string fileName, string contentType, long length)
    {
        if (length <= 0)
            throw new InvalidOperationException("Logo file is empty.");
        if (length > options.Value.LogoMaxBytes)
            throw new InvalidOperationException($"Logo file exceeds maximum size of {options.Value.LogoMaxBytes} bytes.");

        var extension = Path.GetExtension(fileName);
        if (!AllowedLogoExtensions.Contains(extension))
            throw new InvalidOperationException("Logo must be PNG, JPG, or SVG.");

        if (!string.IsNullOrWhiteSpace(contentType) && !AllowedLogoContentTypes.Contains(contentType))
            throw new InvalidOperationException("Logo content type is not supported.");
    }

    public async Task<IReadOnlyList<OrgHolidayDto>> ListHolidaysAsync(Guid organizationId, int? year = null, CancellationToken ct = default)
    {
        var query = db.OrgHolidays.AsNoTracking().Where(h => h.OrganizationId == organizationId);
        if (year.HasValue)
        {
            var start = new DateOnly(year.Value, 1, 1);
            var end = new DateOnly(year.Value, 12, 31);
            query = query.Where(h => h.Date >= start && h.Date <= end);
        }

        return await query
            .OrderBy(h => h.Date)
            .Select(h => new OrgHolidayDto
            {
                Id = h.Id,
                OrganizationId = h.OrganizationId,
                Date = h.Date,
                Name = h.Name
            })
            .ToListAsync(ct);
    }

    public async Task<OrgHolidayDto> AddHolidayAsync(
        Guid organizationId,
        CreateOrgHolidayRequest request,
        Guid actorUserId,
        CancellationToken ct = default)
    {
        _ = actorUserId;
        if (!await db.Organizations.AnyAsync(o => o.Id == organizationId, ct))
            throw new KeyNotFoundException("Organization not found.");

        var name = (request.Name ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(name))
            throw new InvalidOperationException("Holiday name is required.");
        if (name.Length > 200)
            throw new InvalidOperationException("Holiday name must be 200 characters or fewer.");

        if (await db.OrgHolidays.AnyAsync(h => h.OrganizationId == organizationId && h.Date == request.Date, ct))
            throw new InvalidOperationException("A holiday already exists for this date.");

        var entity = new OrgHoliday
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            Date = request.Date,
            Name = name,
            CreatedAt = DateTime.UtcNow
        };
        db.OrgHolidays.Add(entity);
        await db.SaveChangesAsync(ct);

        return new OrgHolidayDto
        {
            Id = entity.Id,
            OrganizationId = entity.OrganizationId,
            Date = entity.Date,
            Name = entity.Name
        };
    }

    public async Task<bool> RemoveHolidayAsync(Guid organizationId, Guid holidayId, CancellationToken ct = default)
    {
        var entity = await db.OrgHolidays.FirstOrDefaultAsync(h => h.Id == holidayId && h.OrganizationId == organizationId, ct);
        if (entity == null) return false;
        db.OrgHolidays.Remove(entity);
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<BusinessCalendarDto?> GetBusinessCalendarAsync(Guid organizationId, int? year = null, CancellationToken ct = default)
    {
        var settings = await db.OrganizationSettings.AsNoTracking()
            .FirstOrDefaultAsync(s => s.OrganizationId == organizationId, ct);
        if (settings == null && !await db.Organizations.AnyAsync(o => o.Id == organizationId, ct))
            return null;

        settings ??= CreateDefaultSettings(organizationId);
        var holidays = await ListHolidaysAsync(organizationId, year, ct);

        return new BusinessCalendarDto
        {
            OrganizationId = organizationId,
            TimeZoneId = settings.TimeZoneId,
            WorkingDays = ParseWorkingDays(settings.WorkingDaysJson),
            WorkingHours = new WorkingHoursDto
            {
                Start = FormatTime(settings.WorkingHoursStart),
                End = FormatTime(settings.WorkingHoursEnd)
            },
            Holidays = holidays
        };
    }

    private static OrganizationSettings CreateDefaultSettings(Guid organizationId) => CreateDefaultSettingsEntity(organizationId);

    internal static OrganizationSettings CreateDefaultSettingsEntity(Guid organizationId) => new()
    {
        OrganizationId = organizationId,
        TimeZoneId = OrganizationSettingsDefaults.DefaultTimeZoneId,
        LanguageCode = OrganizationSettingsDefaults.DefaultLanguageCode,
        WorkingDaysJson = OrganizationSettingsDefaults.DefaultWorkingDaysJson,
        WorkingHoursStart = OrganizationSettingsDefaults.DefaultWorkingHoursStart,
        WorkingHoursEnd = OrganizationSettingsDefaults.DefaultWorkingHoursEnd,
        RowVersion = Array.Empty<byte>()
    };

    private OrganizationSettingsDto MapToDto(Organization org, OrganizationSettings settings, bool canEdit) => new()
    {
        OrganizationId = org.Id,
        OrganizationName = org.Name,
        OrganizationCode = org.OrganizationCode,
        LogoUrl = string.IsNullOrWhiteSpace(settings.LogoStoragePath)
            ? null
            : $"/api/v1/organizations/{org.Id}/settings/logo",
        TimeZoneId = settings.TimeZoneId,
        LanguageCode = settings.LanguageCode,
        WorkingDays = ParseWorkingDays(settings.WorkingDaysJson),
        WorkingHours = new WorkingHoursDto
        {
            Start = FormatTime(settings.WorkingHoursStart),
            End = FormatTime(settings.WorkingHoursEnd)
        },
        CanEdit = canEdit,
        RowVersion = settings.RowVersion.Length == 0 ? null : Convert.ToBase64String(settings.RowVersion)
    };

    internal static IReadOnlyList<string> ParseWorkingDays(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return Array.Empty<string>();
        try
        {
            return JsonSerializer.Deserialize<List<string>>(json, JsonOptions) ?? [];
        }
        catch
        {
            return Array.Empty<string>();
        }
    }

    internal static string SerializeWorkingDays(IReadOnlyList<string> days) =>
        JsonSerializer.Serialize(days.OrderBy(d => Array.IndexOf(OrganizationSettingsDefaults.Weekdays, d)).ToArray(), JsonOptions);

    internal static TimeSpan ParseTime(string value)
    {
        if (!TimeSpan.TryParse(value, out var time))
            throw new InvalidOperationException($"Invalid time value: {value}");
        return time;
    }

    private static string FormatTime(TimeSpan time) => time.ToString(@"hh\:mm");

    private static string FormatHours(TimeSpan start, TimeSpan end) => $"{FormatTime(start)}-{FormatTime(end)}";
}
