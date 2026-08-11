using ComplianceService.Application.Services;
using Xunit;

namespace ComplianceService.Tests;

/// <summary>
/// Verifies the CMI formula: Value = CoreScore×0.40 + CommitmentScore×0.30 + AchievementScore×0.20 + ExcellenceScore×0.10
/// </summary>
public sealed class ComplianceMaturityIndexServiceTests
{
    private readonly ComplianceMaturityIndexService _sut = new();

    [Fact]
    public void Calculate_WithKnownInputs_ReturnsExpectedCMI()
    {
        // Arrange: from the spec example
        // Core=82, Commitment=40, Achievement=15, Excellence=0 → CMI≈47
        // Exact: 82×0.4 + 40×0.3 + 15×0.2 + 0×0.1 = 32.8 + 12 + 3 + 0 = 47.8
        var result = _sut.Calculate(82m, 40m, 15m, 0m);

        Assert.Equal(47.8m, result.Value);
        Assert.Equal(82m, result.CoreScore);
        Assert.Equal(40m, result.CommitmentScore);
        Assert.Equal(15m, result.AchievementScore);
        Assert.Equal(0m, result.ExcellenceScore);
    }

    [Fact]
    public void Calculate_AllZero_ReturnsCmiZero()
    {
        var result = _sut.Calculate(0m, 0m, 0m, 0m);
        Assert.Equal(0m, result.Value);
    }

    [Fact]
    public void Calculate_AllHundred_ReturnsCmiHundred()
    {
        var result = _sut.Calculate(100m, 100m, 100m, 100m);
        Assert.Equal(100m, result.Value);
    }

    [Fact]
    public void Calculate_OnlyCore100_Returns40()
    {
        // CMI = 100×0.40 = 40
        var result = _sut.Calculate(100m, 0m, 0m, 0m);
        Assert.Equal(40m, result.Value);
    }

    [Fact]
    public void Calculate_ClampsNegativeScoreToZero()
    {
        // Negative input → clamped to 0
        var result = _sut.Calculate(-10m, 0m, 0m, 0m);
        Assert.Equal(0m, result.Value);
    }

    [Fact]
    public void Calculate_ClampsOverHundredToHundred()
    {
        // Over-range input → clamped to 100
        var result = _sut.Calculate(110m, 0m, 0m, 0m);
        Assert.Equal(40m, result.Value); // 100×0.40 = 40
    }

    [Fact]
    public void Calculate_WeightsAreCorrect()
    {
        // Validate each weight independently
        var coreOnly       = _sut.Calculate(100m, 0m,    0m,    0m);
        var commitmentOnly = _sut.Calculate(0m,   100m,  0m,    0m);
        var achievementOnly= _sut.Calculate(0m,   0m,    100m,  0m);
        var excellenceOnly = _sut.Calculate(0m,   0m,    0m,    100m);

        Assert.Equal(40m, coreOnly.Value);
        Assert.Equal(30m, commitmentOnly.Value);
        Assert.Equal(20m, achievementOnly.Value);
        Assert.Equal(10m, excellenceOnly.Value);

        // Weights must sum to 100
        Assert.Equal(100m, coreOnly.Value + commitmentOnly.Value + achievementOnly.Value + excellenceOnly.Value);
    }
}
