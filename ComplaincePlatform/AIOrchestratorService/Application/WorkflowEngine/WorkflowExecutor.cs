using AIOrchestratorService.Domain;

namespace AIOrchestratorService.Application.WorkflowEngine;

public class WorkflowExecutor : IWorkflowExecutor
{
    private readonly IWorkflowRegistry _registry;

    public WorkflowExecutor(IWorkflowRegistry registry)
    {
        _registry = registry;
    }

    public async Task ExecuteAsync(IntentResult intentResult, RequestContext requestContext)
    {
        if (intentResult == null)
            throw new ArgumentNullException(nameof(intentResult));

        if (requestContext == null)
            throw new ArgumentNullException(nameof(requestContext));

        var workflow = _registry.GetWorkflow(intentResult.IntentName);

        if (workflow == null)
        {
            throw new InvalidOperationException($"No workflow registered for intent: {intentResult.IntentName}");
        }

        var orchestrationContext = new OrchestrationContext
        {
            RequestContext = requestContext,
            IntentResult = intentResult
        };

        await workflow.ExecuteAsync(orchestrationContext);
    }
}
