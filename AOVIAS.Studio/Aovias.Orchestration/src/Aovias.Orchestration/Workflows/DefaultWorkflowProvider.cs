using Aovias.Orchestration.Contracts;
using Aovias.Orchestration.Contracts.Enums;
using Aovias.Orchestration.Contracts.Workflows;

namespace Aovias.Orchestration.Workflows;

internal sealed class WorkflowTemplate : IWorkflow
{
    public required string WorkflowName { get; init; }

    public required WorkflowType WorkflowType { get; init; }

    public required string WorkflowVersion { get; init; }

    public required IReadOnlyList<WorkflowStepDefinition> Steps { get; init; }
}

internal sealed class DefaultWorkflowProvider : IWorkflowProvider
{
    private static readonly IReadOnlyList<WorkflowStepDefinition> DeploymentSteps =
    [
        Step("Validate Package", 1),
        Step("Verify Runtime", 2),
        Step("Verify Dependencies", 3),
        Step("Backup Current Release", 4),
        Step("Docker Compose Stop", 5),
        Step("Release Extraction", 6),
        Step("Stage Release", 7),
        Step("Load Docker Image", 8),
        Step("Ensure Runtime Folders", 9),
        Step("Docker Compose Up", 10),
        Step("Configure Nginx", 11),
        Step("Health Verification", 12),
        Step("Deployment Acceptance", 13),
        Step("Activate Release", 14),
        Step("Register Deployment", 15),
        Step("Register Runtime", 16),
        Step("Audit", 17)
    ];

    private static readonly IReadOnlyList<WorkflowStepDefinition> RollbackSteps =
    [
        Step("Docker Compose Stop", 1),
        Step("Restore Previous Release", 2),
        Step("Configure Nginx", 3),
        Step("Docker Compose Up", 4),
        Step("Health Verification", 5),
        Step("Audit", 6)
    ];

    /// <inheritdoc />
    public IEnumerable<IWorkflow> GetWorkflows()
    {
        yield return Create("Deployment Workflow", WorkflowType.Deployment, DeploymentSteps);
        yield return Create("Rollback Workflow", WorkflowType.Rollback, RollbackSteps);
        yield return Create("Service Start", WorkflowType.ServiceStart, [Step("Docker Ready", 1), Step("Start", 2), Step("Health Verification", 3), Step("Audit", 4)]);
        yield return Create("Service Stop", WorkflowType.ServiceStop, [Step("Docker Ready", 1), Step("Stop", 2), Step("Audit", 3)]);
        yield return Create("Service Restart", WorkflowType.ServiceRestart, [Step("Docker Ready", 1), Step("Restart Container", 2), Step("Health Verification", 3), Step("Audit", 4)]);
        yield return Create("Health Verification", WorkflowType.HealthVerification, [Step("Verify Runtime", 1), Step("Health Verification", 2), Step("Audit", 3)]);
        yield return Create("Maintenance", WorkflowType.Maintenance, [Step("Maintenance", 1), Step("Audit", 2)]);
        yield return Create("Recovery", WorkflowType.Recovery, [Step("Recovery", 1), Step("Health Verification", 2), Step("Audit", 3)]);
    }

    private static WorkflowTemplate Create(string name, WorkflowType type, IReadOnlyList<WorkflowStepDefinition> steps)
        => new()
        {
            WorkflowName = name,
            WorkflowType = type,
            WorkflowVersion = "1.0",
            Steps = steps
        };

    private static WorkflowStepDefinition Step(string name, int order)
        => new() { Name = name, Description = name, Order = order };
}
