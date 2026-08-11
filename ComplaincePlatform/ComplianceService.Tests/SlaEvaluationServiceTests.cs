using System;
using System.Threading.Tasks;
using ComplianceService.Application.Services;
using ComplianceService.Domain.Enums;
using Xunit;

namespace ComplianceService.Tests;

public sealed class SlaEvaluationServiceTests
{
    [Theory]
    [InlineData(0, SlaStatus.OnTrack)]
    [InlineData(79, SlaStatus.OnTrack)]
    [InlineData(80, SlaStatus.Warning)]
    [InlineData(89, SlaStatus.Warning)]
    [InlineData(90, SlaStatus.AtRisk)]
    [InlineData(99, SlaStatus.AtRisk)]
    public void CalculateStatus_returns_expected_before_due(double percentElapsed, SlaStatus expected)
    {
        var start = DateTime.UtcNow.AddHours(-percentElapsed);
        var due = start.AddHours(100);

        var status = SlaEvaluationService.CalculateStatus(start, due, warningThresholdPercent: 80);

        Assert.Equal(expected, status);
    }

    [Fact]
    public void CalculateStatus_returns_breached_when_past_due()
    {
        var start = DateTime.UtcNow.AddHours(-50);
        var due = DateTime.UtcNow.AddHours(-1);

        var status = SlaEvaluationService.CalculateStatus(start, due, warningThresholdPercent: 80);

        Assert.Equal(SlaStatus.Breached, status);
    }
}
