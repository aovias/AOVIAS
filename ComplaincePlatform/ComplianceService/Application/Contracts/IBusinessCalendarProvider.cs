namespace ComplianceService.Application.Contracts;

public interface IBusinessCalendarProvider
{
    Task<BusinessCalendarSnapshot> GetAsync(Guid organizationId, CancellationToken cancellationToken = default);
}

public sealed class BusinessCalendarSnapshot
{
    public Guid OrganizationId { get; init; }
    public HashSet<DayOfWeek> WorkingDays { get; init; } = new(BusinessCalendarSnapshotDefaults.WorkingDays);
    public HashSet<DateOnly> Holidays { get; init; } = [];
    public TimeSpan WorkingHoursStart { get; init; } = BusinessCalendarSnapshotDefaults.WorkingHoursStart;
    public TimeSpan WorkingHoursEnd { get; init; } = BusinessCalendarSnapshotDefaults.WorkingHoursEnd;
}

public static class BusinessCalendarSnapshotDefaults
{
    public static readonly DayOfWeek[] WorkingDays =
    [
        DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday
    ];

    public static readonly TimeSpan WorkingHoursStart = new(9, 0, 0);
    public static readonly TimeSpan WorkingHoursEnd = new(18, 0, 0);
}
