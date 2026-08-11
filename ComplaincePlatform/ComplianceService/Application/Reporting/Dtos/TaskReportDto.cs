namespace ComplianceService.Application.Reporting.Dtos;

public sealed class TaskReportDto
{
    public decimal CompletionRate { get; init; }
    public int OverdueTasks { get; init; }
    public int CompletedTasks { get; init; }
    public int InProgressTasks { get; init; }
    public List<DepartmentTaskDto> DepartmentTasks { get; init; } = new();
    public DateTime GeneratedAt { get; init; } = DateTime.UtcNow;
}

public sealed class DepartmentTaskDto
{
    public string DepartmentName { get; init; } = string.Empty;
    public int Count { get; init; }
}
