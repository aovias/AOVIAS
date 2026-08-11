using AIOrchestratorService.Application.AI.Models;

namespace AIOrchestratorService.Application.AI.Autonomy;

public sealed class WorkflowStepTemplate
{
    public string StepId { get; set; } = string.Empty;
    public int Sequence { get; set; }
    public string ToolName { get; set; } = string.Empty;
    public string ToolVersion { get; set; } = "1.0";
    public string ToolSchemaVersion { get; set; } = "1.0";
    public List<string> DependsOn { get; set; } = new();
    public bool IsParallelizable { get; set; }
    public bool RequiresApproval { get; set; }
    public int MaxRetries { get; set; } = 2;
    public AIActionRiskClass RiskClass { get; set; } = AIActionRiskClass.Low;
    public string CompensationHandler { get; set; } = "fallback";
}

public sealed class WorkflowTemplate
{
    public string TemplateId { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public List<WorkflowStepTemplate> Steps { get; set; } = new();
}

public interface IWorkflowTemplateRegistry
{
    WorkflowTemplate ResolveForGoal(string goal);
    WorkflowTemplate? GetById(string templateId, string version);
}

public sealed class WorkflowTemplateRegistry : IWorkflowTemplateRegistry
{
    private readonly List<WorkflowTemplate> _templates = new()
    {
        new WorkflowTemplate
        {
            TemplateId = "AuditPreparationWorkflow",
            Version = "1.0",
            Name = "Audit Preparation Workflow",
            Steps = new List<WorkflowStepTemplate>
            {
                new() { StepId = "fetch_tasks", Sequence = 1, ToolName = "query_tasks", IsParallelizable = false, RequiresApproval = false },
                new() { StepId = "identify_missing_oes", Sequence = 2, ToolName = "analyze_missing_oes", DependsOn = new List<string>{"fetch_tasks"} },
                new() { StepId = "create_tasks", Sequence = 3, ToolName = "create_task", DependsOn = new List<string>{"identify_missing_oes"}, RequiresApproval = true, RiskClass = AIActionRiskClass.High, CompensationHandler = "undo_create_task" },
                new() { StepId = "suggest_evidence", Sequence = 4, ToolName = "link_evidence", DependsOn = new List<string>{"create_tasks"}, RequiresApproval = true, CompensationHandler = "unlink_evidence" },
                new() { StepId = "raise_preliminary_findings", Sequence = 5, ToolName = "create_finding", DependsOn = new List<string>{"suggest_evidence"}, RequiresApproval = true, RiskClass = AIActionRiskClass.High, CompensationHandler = "close_finding" }
            }
        },
        new WorkflowTemplate
        {
            TemplateId = "ComplianceGapClosureWorkflow",
            Version = "1.0",
            Name = "Compliance Gap Closure Workflow",
            Steps = new List<WorkflowStepTemplate>
            {
                new() { StepId = "fetch_gaps", Sequence = 1, ToolName = "analyze_missing_oes" },
                new() { StepId = "prepare_gap_tasks", Sequence = 2, ToolName = "create_task", DependsOn = new List<string>{"fetch_gaps"}, RequiresApproval = true, RiskClass = AIActionRiskClass.High, CompensationHandler = "undo_create_task" },
                new() { StepId = "schedule_evidence_actions", Sequence = 3, ToolName = "link_evidence", DependsOn = new List<string>{"prepare_gap_tasks"}, RequiresApproval = true, CompensationHandler = "unlink_evidence" }
            }
        },
        new WorkflowTemplate
        {
            TemplateId = "EvidenceCompletionWorkflow",
            Version = "1.0",
            Name = "Evidence Completion Workflow",
            Steps = new List<WorkflowStepTemplate>
            {
                new() { StepId = "validate_evidence", Sequence = 1, ToolName = "validate_evidence" },
                new() { StepId = "recommend_task", Sequence = 2, ToolName = "create_task", DependsOn = new List<string>{"validate_evidence"}, RequiresApproval = true, CompensationHandler = "undo_create_task" },
                new() { StepId = "suggest_mapping", Sequence = 3, ToolName = "link_evidence", DependsOn = new List<string>{"validate_evidence"}, IsParallelizable = true, RequiresApproval = true, CompensationHandler = "unlink_evidence" }
            }
        }
    };

    public WorkflowTemplate ResolveForGoal(string goal)
    {
        if (goal.Contains("evidence", StringComparison.OrdinalIgnoreCase))
        {
            return _templates.First(x => x.TemplateId == "EvidenceCompletionWorkflow");
        }

        if (goal.Contains("gap", StringComparison.OrdinalIgnoreCase) || goal.Contains("failing", StringComparison.OrdinalIgnoreCase))
        {
            return _templates.First(x => x.TemplateId == "ComplianceGapClosureWorkflow");
        }

        return _templates.First(x => x.TemplateId == "AuditPreparationWorkflow");
    }

    public WorkflowTemplate? GetById(string templateId, string version)
    {
        return _templates.FirstOrDefault(x =>
            string.Equals(x.TemplateId, templateId, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(x.Version, version, StringComparison.OrdinalIgnoreCase));
    }
}
