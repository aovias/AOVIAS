using ComplianceService.Domain.Enums;

namespace ComplianceService.Application.Reporting;

public static class ReportComplianceScoring
{
    public static decimal StatusWeight(ComplianceEvaluationStatus status, string? remarks) =>
        status switch
        {
            ComplianceEvaluationStatus.FullyCompliant => 1.0m,
            ComplianceEvaluationStatus.PartiallyCompliant => 0.5m,
            ComplianceEvaluationStatus.NotApplicable => string.IsNullOrWhiteSpace(remarks) ? 0m : -1m,
            _ => 0m
        };

    public static string ScoreToStatusLabel(decimal scorePercent) =>
        scorePercent >= 80m ? "compliant" : scorePercent >= 50m ? "partial" : "non-compliant";

    public static string RiskSeverityFromScore(decimal scorePercent) =>
        scorePercent < 50m ? "high" : scorePercent < 70m ? "medium" : "low";
}
