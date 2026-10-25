namespace ComplianceService.Infrastructure.Configs;

public sealed class ReadinessWeightOptions
{
    public const string SectionName = "Reporting:ReadinessWeights";

    public decimal Compliance { get; set; } = 0.40m;
    public decimal EvidenceCoverage { get; set; } = 0.25m;
    public decimal OpenFindings { get; set; } = 0.20m;
    public decimal TaskCompletion { get; set; } = 0.15m;

    public (decimal Compliance, decimal Evidence, decimal Findings, decimal Tasks) Normalized()
    {
        var sum = Compliance + EvidenceCoverage + OpenFindings + TaskCompletion;
        if (sum <= 0m)
        {
            return (0.40m, 0.25m, 0.20m, 0.15m);
        }

        return (Compliance / sum, EvidenceCoverage / sum, OpenFindings / sum, TaskCompletion / sum);
    }
}
