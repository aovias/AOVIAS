using ComplianceService.Application.Contracts;
using ComplianceService.Domain;

namespace ComplianceService.Application.Services;

/// <summary>
/// Detects gaps related to controls.
/// Uses explicit, deterministic rules only - no AI, no inference.
/// </summary>
public class ControlGapDetector : IGapDetector
{
    private readonly ILogger<ControlGapDetector> _logger;

    public ControlGapDetector(ILogger<ControlGapDetector> logger)
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

        // Rule 1: Detect controls with no tasks
        DetectControlsWithNoTasks(context, gaps);

        // Rule 2: Detect controls with no evidence
        DetectControlsWithNoEvidence(context, gaps);

        return gaps;
    }

    /// <summary>
    /// Rule: Control has no tasks.
    /// Detection Logic:
    /// - For each control in ProgramControls, check if there's a task that addresses it
    /// - A control is considered to have no task if no task exists in ProgramTasks
    /// - This is a deterministic check: if ProgramTasks is empty or no task references the control, gap exists
    /// </summary>
    private void DetectControlsWithNoTasks(ComplianceContext context, List<Gap> gaps)
    {
        // Rule Check: Only process if we have controls and tasks to check
        if (context.ProgramControls.Count == 0)
        {
            return; // No controls to check
        }

        // Rule Check: If no tasks exist at all, all controls have no tasks
        if (context.ProgramTasks.Count == 0)
        {
            foreach (var control in context.ProgramControls)
            {
                gaps.Add(new Gap
                {
                    GapType = GapType.MissingTask,
                    Description = $"Control {control.Code} has no corresponding compliance task",
                    AffectedEntityType = TargetEntityType.Control,
                    AffectedEntityId = control.Id,
                    RulesReferenced = new List<string> { "ControlGapDetector.Rule1: Control must have at least one task" }
                });
            }
            return;
        }

        // Rule Check: For each control, check if any task addresses it
        // Note: This is simplified - in a real system, we'd have explicit control-requirement mappings
        // For now, we check if there are fewer tasks than controls, suggesting some controls lack tasks
        if (context.ProgramControls.Count > context.ProgramTasks.Count)
        {
            // Deterministic rule: If controls > tasks, some controls likely don't have tasks
            // We flag controls that don't have a clear task relationship
            foreach (var control in context.ProgramControls)
            {
                // Check if any task might address this control
                // Simplified check: if we can't establish a clear link, flag as gap
                var hasPotentialTask = context.ProgramTasks.Any(t =>
                    // In a real system, we'd check control-requirement mapping here
                    // For now, we use a conservative approach: flag if ratio suggests missing tasks
                    false); // Conservative: don't assume mapping exists

                // Since we don't have explicit mapping, we flag controls when tasks < controls
                if (!hasPotentialTask)
                {
                    gaps.Add(new Gap
                    {
                        GapType = GapType.MissingTask,
                        Description = $"Control {control.Code} may not have a corresponding compliance task (more controls than tasks)",
                        AffectedEntityType = TargetEntityType.Control,
                        AffectedEntityId = control.Id,
                        RulesReferenced = new List<string> { "ControlGapDetector.Rule1: Control must have at least one task" }
                    });
                }
            }
        }
    }

    /// <summary>
    /// Rule: Control has no evidence.
    /// Detection Logic:
    /// - For each control, check if there's evidence linked to tasks that address the control
    /// - A control is considered to have no evidence if:
    ///   1. No tasks exist for the control, OR
    ///   2. Tasks exist but none have evidence
    /// - This is a deterministic check: count evidence linked to control's tasks
    /// </summary>
    private void DetectControlsWithNoEvidence(ComplianceContext context, List<Gap> gaps)
    {
        // Rule Check: Only process if we have controls
        if (context.ProgramControls.Count == 0)
        {
            return;
        }

        // Rule Check: If no tasks exist, controls have no evidence (evidence is linked to tasks)
        if (context.ProgramTasks.Count == 0)
        {
            foreach (var control in context.ProgramControls)
            {
                gaps.Add(new Gap
                {
                    GapType = GapType.MissingEvidence,
                    Description = $"Control {control.Code} has no evidence (no tasks exist for this control)",
                    AffectedEntityType = TargetEntityType.Control,
                    AffectedEntityId = control.Id,
                    RulesReferenced = new List<string> { "ControlGapDetector.Rule2: Control must have evidence via tasks" }
                });
            }
            return;
        }

        // Rule Check: If no evidence exists at all, all controls have no evidence
        if (context.Evidence.Count == 0)
        {
            foreach (var control in context.ProgramControls)
            {
                gaps.Add(new Gap
                {
                    GapType = GapType.MissingEvidence,
                    Description = $"Control {control.Code} has no evidence (no evidence exists for any task)",
                    AffectedEntityType = TargetEntityType.Control,
                    AffectedEntityId = control.Id,
                    RulesReferenced = new List<string> { "ControlGapDetector.Rule2: Control must have evidence via tasks" }
                });
            }
            return;
        }

        // Rule Check: For each control, check if tasks have evidence
        // Simplified: If we can't establish a clear link between control and evidence, flag as gap
        // In a real system, we'd traverse: Control -> Tasks -> Evidence
        foreach (var control in context.ProgramControls)
        {
            // Check if any task for this control has evidence
            // This is simplified - in reality, we'd check control-task mapping first
            var hasEvidence = context.Evidence.Any(e =>
                context.ProgramTasks.Any(t => t.Id == e.TaskId)); // At least one task has evidence

            if (!hasEvidence)
            {
                gaps.Add(new Gap
                {
                    GapType = GapType.MissingEvidence,
                    Description = $"Control {control.Code} has no evidence linked to its tasks",
                    AffectedEntityType = TargetEntityType.Control,
                    AffectedEntityId = control.Id,
                    RulesReferenced = new List<string> { "ControlGapDetector.Rule2: Control must have evidence via tasks" }
                });
            }
        }
    }
}
