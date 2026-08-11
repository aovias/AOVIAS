using ComplianceService.Application.TaskExecution;
using ComplianceService.Domain;
using ComplianceService.Domain.Enums;
using Xunit;

namespace ComplianceService.Tests;

public sealed class RecurringTaskMaterializerTests
{
    [Fact]
    public void IsDue_active_definition_with_past_next_occurrence()
    {
        var def = new RecurringTaskDefinition
        {
            Id = Guid.NewGuid(),
            IsActive = true,
            StartDate = new DateTime(2026, 1, 1),
            NextOccurrenceDate = new DateTime(2026, 7, 1),
            Frequency = RecurrenceFrequency.Monthly,
            Interval = 1
        };

        Assert.True(RecurringTaskMaterializer.IsDue(def, new DateTime(2026, 7, 26)));
        Assert.False(RecurringTaskMaterializer.IsDue(def, new DateTime(2026, 6, 15)));
    }

    [Fact]
    public void MaterializeOccurrence_copies_template_and_sets_due_date()
    {
        var defId = Guid.NewGuid();
        var hospitalId = Guid.NewGuid();
        var oeId = Guid.NewGuid();
        var stdId = Guid.NewGuid();
        var def = new RecurringTaskDefinition
        {
            Id = defId,
            HospitalId = hospitalId,
            ComplianceStandardId = stdId,
            ObjectiveElementId = oeId,
            AssignedToUserId = "user-1",
            Title = "Monthly review",
            Category = TaskCategory.DocumentReview,
            TaskKind = TaskKind.Review,
            Criticality = TaskCriticalityLevel.High,
            Priority = ComplianceTaskPriority.High,
            Frequency = RecurrenceFrequency.Monthly,
            DueDaysOffset = 7,
            StartDate = new DateTime(2026, 7, 1)
        };

        var taskId = Guid.NewGuid();
        var task = RecurringTaskMaterializer.MaterializeOccurrence(def, new DateTime(2026, 7, 1), taskId);

        Assert.Equal(taskId, task.Id);
        Assert.Equal(hospitalId, task.HospitalId);
        Assert.Equal(oeId, task.ObjectiveElementId);
        Assert.Equal(stdId, task.ComplianceStandardId);
        Assert.Equal("Monthly review", task.Title);
        Assert.Equal(TaskCategory.DocumentReview, task.Category);
        Assert.Equal(TaskKind.Review, task.TaskKind);
        Assert.Equal(defId, task.RecurringDefinitionId);
        Assert.Equal(new DateTime(2026, 7, 8), task.DueDate);
        Assert.Equal(ComplianceTaskStatus.Pending, task.Status);
    }

    [Fact]
    public void AdvanceDefinition_moves_next_occurrence_and_deactivates_past_end()
    {
        var def = new RecurringTaskDefinition
        {
            Id = Guid.NewGuid(),
            IsActive = true,
            Frequency = RecurrenceFrequency.Weekly,
            Interval = 1,
            StartDate = new DateTime(2026, 7, 1),
            NextOccurrenceDate = new DateTime(2026, 7, 1),
            EndDate = new DateTime(2026, 7, 10)
        };

        var taskId = Guid.NewGuid();
        RecurringTaskMaterializer.AdvanceDefinition(def, new DateTime(2026, 7, 1), taskId, DateTime.UtcNow);

        Assert.Equal(taskId, def.LastMaterializedTaskId);
        Assert.Equal(new DateTime(2026, 7, 8), def.NextOccurrenceDate);
        Assert.True(def.IsActive);

        RecurringTaskMaterializer.AdvanceDefinition(def, def.NextOccurrenceDate!.Value, Guid.NewGuid(), DateTime.UtcNow);
        Assert.Equal(new DateTime(2026, 7, 15), def.NextOccurrenceDate);
        Assert.False(def.IsActive);
    }

    [Theory]
    [InlineData(RecurrenceFrequency.Daily, 1, 1)]
    [InlineData(RecurrenceFrequency.Weekly, 2, 14)]
    public void ComputeNextOccurrence_daily_weekly(RecurrenceFrequency freq, int interval, int expectedDays)
    {
        var def = new RecurringTaskDefinition { Frequency = freq, Interval = interval };
        var next = RecurringTaskMaterializer.ComputeNextOccurrence(def, new DateTime(2026, 7, 1));
        Assert.Equal(new DateTime(2026, 7, 1).AddDays(expectedDays), next);
    }
}
