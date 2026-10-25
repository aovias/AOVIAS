using ComplianceService.Application.Reporting;
using ComplianceService.Domain.Enums;
using Xunit;

namespace ComplianceService.Tests.Reporting;

public class ReportComplianceScoringTests
{
    [Theory]
    [InlineData(ComplianceEvaluationStatus.FullyCompliant, null, 1.0)]
    [InlineData(ComplianceEvaluationStatus.PartiallyCompliant, null, 0.5)]
    [InlineData(ComplianceEvaluationStatus.NonCompliant, null, 0.0)]
    public void StatusWeight_MatchesFormula(ComplianceEvaluationStatus status, string? remarks, decimal expected)
    {
        var weight = ReportComplianceScoring.StatusWeight(status, remarks);
        Assert.Equal(expected, weight);
    }

    [Fact]
    public void WeightedScore_ThreeClauses_MatchesPromptFormula()
    {
        // Compliant=100%, Partial=50%, NonCompliant=0% with equal weights
        var weights = new[]
        {
            ReportComplianceScoring.StatusWeight(ComplianceEvaluationStatus.FullyCompliant, null),
            ReportComplianceScoring.StatusWeight(ComplianceEvaluationStatus.PartiallyCompliant, null),
            ReportComplianceScoring.StatusWeight(ComplianceEvaluationStatus.NonCompliant, null),
        };
        var score = weights.Average() * 100m;
        Assert.Equal(50m, score);
    }
}
