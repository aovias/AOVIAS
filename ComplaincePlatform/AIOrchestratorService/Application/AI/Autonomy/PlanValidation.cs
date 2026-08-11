using AIOrchestratorService.Application.AI.Actions;
using AIOrchestratorService.Application.AI.Models;

namespace AIOrchestratorService.Application.AI.Autonomy;

public sealed class PlanValidationIssue
{
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public sealed class PlanValidationResult
{
    public bool IsValid => Issues.Count == 0;
    public List<PlanValidationIssue> Issues { get; set; } = new();
}

public interface IStepDependencyGraph
{
    bool HasCycle(IReadOnlyList<WorkflowStepTemplate> steps);
    bool DependenciesSatisfied(WorkflowStepTemplate step, IReadOnlySet<string> completedStepIds);
    IReadOnlyList<WorkflowStepTemplate> GetReadySteps(
        IReadOnlyList<WorkflowStepTemplate> steps,
        IReadOnlySet<string> completedStepIds);
}

public sealed class StepDependencyGraph : IStepDependencyGraph
{
    public bool HasCycle(IReadOnlyList<WorkflowStepTemplate> steps)
    {
        var map = steps.ToDictionary(x => x.StepId, x => x.DependsOn, StringComparer.OrdinalIgnoreCase);
        var visiting = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var step in steps)
        {
            if (Detect(step.StepId))
            {
                return true;
            }
        }

        return false;

        bool Detect(string node)
        {
            if (visited.Contains(node))
            {
                return false;
            }

            if (!visiting.Add(node))
            {
                return true;
            }

            if (map.TryGetValue(node, out var deps))
            {
                foreach (var dep in deps)
                {
                    if (map.ContainsKey(dep) && Detect(dep))
                    {
                        return true;
                    }
                }
            }

            visiting.Remove(node);
            visited.Add(node);
            return false;
        }
    }

    public bool DependenciesSatisfied(WorkflowStepTemplate step, IReadOnlySet<string> completedStepIds)
    {
        return step.DependsOn.All(x => completedStepIds.Contains(x));
    }

    public IReadOnlyList<WorkflowStepTemplate> GetReadySteps(
        IReadOnlyList<WorkflowStepTemplate> steps,
        IReadOnlySet<string> completedStepIds)
    {
        return steps
            .Where(step => !completedStepIds.Contains(step.StepId) && DependenciesSatisfied(step, completedStepIds))
            .OrderBy(step => step.Sequence)
            .ToList();
    }
}

public interface IPlanValidator
{
    PlanValidationResult Validate(WorkflowTemplate template);
}

public sealed class PlanValidator : IPlanValidator
{
    private readonly IAIToolRegistry _toolRegistry;
    private readonly IStepDependencyGraph _dependencyGraph;

    public PlanValidator(IAIToolRegistry toolRegistry, IStepDependencyGraph dependencyGraph)
    {
        _toolRegistry = toolRegistry;
        _dependencyGraph = dependencyGraph;
    }

    public PlanValidationResult Validate(WorkflowTemplate template)
    {
        var result = new PlanValidationResult();
        var stepIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var sorted = template.Steps.OrderBy(x => x.Sequence).ToList();

        foreach (var step in sorted)
        {
            if (string.IsNullOrWhiteSpace(step.StepId))
            {
                result.Issues.Add(new PlanValidationIssue { Code = "STEP_ID_MISSING", Message = "Step id is required." });
            }
            else if (!stepIds.Add(step.StepId))
            {
                result.Issues.Add(new PlanValidationIssue { Code = "DUPLICATE_STEP", Message = $"Duplicate step id '{step.StepId}'." });
            }

            if (!_toolRegistry.IsAllowed(step.ToolName))
            {
                result.Issues.Add(new PlanValidationIssue { Code = "FORBIDDEN_TOOL", Message = $"Tool '{step.ToolName}' is not allowed." });
            }

            if (!_toolRegistry.IsCompatible(step.ToolName, step.ToolVersion, step.ToolSchemaVersion))
            {
                result.Issues.Add(new PlanValidationIssue
                {
                    Code = "TOOL_VERSION_INCOMPATIBLE",
                    Message = $"Tool '{step.ToolName}' is not compatible with version '{step.ToolVersion}' and schema '{step.ToolSchemaVersion}'."
                });
            }

            if (step.IsParallelizable && step.RiskClass == AIActionRiskClass.High)
            {
                result.Issues.Add(new PlanValidationIssue
                {
                    Code = "UNSAFE_PARALLEL_STEP",
                    Message = $"High-risk step '{step.StepId}' cannot be marked parallelizable."
                });
            }
        }

        if (_dependencyGraph.HasCycle(sorted))
        {
            result.Issues.Add(new PlanValidationIssue { Code = "DEPENDENCY_CYCLE", Message = "Circular dependencies detected in workflow steps." });
        }

        foreach (var step in sorted)
        {
            foreach (var dep in step.DependsOn)
            {
                if (!stepIds.Contains(dep))
                {
                    result.Issues.Add(new PlanValidationIssue { Code = "DEPENDENCY_MISSING", Message = $"Step '{step.StepId}' depends on unknown step '{dep}'." });
                }
            }
        }

        foreach (var sequenceGroup in sorted.GroupBy(x => x.Sequence))
        {
            if (sequenceGroup.Count() <= 1)
            {
                continue;
            }

            if (sequenceGroup.Any(x => !x.IsParallelizable))
            {
                result.Issues.Add(new PlanValidationIssue
                {
                    Code = "UNSAFE_PARALLEL_SEQUENCE",
                    Message = $"Sequence '{sequenceGroup.Key}' contains mixed parallel/non-parallel steps."
                });
            }
        }

        return result;
    }
}
