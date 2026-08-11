namespace ComplianceService.Application.Journey;

/// <summary>Maps 1–5 scores to UI labels (server is authoritative).</summary>
public static class NabhScoreLabel
{
    public static string FromScore(int score)
    {
        return score switch
        {
            1 => "Poor",
            2 => "Below Expectations",
            3 => "Partial Compliance",
            4 => "Substantial Compliance",
            5 => "Full Compliance",
            _ => "Unknown"
        };
    }
}
