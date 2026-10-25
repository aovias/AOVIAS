using AIOrchestratorService.Domain;

namespace AIOrchestratorService.Application.IntentDetection;

public interface IIntentClassifier
{
    Task<IntentResult> ClassifyAsync(string instruction);
}
