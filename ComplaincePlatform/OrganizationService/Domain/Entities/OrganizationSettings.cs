namespace OrganizationService.Domain.Entities;

/// <summary>
/// Organization-wide platform configuration (1:1 with Organization).
/// </summary>
public class OrganizationSettings
{
    public Guid OrganizationId { get; set; }
    public string? LogoStoragePath { get; set; }
    public string BrandingAssetsJson { get; set; } = "{}";
    public string BrandingConfigJson { get; set; } = "{}";
    public string TimeZoneId { get; set; } = OrganizationSettingsDefaults.DefaultTimeZoneId;
    public string LanguageCode { get; set; } = OrganizationSettingsDefaults.DefaultLanguageCode;
    public string WorkingDaysJson { get; set; } = OrganizationSettingsDefaults.DefaultWorkingDaysJson;
    public TimeSpan WorkingHoursStart { get; set; } = OrganizationSettingsDefaults.DefaultWorkingHoursStart;
    public TimeSpan WorkingHoursEnd { get; set; } = OrganizationSettingsDefaults.DefaultWorkingHoursEnd;
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}

public static class OrganizationSettingsDefaults
{
    public const string DefaultTimeZoneId = "Asia/Kolkata";
    public const string DefaultLanguageCode = "en";
    public const string DefaultWorkingDaysJson = "[\"Monday\",\"Tuesday\",\"Wednesday\",\"Thursday\",\"Friday\"]";

    public static readonly TimeSpan DefaultWorkingHoursStart = new(9, 0, 0);
    public static readonly TimeSpan DefaultWorkingHoursEnd = new(18, 0, 0);

    public static readonly string[] SupportedLanguages = ["en"];

    public static readonly string[] Weekdays =
    [
        "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday"
    ];

    public static readonly string[] BrandingAssetKeys =
    [
        "secondary", "favicon", "report", "emailHeader", "emailFooter", "loginBackground"
    ];
}

public static class OrganizationCodeHelper
{
    public static string Normalize(string code) => code.Trim().ToUpperInvariant();

    public static string DeriveFromPlatformId(string platformId)
    {
        var normalized = platformId.Trim().ToUpperInvariant();
        return normalized.Length <= 20 ? normalized : normalized[..20];
    }
}
