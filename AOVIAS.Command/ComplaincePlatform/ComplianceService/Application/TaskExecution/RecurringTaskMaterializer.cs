using ComplianceService.Domain;
using ComplianceService.Domain.Enums;

namespace ComplianceService.Application.TaskExecution;

/// <summary>
/// Pure recurrence scheduling + occurrence factory (Hangfire job wraps persistence).
/// </summary>
public static class RecurringTaskMaterializer
{
    public static bool IsDue(RecurringTaskDefinition definition, DateTime utcNow)
    {
        if (!definition.IsActive)
        {
            return false;
        }

        if (definition.EndDate is { } end && end.Date < utcNow.Date)
        {
            return false;
        }

        var next = definition.NextOccurrenceDate ?? definition.StartDate;
        return next.Date <= utcNow.Date;
    }

    public static DateTime ComputeNextOccurrence(RecurringTaskDefinition definition, DateTime fromOccurrence)
    {
        var interval = Math.Max(1, definition.Interval);
        return definition.Frequency switch
        {
            RecurrenceFrequency.Daily => fromOccurrence.Date.AddDays(interval),
            RecurrenceFrequency.Weekly => fromOccurrence.Date.AddDays(7 * interval),
            RecurrenceFrequency.Monthly => fromOccurrence.Date.AddMonths(interval),
            RecurrenceFrequency.Quarterly => fromOccurrence.Date.AddMonths(3 * interval),
            RecurrenceFrequency.Yearly => fromOccurrence.Date.AddYears(interval),
            _ => fromOccurrence.Date.AddMonths(interval)
        };
    }

    public static ComplianceTask MaterializeOccurrence(
        RecurringTaskDefinition definition,
        DateTime occurrenceDate,
        Guid taskId)
    {
        var due = occurrenceDate.Date.AddDays(Math.Max(0, definition.DueDaysOffset));
        return new ComplianceTask
        {
            Id = taskId,
            HospitalId = definition.HospitalId,
            ComplianceStandardId = definition.ComplianceStandardId,
            ObjectiveElementId = definition.ObjectiveElementId,
            DepartmentId = definition.DepartmentId,
            DepartmentName = definition.DepartmentName,
            AssignedToUserId = definition.AssignedToUserId,
            Title = definition.Title,
            Description = definition.Description,
            Category = definition.Category,
            TaskKind = definition.TaskKind,
            Criticality = definition.Criticality,
            Priority = definition.Priority,
            Status = ComplianceTaskStatus.Pending,
            DueDate = due,
            RecurringDefinitionId = definition.Id,
            CreatedAt = DateTime.UtcNow,
            RowVersion = new byte[8]
        };
    }

    public static void AdvanceDefinition(
        RecurringTaskDefinition definition,
        DateTime occurrenceDate,
        Guid materializedTaskId,
        DateTime utcNow)
    {
        definition.LastMaterializedAt = utcNow;
        definition.LastMaterializedTaskId = materializedTaskId;
        definition.NextOccurrenceDate = ComputeNextOccurrence(definition, occurrenceDate);
        if (definition.EndDate is { } end && definition.NextOccurrenceDate.Value.Date > end.Date)
        {
            definition.IsActive = false;
        }
    }
}
