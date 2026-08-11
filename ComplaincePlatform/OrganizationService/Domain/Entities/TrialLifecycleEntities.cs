namespace OrganizationService.Domain.Entities;

/// <summary>CRP Phase 5 — idempotent customer/internal notification ledger.</summary>
public sealed class TrialLifecycleNotificationLog
{
    public Guid Id { get; set; }
    public Guid LicenseKeyId { get; set; }
    public Guid OrganizationId { get; set; }
    public string Milestone { get; set; } = string.Empty;
    public DateTime SentAtUtc { get; set; }
    public string? CorrelationId { get; set; }
    public string? Recipient { get; set; }
}

/// <summary>CRP Phase 5 — batch evaluation run metrics.</summary>
public sealed class TrialLifecycleRun
{
    public Guid Id { get; set; }
    public string Trigger { get; set; } = "Daily";
    public string CorrelationId { get; set; } = string.Empty;
    public DateTime StartedAtUtc { get; set; }
    public DateTime? FinishedAtUtc { get; set; }
    public int ProcessedCount { get; set; }
    public int TransitionCount { get; set; }
    public int ReminderCount { get; set; }
    public int FailureCount { get; set; }
    public string? FailedLicenseKeyIdsJson { get; set; }
    public string? ErrorSummary { get; set; }
}
