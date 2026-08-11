using ComplianceService.Domain.Enums;

namespace ComplianceService.Application.TaskExecution;

public static class TaskStatusMapper
{
    public static TaskApiStatus ToApi(ComplianceTaskStatus status) => status switch
    {
        ComplianceTaskStatus.Pending => TaskApiStatus.NotStarted,
        ComplianceTaskStatus.InProgress => TaskApiStatus.InProgress,
        ComplianceTaskStatus.OnHold => TaskApiStatus.Blocked,
        ComplianceTaskStatus.Blocked => TaskApiStatus.Blocked,
        ComplianceTaskStatus.Completed => TaskApiStatus.Completed,
        _ => TaskApiStatus.NotStarted
    };

    public static bool MatchesApiFilter(ComplianceTaskStatus domain, TaskApiStatus? filter)
    {
        if (!filter.HasValue)
        {
            return true;
        }

        return ToApi(domain) == filter.Value;
    }
}
