namespace Aovias.Deployment.Contracts.Reconciliation;

/// <summary>
/// Turns a <see cref="DriftReport"/> into a <see cref="ReconciliationPlan"/> (Sprint M2.6 D5): for
/// every finding, decide which <see cref="RepairActionType"/> would resolve it, how risky that
/// action is, and whether the matrix's fixed policy already requires operator approval — all via
/// <see cref="DriftRepairMatrix"/>, never invented ad hoc. No repair execution occurs while
/// planning (Final Engineering Rule: "The Reconciliation Planner decides how drift should be
/// resolved" — deciding, not doing). The sole implementation is <see cref="ReconciliationPlanner"/>.
/// </summary>
public interface IReconciliationPlanner
{
    ReconciliationPlan Plan(DriftReport driftReport);
}

/// <summary>
/// The single <see cref="IReconciliationPlanner"/> implementation (Sprint M2.6 D5). Pure and
/// deterministic: the same <see cref="DriftReport"/> always produces an equal
/// <see cref="ReconciliationPlan"/> (modulo <see cref="ReconciliationPlan.GeneratedUtc"/>).
/// </summary>
public sealed class ReconciliationPlanner : IReconciliationPlanner
{
    public ReconciliationPlan Plan(DriftReport driftReport)
    {
        ArgumentNullException.ThrowIfNull(driftReport);

        var tasks = driftReport.Findings
            .Select(BuildTask)
            // Ascending risk: the lowest-risk, most likely to succeed repairs are attempted
            // first — a deliberate, deterministic execution order (D5), not simply the order
            // findings happened to be detected in.
            .OrderBy(task => task.Risk)
            .ThenBy(task => task.TaskId, StringComparer.Ordinal)
            .ToList();

        return new ReconciliationPlan
        {
            EnvironmentId = driftReport.EnvironmentId,
            ServiceId = driftReport.ServiceId,
            CorrelationId = driftReport.CorrelationId,
            GeneratedUtc = DateTimeOffset.UtcNow,
            RepairTasks = tasks
        };
    }

    private static RepairTask BuildTask(DriftFinding finding)
    {
        var entry = DriftRepairMatrix.Resolve(finding.Category, finding.ResourceType);

        return new RepairTask
        {
            TaskId = $"repair:{finding.FindingId}",
            FindingId = finding.FindingId,
            ActionType = entry.ActionType,
            ResourceType = finding.ResourceType,
            ResourceId = finding.ResourceId,
            Description = $"{entry.ActionType} for {finding.ResourceType} '{finding.ResourceId}' ({finding.Description})",
            Risk = entry.Risk,
            RequiresApprovalByDefault = entry.RequiresApprovalByDefault
        };
    }
}
