using AIOrchestratorService.Domain;

namespace AIOrchestratorService.Application.IntentDetection;

public class BasicIntentClassifier : IIntentClassifier
{
    public Task<IntentResult> ClassifyAsync(string instruction)
    {
        if (string.IsNullOrWhiteSpace(instruction))
        {
            return Task.FromResult(new IntentResult
            {
                IntentName = "Unknown",
                Confidence = 0.0,
                Entities = new Dictionary<string, string>()
            });
        }

        var lowerInstruction = instruction.ToLowerInvariant();

        // Rule-based detection: If instruction contains "start" and "compliance" -> IntentName = "StartCompliance"
        if (lowerInstruction.Contains("start") && lowerInstruction.Contains("compliance"))
        {
            return Task.FromResult(new IntentResult
            {
                IntentName = "StartCompliance",
                Confidence = 0.8, // Rule-based confidence
                Entities = new Dictionary<string, string>()
            });
        }

        // Default: Unknown intent
        return Task.FromResult(new IntentResult
        {
            IntentName = "Unknown",
            Confidence = 0.0,
            Entities = new Dictionary<string, string>()
        });
    }
}
