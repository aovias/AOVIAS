namespace AIOrchestratorService.Application.WorkflowEngine;

public class WorkflowRegistry : IWorkflowRegistry
{
    private readonly Dictionary<string, IWorkflow> _workflows = new();

    public void Register(string intentName, IWorkflow workflow)
    {
        if (string.IsNullOrWhiteSpace(intentName))
            throw new ArgumentException("Intent name cannot be null or empty.", nameof(intentName));

        if (workflow == null)
            throw new ArgumentNullException(nameof(workflow));

        _workflows[intentName] = workflow;
    }

    public IWorkflow? GetWorkflow(string intentName)
    {
        _workflows.TryGetValue(intentName, out var workflow);
        return workflow;
    }

    public bool HasWorkflow(string intentName)
    {
        return _workflows.ContainsKey(intentName);
    }
}
