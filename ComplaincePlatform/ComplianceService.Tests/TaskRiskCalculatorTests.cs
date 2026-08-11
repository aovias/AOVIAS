using ComplianceService.Application.TaskExecution;
using ComplianceService.Domain.Enums;
using Xunit;

namespace ComplianceService.Tests;

public sealed class TaskRiskCalculatorTests
{
    [Fact]
    public void Low_criticality_low_priority_not_overdue_scores_2_band_Low()
    {
        var today = new DateTime(2026, 4, 21, 0, 0, 0, DateTimeKind.Utc);
        var score = TaskRiskCalculator.ComputeScore(
            new DateTime(2026, 5, 1),
            ComplianceTaskStatus.Pending,
            TaskCriticalityLevel.Low,
            ComplianceTaskPriority.Low,
            today);
        Assert.Equal(2, score);
        Assert.Equal(TaskRiskLevelApi.Low, TaskRiskCalculator.ToRiskLevel(score));
    }

    [Fact]
    public void Overdue_adds_2()
    {
        var today = new DateTime(2026, 4, 21, 0, 0, 0, DateTimeKind.Utc);
        var score = TaskRiskCalculator.ComputeScore(
            new DateTime(2026, 4, 10),
            ComplianceTaskStatus.InProgress,
            TaskCriticalityLevel.Low,
            ComplianceTaskPriority.Low,
            today);
        Assert.Equal(4, score);
        Assert.Equal(TaskRiskLevelApi.Medium, TaskRiskCalculator.ToRiskLevel(score));
    }

    [Fact]
    public void Completed_never_overdue()
    {
        var today = new DateTime(2026, 4, 21, 0, 0, 0, DateTimeKind.Utc);
        Assert.False(TaskRiskCalculator.IsOverdue(new DateTime(2020, 1, 1), ComplianceTaskStatus.Completed, today));
    }
}
