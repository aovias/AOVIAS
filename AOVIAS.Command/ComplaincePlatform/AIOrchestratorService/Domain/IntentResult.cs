namespace AIOrchestratorService.Domain;

public class IntentResult
{
    public string IntentName { get; set; } = string.Empty;
    public double Confidence { get; set; }
    public Dictionary<string, string> Entities { get; set; } = new();
}
