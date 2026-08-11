using ComplianceService.Domain;
using ComplianceService.Domain.Enums;

namespace ComplianceService.Application.TaskExecution;

/// <summary>
/// Deterministic risk scoring — must stay in sync with WebApp <c>task-risk.util.ts</c>.
/// </summary>
public static class TaskRiskCalculator
{
    public static int CriticalityWeight(TaskCriticalityLevel c) => c switch
    {
        TaskCriticalityLevel.Critical => 4,
        TaskCriticalityLevel.High => 3,
        TaskCriticalityLevel.Medium => 2,
        TaskCriticalityLevel.Low => 1,
        _ => 2
    };

    public static int PriorityWeight(ComplianceTaskPriority p) => p switch
    {
        ComplianceTaskPriority.Urgent => 4,
        ComplianceTaskPriority.High => 3,
        ComplianceTaskPriority.Medium => 2,
        ComplianceTaskPriority.Low => 1,
        _ => 2
    };

    public static bool IsOverdue(DateTime? dueDate, ComplianceTaskStatus status, DateTime todayUtc)
    {
        var api = TaskStatusMapper.ToApi(status);
        return dueDate.HasValue
               && dueDate.Value.Date < todayUtc.Date
               && api != TaskApiStatus.Completed;
    }

    public static int ComputeScore(
        DateTime? dueDate,
        ComplianceTaskStatus status,
        TaskCriticalityLevel criticality,
        ComplianceTaskPriority priority,
        DateTime todayUtc)
    {
        var overdueW = IsOverdue(dueDate, status, todayUtc) ? 2 : 0;
        return CriticalityWeight(criticality) + overdueW + PriorityWeight(priority);
    }

    public static TaskRiskLevelApi ToRiskLevel(int riskScore) => riskScore switch
    {
        <= 2 => TaskRiskLevelApi.Low,
        <= 4 => TaskRiskLevelApi.Medium,
        <= 6 => TaskRiskLevelApi.High,
        _ => TaskRiskLevelApi.Critical
    };

    /// <summary>Used in LINQ queries — inline duplicate of <see cref="ComputeScore"/> for EF translation.</summary>
    public static int ComputeScoreFromParts(int criticalityWeight, int overdueWeight, int priorityWeight) =>
        criticalityWeight + overdueWeight + priorityWeight;
}
