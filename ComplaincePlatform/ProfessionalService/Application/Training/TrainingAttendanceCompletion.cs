namespace ProfessionalService.Application.Training;

/// <summary>
/// Pure helpers for session attendance completion percentage (unit-tested).
/// </summary>
public static class TrainingAttendanceCompletion
{
    public const string StatusAssigned = "Assigned";
    public const string StatusAttended = "Attended";
    public const string StatusAbsent = "Absent";
    public const string StatusExcused = "Excused";

    public static bool IsCompleted(string? attendanceStatus, bool? passed = null, bool requirePass = false)
    {
        if (!string.Equals(attendanceStatus, StatusAttended, StringComparison.OrdinalIgnoreCase))
            return false;
        if (requirePass && passed != true)
            return false;
        return true;
    }

    public static decimal CalculatePercent(int assignedOrTrackedCount, int completedCount)
    {
        if (assignedOrTrackedCount <= 0) return 0m;
        if (completedCount < 0) completedCount = 0;
        if (completedCount > assignedOrTrackedCount) completedCount = assignedOrTrackedCount;
        return Math.Round((decimal)completedCount / assignedOrTrackedCount * 100m, 2);
    }

    public static decimal CalculatePercent(
        IEnumerable<(string Status, bool? Passed)> rows,
        bool requirePass = false)
    {
        var list = rows?.ToList() ?? new List<(string Status, bool? Passed)>();
        if (list.Count == 0) return 0m;
        var completed = list.Count(r => IsCompleted(r.Status, r.Passed, requirePass));
        return CalculatePercent(list.Count, completed);
    }
}
