using ComplianceService.Application.Services;
using Xunit;

namespace ComplianceService.Tests;

/// <summary>
/// Verifies the rules-based prediction engine produces correct results per maturity stage.
/// Rules:
///   Core → Commitment unlock:   CoreScore >= 70
///   Commitment → Achievement:   CommitmentScore >= 75
///   Achievement → Excellence:   AchievementScore >= 85
/// </summary>
public sealed class PredictedGapEngineServiceTests
{
    private readonly PredictedGapEngineService _sut = new();

    // ── Commitment stage ─────────────────────────────────────────────────────

    [Fact]
    public void Commitment_IsLocked_WhenCoreScoreBelowThreshold()
    {
        var result = _sut.GeneratePredictions(coreScore: 60m, commitmentScore: 0m, achievementScore: 0m);

        Assert.True(result.Commitment.IsLocked);
    }

    [Fact]
    public void Commitment_IsNotLocked_WhenCoreScoreMeetsThreshold()
    {
        var result = _sut.GeneratePredictions(coreScore: 70m, commitmentScore: 0m, achievementScore: 0m);

        Assert.False(result.Commitment.IsLocked);
    }

    [Fact]
    public void Commitment_HasPredictions_WhenLocked()
    {
        var result = _sut.GeneratePredictions(coreScore: 40m, commitmentScore: 0m, achievementScore: 0m);

        Assert.True(result.Commitment.PredictedGapCount > 0);
        Assert.NotEmpty(result.Commitment.Items);
    }

    [Fact]
    public void Commitment_UnlockCriteriaContainsThreshold()
    {
        var result = _sut.GeneratePredictions(coreScore: 60m, commitmentScore: 0m, achievementScore: 0m);

        Assert.Contains("70", result.Commitment.UnlockCriteria);
    }

    // ── Achievement stage ─────────────────────────────────────────────────────

    [Fact]
    public void Achievement_IsLocked_WhenCommitmentScoreBelowThreshold()
    {
        var result = _sut.GeneratePredictions(coreScore: 80m, commitmentScore: 50m, achievementScore: 0m);

        Assert.True(result.Achievement.IsLocked);
    }

    [Fact]
    public void Achievement_IsNotLocked_WhenCommitmentScoreMeetsThreshold()
    {
        var result = _sut.GeneratePredictions(coreScore: 80m, commitmentScore: 75m, achievementScore: 0m);

        Assert.False(result.Achievement.IsLocked);
    }

    [Fact]
    public void Achievement_HasPredictions_WhenLocked()
    {
        var result = _sut.GeneratePredictions(coreScore: 80m, commitmentScore: 40m, achievementScore: 0m);

        Assert.True(result.Achievement.PredictedGapCount > 0);
        Assert.NotEmpty(result.Achievement.Items);
    }

    // ── Excellence stage ──────────────────────────────────────────────────────

    [Fact]
    public void Excellence_IsLocked_WhenAchievementScoreBelowThreshold()
    {
        var result = _sut.GeneratePredictions(coreScore: 90m, commitmentScore: 80m, achievementScore: 70m);

        Assert.True(result.Excellence.IsLocked);
    }

    [Fact]
    public void Excellence_IsNotLocked_WhenAchievementScoreMeetsThreshold()
    {
        var result = _sut.GeneratePredictions(coreScore: 90m, commitmentScore: 80m, achievementScore: 85m);

        Assert.False(result.Excellence.IsLocked);
    }

    [Fact]
    public void Excellence_HasPredictions_WhenLocked()
    {
        var result = _sut.GeneratePredictions(coreScore: 90m, commitmentScore: 80m, achievementScore: 50m);

        Assert.True(result.Excellence.PredictedGapCount > 0);
        Assert.NotEmpty(result.Excellence.Items);
    }

    // ── General invariants ────────────────────────────────────────────────────

    [Fact]
    public void AllStages_PredictedGapCount_MatchesItemsCount()
    {
        var result = _sut.GeneratePredictions(coreScore: 50m, commitmentScore: 0m, achievementScore: 0m);

        Assert.Equal(result.Commitment.Items.Count, result.Commitment.PredictedGapCount);
        Assert.Equal(result.Achievement.Items.Count, result.Achievement.PredictedGapCount);
        Assert.Equal(result.Excellence.Items.Count, result.Excellence.PredictedGapCount);
    }

    [Fact]
    public void PredictedItems_HaveNonEmptyTitles()
    {
        var result = _sut.GeneratePredictions(coreScore: 40m, commitmentScore: 0m, achievementScore: 0m);

        Assert.All(result.Commitment.Items, item => Assert.NotEmpty(item.Title));
        Assert.All(result.Achievement.Items, item => Assert.NotEmpty(item.Title));
        Assert.All(result.Excellence.Items, item => Assert.NotEmpty(item.Title));
    }

    [Theory]
    [InlineData(0, 0, 0)]
    [InlineData(100, 100, 100)]
    [InlineData(70, 75, 85)]
    public void GeneratePredictions_NeverThrows(double core, double commitment, double achievement)
    {
        var exception = Record.Exception(() =>
            _sut.GeneratePredictions((decimal)core, (decimal)commitment, (decimal)achievement));

        Assert.Null(exception);
    }
}
