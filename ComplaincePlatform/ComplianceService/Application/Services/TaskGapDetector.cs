using ComplianceService.Application.Contracts;
using ComplianceService.Domain;

namespace ComplianceService.Application.Services;

/// <summary>
/// Detects gaps related to compliance tasks.
/// Uses explicit, deterministic rules only - no AI, no inference.
/// </summary>
public class TaskGapDetector : IGapDetector
{
    private readonly ILogger<TaskGapDetector> _logger;

    public TaskGapDetector(ILogger<TaskGapDetector> logger)
    {
        _logger = logger;
    }

    public List<Gap> Detect(ComplianceContext context)
    {
        var gaps = new List<Gap>();

        if (context == null)
        {
            _logger.LogWarning("ComplianceContext is null, returning empty gap list");
            return gaps;
        }

        // Rule 1: Detect tasks with no evidence
        DetectTasksWithNoEvidence(context, gaps);

        // Rule 2: Detect overdue tasks
        DetectOverdueTasks(context, gaps);

        return gaps;
    }

    /// <summary>
    /// Rule: Task has no evidence.
    /// Detection Logic:
    /// - For each task in ProgramTasks, check if there's evidence linked to it
    /// - A task is considered to have no evidence if:
    ///   1. No evidence exists in the Evidence list, OR
    ///   2. No evidence has TaskId matching the task's Id
    /// - This is a deterministic check: count evidence where Evidence.TaskId == Task.Id
    /// </summary>
    private void DetectTasksWithNoEvidence(ComplianceContext context, List<Gap> gaps)
    {
        // Rule Check: Only process if we have tasks
        if (context.ProgramTasks.Count == 0)
        {
            return; // No tasks to check
        }

        // Rule Check: If no evidence exists at all, all tasks have no evidence
        if (context.Evidence.Count == 0)
        {
            foreach (var task in context.ProgramTasks)
            {
                // Rule: Skip completed tasks (they may not need evidence)
                if (IsTaskCompleted(task))
                {
                    continue;
                }

                gaps.Add(new Gap
                {
                    GapType = GapType.MissingEvidence,
                    Description = $"Task for objective element {task.ObjectiveElementId} has no evidence",
                    AffectedEntityType = TargetEntityType.Task,
                    AffectedEntityId = task.Id,
                    RulesReferenced = new List<string> { "TaskGapDetector.Rule1: Task must have evidence unless completed" }
                });
            }
            return;
        }

        // Rule Check: For each task, check if evidence exists
        foreach (var task in context.ProgramTasks)
        {
            // Rule: Skip completed tasks
            if (IsTaskCompleted(task))
            {
                continue;
            }

            // Deterministic check: Count evidence linked to this task
            var hasEvidence = context.Evidence.Any(e => e.TaskId == task.Id);

            if (!hasEvidence)
            {
                gaps.Add(new Gap
                {
                    GapType = GapType.MissingEvidence,
                    Description = $"Task {task.Id} has no evidence",
                    AffectedEntityType = TargetEntityType.Task,
                    AffectedEntityId = task.Id,
                    RulesReferenced = new List<string> { "TaskGapDetector.Rule1: Task must have evidence unless completed" }
                });
            }
        }
    }

    /// <summary>
    /// Rule: Task is overdue (if due date exists).
    /// Detection Logic:
    /// - For each task, check if DueDate exists and is in the past
    /// - A task is overdue if:
    ///   1. DueDate is set (not default), AND
    ///   2. DueDate < current date/time, AND
    ///   3. Task is not completed
    /// - This is a deterministic check: DateTime comparison only
    /// </summary>
    private void DetectOverdueTasks(ComplianceContext context, List<Gap> gaps)
    {
        // Rule Check: Only process if we have tasks
        if (context.ProgramTasks.Count == 0)
        {
            return;
        }

        var now = DateTime.UtcNow;

        foreach (var task in context.ProgramTasks)
        {
            // Rule Check: Skip completed tasks (they can't be overdue)
            if (IsTaskCompleted(task))
            {
                continue;
            }

            if (task.DueDate is not { } dueDate)
            {
                continue;
            }

            // Deterministic check: Compare DueDate with current time
            if (dueDate < now)
            {
                var daysOverdue = (now - dueDate).Days;

                gaps.Add(new Gap
                {
                    GapType = GapType.MissingTask, // Using MissingTask as closest match, or could add new type
                    Description = $"Task {task.Id} is overdue by {daysOverdue} day(s). Due date: {dueDate:yyyy-MM-dd}",
                    AffectedEntityType = TargetEntityType.Task,
                    AffectedEntityId = task.Id,
                    RulesReferenced = new List<string> { "TaskGapDetector.Rule2: Task must be completed before due date" }
                });
            }
        }
    }

    /// <summary>
    /// Helper: Determines if a task is completed.
    /// Rule: Task is completed if Status equals "Completed" or "Done" (case-insensitive).
    /// </summary>
    private static bool IsTaskCompleted(ComplianceTask task) =>
        ComplianceTaskStatusExtensions.IsCompletedForScoring(task.Status.ToScoringStatusString());
}
