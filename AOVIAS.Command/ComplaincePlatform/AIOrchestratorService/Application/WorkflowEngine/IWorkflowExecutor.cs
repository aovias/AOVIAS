using AIOrchestratorService.Domain;

namespace AIOrchestratorService.Application.WorkflowEngine;

public interface IWorkflowExecutor
{
    Task ExecuteAsync(IntentResult intentResult, RequestContext requestContext);
}
