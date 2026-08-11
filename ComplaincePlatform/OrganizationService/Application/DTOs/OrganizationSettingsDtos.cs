namespace OrganizationService.Application.DTOs;

public class OrganizationSettingsDto
{
    public Guid OrganizationId { get; set; }
    public string OrganizationName { get; set; } = string.Empty;
    public string OrganizationCode { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }
    public string TimeZoneId { get; set; } = string.Empty;
    public string LanguageCode { get; set; } = string.Empty;
    public IReadOnlyList<string> WorkingDays { get; set; } = Array.Empty<string>();
    public WorkingHoursDto WorkingHours { get; set; } = new();
    public bool CanEdit { get; set; }
    public string? RowVersion { get; set; }
}

public class WorkingHoursDto
{
    public string Start { get; set; } = "09:00";
    public string End { get; set; } = "18:00";
}

public class UpdateOrganizationSettingsRequest
{
    public string OrganizationName { get; set; } = string.Empty;
    public string OrganizationCode { get; set; } = string.Empty;
    public string TimeZoneId { get; set; } = string.Empty;
    public string LanguageCode { get; set; } = string.Empty;
    public IReadOnlyList<string> WorkingDays { get; set; } = Array.Empty<string>();
    public WorkingHoursDto WorkingHours { get; set; } = new();
    public string? RowVersion { get; set; }
}

public class OrganizationSettingsChangeDto
{
    public string Field { get; set; } = string.Empty;
    public string? Previous { get; set; }
    public string? New { get; set; }
}

public sealed class OrganizationAuditLogDto
{
    public Guid Id { get; init; }
    public string EntityType { get; init; } = string.Empty;
    public Guid EntityId { get; init; }
    public string Action { get; init; } = string.Empty;
    public Guid ActorUserId { get; init; }
    public DateTime OccurredAt { get; init; }
}

public sealed class OrgHolidayDto
{
    public Guid Id { get; init; }
    public Guid OrganizationId { get; init; }
    public DateOnly Date { get; init; }
    public string Name { get; init; } = string.Empty;
}

public sealed class CreateOrgHolidayRequest
{
    public DateOnly Date { get; set; }
    public string Name { get; set; } = string.Empty;
}

public sealed class BusinessCalendarDto
{
    public Guid OrganizationId { get; init; }
    public string TimeZoneId { get; init; } = string.Empty;
    public IReadOnlyList<string> WorkingDays { get; init; } = Array.Empty<string>();
    public WorkingHoursDto WorkingHours { get; init; } = new();
    public IReadOnlyList<OrgHolidayDto> Holidays { get; init; } = Array.Empty<OrgHolidayDto>();
}
