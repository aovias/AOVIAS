namespace OrganizationService.Domain;

/// <summary>CRP Phase 5 — idempotent trial lifecycle notification milestones.</summary>
public static class TrialLifecycleMilestones
{
    public const string Reminder7Days = "Reminder7Days";
    public const string Reminder3Days = "Reminder3Days";
    public const string Reminder1Day = "Reminder1Day";
    public const string TrialExpired = "TrialExpired";
    public const string GraceStarted = "GraceStarted";
    public const string GraceEnding = "GraceEnding";
    public const string ReadOnlyEnabled = "ReadOnlyEnabled";
    public const string Converted = "Converted";
    public const string HighSeatUsage = "HighSeatUsage";

    public static string ReminderForDays(int days) => days switch
    {
        7 => Reminder7Days,
        3 => Reminder3Days,
        1 => Reminder1Day,
        _ => $"Reminder{days}Days"
    };
}
