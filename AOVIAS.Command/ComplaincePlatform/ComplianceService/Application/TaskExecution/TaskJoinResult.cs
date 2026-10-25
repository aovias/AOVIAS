using ComplianceService.Domain;

namespace ComplianceService.Application.TaskExecution;

/// <summary>Flattened join row for task list queries (EF-translatable projection).</summary>
public sealed class TaskJoinResult
{
    public ComplianceTask Task { get; set; } = null!;
    public Guid ChapterId { get; set; }
    public string OeCode { get; set; } = string.Empty;
    public string OeTitle { get; set; } = string.Empty;
    public string StandardCode { get; set; } = string.Empty;
}
