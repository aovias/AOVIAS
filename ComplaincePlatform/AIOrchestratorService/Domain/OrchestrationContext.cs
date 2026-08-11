namespace AIOrchestratorService.Domain;

public class OrchestrationContext
{
    public RequestContext RequestContext { get; set; } = null!;
    public IntentResult IntentResult { get; set; } = null!;
}
