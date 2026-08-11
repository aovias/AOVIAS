using AIOrchestratorService.Domain;

namespace AIOrchestratorService.Application.WorkflowEngine;

public interface IWorkflow
{
    Task ExecuteAsync(OrchestrationContext context);
}
