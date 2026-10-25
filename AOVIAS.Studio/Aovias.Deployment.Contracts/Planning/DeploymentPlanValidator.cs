namespace Aovias.Deployment.Contracts.Planning;

/// <summary>The outcome of validating a <see cref="DeploymentPlan"/> (Sprint M2.4 D9).</summary>
public sealed class PlanValidationResult
{
    public required bool IsValid { get; init; }

    public IReadOnlyList<string> Errors { get; init; } = [];
}

/// <summary>
/// Validates a <see cref="DeploymentPlan"/> before execution (Sprint M2.4 D9): circular task
/// dependencies, duplicate phases, missing checkpoints, invalid service ordering, orphan tasks,
/// and unreachable phases. A plan that fails any of these checks must never reach a provisioner —
/// this is the plan-level analogue of Sprint M2.3's <c>RequirementValidator</c>, run once more,
/// independently of whatever produced the plan, so a bug in <see cref="DeploymentPlanBuilder"/>
/// cannot silently hand a broken plan to execution.
/// </summary>
public static class DeploymentPlanValidator
{
    private static readonly DeploymentExecutionPhaseName[] CanonicalPhaseSequence =
    [
        DeploymentExecutionPhaseName.Preflight,
        DeploymentExecutionPhaseName.Infrastructure,
        DeploymentExecutionPhaseName.Upload,
        DeploymentExecutionPhaseName.Configuration,
        DeploymentExecutionPhaseName.Deployment,
        DeploymentExecutionPhaseName.Health,
        DeploymentExecutionPhaseName.Certification,
        DeploymentExecutionPhaseName.Completion
    ];

    public static PlanValidationResult Validate(DeploymentPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);
        var errors = new List<string>();

        ValidatePhases(plan, errors);
        ValidateCheckpoints(plan, errors);
        ValidateServiceOrdering(plan, errors);
        ValidateTasks(plan, errors);

        return new PlanValidationResult { IsValid = errors.Count == 0, Errors = errors };
    }

    private static void ValidatePhases(DeploymentPlan plan, List<string> errors)
    {
        var duplicatePhases = plan.ExecutionPhases
            .GroupBy(phase => phase.Name)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToList();
        foreach (var duplicate in duplicatePhases)
        {
            errors.Add($"Duplicate execution phase '{duplicate}'.");
        }

        var missingPhases = CanonicalPhaseSequence.Except(plan.ExecutionPhases.Select(phase => phase.Name)).ToList();
        foreach (var missing in missingPhases)
        {
            errors.Add($"Plan is missing required execution phase '{missing}' — every plan must declare the full eight-phase sequence.");
        }

        foreach (var phase in plan.ExecutionPhases)
        {
            if (phase.Order != (int)phase.Name)
            {
                errors.Add($"Execution phase '{phase.Name}' has order {phase.Order}, which does not match its canonical position {(int)phase.Name} — phase is unreachable in the fixed sequence.");
            }
        }

        var orders = plan.ExecutionPhases.Select(phase => phase.Order).OrderBy(order => order).ToList();
        for (var index = 0; index < orders.Count; index++)
        {
            if (orders[index] != index + 1)
            {
                errors.Add($"Execution phase ordering is not contiguous starting at 1 (found order {orders[index]} at sequence position {index + 1}).");
                break;
            }
        }
    }

    private static void ValidateCheckpoints(DeploymentPlan plan, List<string> errors)
    {
        if (plan.RollbackPoints.Count == 0)
        {
            errors.Add("Plan declares no rollback checkpoints — every plan must identify at least one safe rollback point.");
        }

        if (plan.CertificationGates.Count == 0)
        {
            errors.Add("Plan declares no certification gates — every plan must identify at least one certification gate.");
        }

        if (plan.RollbackPoints.Count > 0 && !plan.RollbackPoints.Any(point => point.BeforePhase == DeploymentExecutionPhaseName.Certification))
        {
            errors.Add("Plan is missing a rollback checkpoint before the Certification phase.");
        }

        if (plan.RollbackPoints.Count > 0 && !plan.RollbackPoints.Any(point => point.BeforePhase == DeploymentExecutionPhaseName.Deployment))
        {
            errors.Add("Plan is missing a rollback checkpoint before the Deployment phase (before service replacement).");
        }
    }

    private static void ValidateServiceOrdering(DeploymentPlan plan, List<string> errors)
    {
        var serviceIds = plan.Services.Select(service => service.ServiceId).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var dependency in plan.Dependencies)
        {
            if (!serviceIds.Contains(dependency))
            {
                errors.Add($"Startup order references undeclared service '{dependency}' — every entry in Dependencies must correspond to a declared Services entry.");
            }
        }

        var duplicateServiceIds = plan.Services
            .Select(service => service.ServiceId)
            .GroupBy(id => id, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToList();
        foreach (var duplicate in duplicateServiceIds)
        {
            errors.Add($"Duplicate service entry '{duplicate}' in the plan's service graph.");
        }
    }

    private static void ValidateTasks(DeploymentPlan plan, List<string> errors)
    {
        var allTasks = plan.ExecutionPhases.SelectMany(phase => phase.Tasks).ToList();

        var duplicateTaskIds = allTasks
            .Select(task => task.TaskId)
            .GroupBy(id => id, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToList();
        foreach (var duplicate in duplicateTaskIds)
        {
            errors.Add($"Duplicate task id '{duplicate}'.");
        }

        var taskIds = allTasks.Select(task => task.TaskId).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var task in allTasks)
        {
            foreach (var dependsOn in task.DependsOnTaskIds)
            {
                if (!taskIds.Contains(dependsOn))
                {
                    errors.Add($"Task '{task.TaskId}' depends on unknown task '{dependsOn}' — orphaned task dependency.");
                }
            }
        }

        var cycle = DetectCircularDependency(allTasks);
        if (cycle.Count > 0)
        {
            errors.Add($"Circular task dependency detected: {string.Join(" -> ", cycle)}.");
        }
    }

    /// <summary>Three-color DFS, the same pattern <see cref="Capabilities.CapabilityRegistry.DetectCircularDependencies"/> uses (Sprint M2.3 D9), applied here to the plan's task graph.</summary>
    private static List<string> DetectCircularDependency(IReadOnlyList<DeploymentTask> tasks)
    {
        var byId = tasks
            .GroupBy(task => task.TaskId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
        var state = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var stack = new List<string>();

        foreach (var task in tasks)
        {
            var cycle = Visit(task.TaskId);
            if (cycle.Count > 0)
            {
                return cycle;
            }
        }

        return [];

        List<string> Visit(string id)
        {
            if (state.TryGetValue(id, out var existing))
            {
                if (existing == 1)
                {
                    var cycleStart = stack.IndexOf(id);
                    return cycleStart >= 0 ? stack.Skip(cycleStart).Append(id).ToList() : [id];
                }

                return [];
            }

            state[id] = 1;
            stack.Add(id);

            if (byId.TryGetValue(id, out var task))
            {
                foreach (var dependency in task.DependsOnTaskIds)
                {
                    var cycle = Visit(dependency);
                    if (cycle.Count > 0)
                    {
                        return cycle;
                    }
                }
            }

            stack.RemoveAt(stack.Count - 1);
            state[id] = 2;
            return [];
        }
    }
}
