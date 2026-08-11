namespace AIOrchestratorService.Application.WorkflowEngine;

public interface IWorkflowRegistry
{
    void Register(string intentName, IWorkflow workflow);
    IWorkflow? GetWorkflow(string intentName);
    bool HasWorkflow(string intentName);
}
