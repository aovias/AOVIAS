namespace ComplianceService.Application.Scheduling;

/// <summary>
/// Working-day / working-hour calendar math for SLA due dates and remaining hours.
/// </summary>
public static class BusinessCalendarCalculator
{
    public static readonly HashSet<DayOfWeek> DefaultWorkingDays =
    [
        DayOfWeek.Monday,
        DayOfWeek.Tuesday,
        DayOfWeek.Wednesday,
        DayOfWeek.Thursday,
        DayOfWeek.Friday
    ];

    public static readonly TimeSpan DefaultWorkingHoursStart = new(9, 0, 0);
    public static readonly TimeSpan DefaultWorkingHoursEnd = new(18, 0, 0);

    public static HashSet<DayOfWeek> ParseWorkingDays(IEnumerable<string>? workingDayNames)
    {
        var result = new HashSet<DayOfWeek>();
        if (workingDayNames == null)
        {
            return new HashSet<DayOfWeek>(DefaultWorkingDays);
        }

        foreach (var name in workingDayNames)
        {
            if (Enum.TryParse<DayOfWeek>(name, ignoreCase: true, out var day))
            {
                result.Add(day);
            }
        }

        return result.Count == 0 ? new HashSet<DayOfWeek>(DefaultWorkingDays) : result;
    }

    public static DateTime AddWorkingHours(
        DateTime startUtc,
        double workingHours,
        IReadOnlySet<DayOfWeek> workingDays,
        IReadOnlySet<DateOnly> holidays,
        TimeSpan dayStart,
        TimeSpan dayEnd)
    {
        if (workingHours <= 0)
        {
            return startUtc;
        }

        var remaining = workingHours;
        var cursor = AlignToWorkingInstant(startUtc, workingDays, holidays, dayStart, dayEnd);

        // Cap iterations to avoid infinite loops on pathological calendars.
        for (var guard = 0; guard < 100_000 && remaining > 0.0001; guard++)
        {
            var date = DateOnly.FromDateTime(cursor);
            if (!IsWorkingDay(date, workingDays, holidays))
            {
                cursor = NextWorkingDayStart(cursor, workingDays, holidays, dayStart);
                continue;
            }

            var dayEndInstant = date.ToDateTime(TimeOnly.FromTimeSpan(dayEnd), DateTimeKind.Utc);
            var available = (dayEndInstant - cursor).TotalHours;
            if (available <= 0)
            {
                cursor = NextWorkingDayStart(cursor, workingDays, holidays, dayStart);
                continue;
            }

            if (remaining <= available)
            {
                return cursor.AddHours(remaining);
            }

            remaining -= available;
            cursor = NextWorkingDayStart(cursor, workingDays, holidays, dayStart);
        }

        return cursor;
    }

    public static double CountWorkingHours(
        DateTime fromUtc,
        DateTime toUtc,
        IReadOnlySet<DayOfWeek> workingDays,
        IReadOnlySet<DateOnly> holidays,
        TimeSpan dayStart,
        TimeSpan dayEnd)
    {
        if (toUtc <= fromUtc)
        {
            return 0;
        }

        var total = 0.0;
        var cursor = AlignToWorkingInstant(fromUtc, workingDays, holidays, dayStart, dayEnd);

        for (var guard = 0; guard < 100_000 && cursor < toUtc; guard++)
        {
            var date = DateOnly.FromDateTime(cursor);
            if (!IsWorkingDay(date, workingDays, holidays))
            {
                cursor = NextWorkingDayStart(cursor, workingDays, holidays, dayStart);
                continue;
            }

            var dayEndInstant = date.ToDateTime(TimeOnly.FromTimeSpan(dayEnd), DateTimeKind.Utc);
            var segmentEnd = toUtc < dayEndInstant ? toUtc : dayEndInstant;
            if (segmentEnd > cursor)
            {
                total += (segmentEnd - cursor).TotalHours;
            }

            if (toUtc <= dayEndInstant)
            {
                break;
            }

            cursor = NextWorkingDayStart(cursor, workingDays, holidays, dayStart);
        }

        return Math.Round(total, 4);
    }

    public static bool IsWorkingDay(DateOnly date, IReadOnlySet<DayOfWeek> workingDays, IReadOnlySet<DateOnly> holidays) =>
        workingDays.Contains(date.DayOfWeek) && !holidays.Contains(date);

    private static DateTime AlignToWorkingInstant(
        DateTime instantUtc,
        IReadOnlySet<DayOfWeek> workingDays,
        IReadOnlySet<DateOnly> holidays,
        TimeSpan dayStart,
        TimeSpan dayEnd)
    {
        var date = DateOnly.FromDateTime(instantUtc);
        var time = instantUtc.TimeOfDay;

        if (!IsWorkingDay(date, workingDays, holidays) || time >= dayEnd)
        {
            return NextWorkingDayStart(instantUtc, workingDays, holidays, dayStart);
        }

        if (time < dayStart)
        {
            return date.ToDateTime(TimeOnly.FromTimeSpan(dayStart), DateTimeKind.Utc);
        }

        return DateTime.SpecifyKind(instantUtc, DateTimeKind.Utc);
    }

    private static DateTime NextWorkingDayStart(
        DateTime fromUtc,
        IReadOnlySet<DayOfWeek> workingDays,
        IReadOnlySet<DateOnly> holidays,
        TimeSpan dayStart)
    {
        var date = DateOnly.FromDateTime(fromUtc).AddDays(1);
        for (var i = 0; i < 3660; i++)
        {
            if (IsWorkingDay(date, workingDays, holidays))
            {
                return date.ToDateTime(TimeOnly.FromTimeSpan(dayStart), DateTimeKind.Utc);
            }

            date = date.AddDays(1);
        }

        return fromUtc.AddDays(1);
    }
}
