using ComplianceService.Domain.Enums;
using Shared.Infrastructure.Domain;

namespace ComplianceService.Domain;

/// <summary>
/// Template that Hangfire materializes into concrete <see cref="ComplianceTask"/> occurrences.
/// </summary>
public class RecurringTaskDefinition : BaseEntity
{
    public Guid HospitalId { get; set; }
    public Guid ComplianceStandardId { get; set; }
    public Guid ObjectiveElementId { get; set; }
    public Guid? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public string AssignedToUserId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public TaskCategory Category { get; set; } = TaskCategory.Compliance;
    public TaskKind TaskKind { get; set; } = TaskKind.Standard;
    public TaskCriticalityLevel Criticality { get; set; } = TaskCriticalityLevel.Medium;
    public ComplianceTaskPriority Priority { get; set; } = ComplianceTaskPriority.Medium;
    public RecurrenceFrequency Frequency { get; set; } = RecurrenceFrequency.Monthly;
    public int Interval { get; set; } = 1;
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public DateTime? NextOccurrenceDate { get; set; }
    public DateTime? LastMaterializedAt { get; set; }
    public Guid? LastMaterializedTaskId { get; set; }
    public bool IsActive { get; set; } = true;
    /// <summary>Days from occurrence date to set as the materialized task DueDate.</summary>
    public int DueDaysOffset { get; set; }
}
