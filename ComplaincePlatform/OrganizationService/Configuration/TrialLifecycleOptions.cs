namespace OrganizationService.Configuration;

/// <summary>CRP Phase 5 — trial lifecycle automation policies.</summary>
public sealed class TrialLifecycleOptions
{
    public const string SectionName = "TrialLifecycle";

    public int[] ReminderDays { get; set; } = [7, 3, 1];
    public int GraceEndingReminderHours { get; set; } = 24;
    public int EvaluationIntervalHours { get; set; } = 24;
    public int BatchSize { get; set; } = 100;
    public bool EnforceReadOnly { get; set; } = true;
    public bool AutoSuspendOrganizationOnExpired { get; set; }
    public int? AutoArchiveAfterExpiredDays { get; set; }
    public double HighSeatUsageThreshold { get; set; } = 0.8;
    public List<string> InternalAlertEmails { get; set; } = [];
    public string? SupportEmail { get; set; } = "support@aovias.com";
    public string? DocsBaseUrl { get; set; } = "/docs";
    public string? UpgradeInfoUrl { get; set; } = "/docs/upgrade";
    public string? CommercialAdminBaseUrl { get; set; } = "http://localhost:4200/commercial-admin";
    public string? PublicAppBaseUrl { get; set; } = "http://localhost:4200";
}
