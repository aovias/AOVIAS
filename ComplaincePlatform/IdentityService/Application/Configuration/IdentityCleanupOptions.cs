namespace IdentityService.Application.Configuration;

public sealed class IdentityCleanupOptions
{
    public const string SectionName = "Identity:Cleanup";

    public int IntervalHours { get; set; } = 6;
    public int RevokedSessionRetentionDays { get; set; } = 30;
    public int LoginAttemptRetentionDays { get; set; } = 90;
    public int SessionHistoryRetentionDays { get; set; } = 180;
}
