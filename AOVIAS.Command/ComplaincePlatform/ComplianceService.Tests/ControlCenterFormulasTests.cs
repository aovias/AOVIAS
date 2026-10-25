using ComplianceService.Application.Services;
using Xunit;

namespace ComplianceService.Tests;

public class ControlCenterFormulasTests
{
    [Fact]
    public void ChapterRiskScore_PerfectCompliance_NoSignals_IsZero()
    {
        var risk = ControlCenterFormulas.ChapterRiskScore(compliancePct: 100m, openFindings: 0, overdueTasks: 0, missingEvidence: 0, auditFailures: 0);
        Assert.Equal(0m, risk);
    }

    [Fact]
    public void ChapterRiskScore_ZeroCompliance_NoSignals_IsComplianceWeight()
    {
        // 0.40 * (100 - 0) = 40
        var risk = ControlCenterFormulas.ChapterRiskScore(compliancePct: 0m, openFindings: 0, overdueTasks: 0, missingEvidence: 0, auditFailures: 0);
        Assert.Equal(40m, risk);
    }

    [Fact]
    public void ChapterRiskScore_BlendsAllSignals_AndClampsComponents()
    {
        // gap = 0.40*(100-50)=20; findings capped (10*20=200 -> 100)*0.20=20; overdue 4*15=60*0.20=12;
        // missing 2*10=20*0.10=2; failures 1*25=25*0.10=2.5  => 56.5
        var risk = ControlCenterFormulas.ChapterRiskScore(compliancePct: 50m, openFindings: 10, overdueTasks: 4, missingEvidence: 2, auditFailures: 1);
        Assert.Equal(56.5m, risk);
    }

    [Theory]
    [InlineData(10, "low")]
    [InlineData(24.99, "low")]
    [InlineData(25, "medium")]
    [InlineData(49.99, "medium")]
    [InlineData(50, "high")]
    [InlineData(90, "high")]
    public void ChapterRiskBand_MapsToUi(double risk, string expected)
    {
        Assert.Equal(expected, ControlCenterFormulas.ChapterRiskBandToUi((decimal)risk));
    }

    [Theory]
    [InlineData(0, "low")]
    [InlineData(39.99, "low")]
    [InlineData(40, "medium")]
    [InlineData(69.99, "medium")]
    [InlineData(70, "high")]
    public void DepartmentRiskBand_MapsToUi(double risk, string expected)
    {
        Assert.Equal(expected, ControlCenterFormulas.DepartmentRiskBandToUi((decimal)risk));
    }

    [Fact]
    public void AuditConfidence_HealthyPosture_IsHigh()
    {
        // 100 - 0 - 0 - 0 - 0 + 0.3*90 + 0.4*90 = 100 (clamped) ; raw = 100+27+36 -> clamp 100
        var confidence = ControlCenterFormulas.AuditConfidence(avgChapterReadiness: 90m, criticalNc: 0, openFindings: 0, overdueCapa: 0, missingClauses: 0, evidenceCoverage: 90m);
        Assert.Equal(100m, confidence);
    }

    [Fact]
    public void AuditConfidence_PenalizesGaps()
    {
        // 100 - 6*2 - 3*5 - 4*1 - 1.5*4 + 0.3*40 + 0.4*60
        // = 100 - 12 - 15 - 4 - 6 + 12 + 24 = 99
        var confidence = ControlCenterFormulas.AuditConfidence(avgChapterReadiness: 60m, criticalNc: 2, openFindings: 5, overdueCapa: 1, missingClauses: 4, evidenceCoverage: 40m);
        Assert.Equal(99m, confidence);
    }

    [Fact]
    public void AuditConfidence_IsClampedToZero()
    {
        var confidence = ControlCenterFormulas.AuditConfidence(avgChapterReadiness: 0m, criticalNc: 50, openFindings: 50, overdueCapa: 50, missingClauses: 50, evidenceCoverage: 0m);
        Assert.Equal(0m, confidence);
    }

    [Fact]
    public void AuditPassProbability_AtThreshold_IsFiftyPercent()
    {
        var probability = ControlCenterFormulas.AuditPassProbability(confidence: 80m, threshold: 80m);
        Assert.Equal(50m, probability);
    }

    [Fact]
    public void AuditPassProbability_AboveThreshold_ExceedsFifty()
    {
        var probability = ControlCenterFormulas.AuditPassProbability(confidence: 95m, threshold: 80m);
        Assert.True(probability > 50m);
        Assert.True(probability <= 100m);
    }

    [Fact]
    public void AuditPassProbability_BelowThreshold_IsBelowFifty()
    {
        var probability = ControlCenterFormulas.AuditPassProbability(confidence: 60m, threshold: 80m);
        Assert.True(probability < 50m);
        Assert.True(probability >= 0m);
    }

    [Theory]
    [InlineData(90, "Audit Ready")]
    [InlineData(75, "On Track")]
    [InlineData(55, "Needs Attention")]
    [InlineData(20, "At Risk")]
    public void ConfidenceStatus_BandsCorrectly(double confidence, string expected)
    {
        Assert.Equal(expected, ControlCenterFormulas.ConfidenceStatus((decimal)confidence));
    }
}
