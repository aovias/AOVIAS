using AIOrchestratorService.Application.AI.Governance;
using AIOrchestratorService.Application.AI.Models;
using Xunit;

namespace AIOrchestratorService.Tests;

public class HallucinationGuardTests
{
    private readonly HallucinationGuard _guard = new();

    [Fact]
    public void Skips_When_No_Citations_Or_Facts()
    {
        var result = _guard.Evaluate(
            "Your readiness score is 91%.",
            citations: null,
            facts: null,
            currentConfidence: 0.9m);

        Assert.False(result.IsFlagged);
        Assert.Equal(0.9m, result.AdjustedConfidence);
    }

    [Fact]
    public void Flags_Number_Not_Present_In_Citations()
    {
        var citations = new List<AISourceReference>
        {
            new()
            {
                SourceType = "Snapshot",
                SourceId = "score-card",
                Summary = "Hospital readiness score is 78%.",
                Excerpt = "Readiness 78%"
            }
        };

        var result = _guard.Evaluate(
            "Your readiness score is 91%.",
            citations,
            facts: null,
            currentConfidence: 0.88m);

        Assert.True(result.IsFlagged);
        Assert.Contains(result.UnsupportedClaims, c => c.Contains("91", StringComparison.Ordinal));
        Assert.True(result.AdjustedConfidence < 0.88m);
    }

    [Fact]
    public void Accepts_Number_Present_In_Facts()
    {
        var facts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["ReadinessScore"] = "82%"
        };

        var result = _guard.Evaluate(
            "Current readiness is 82%.",
            citations: new List<AISourceReference>
            {
                new() { SourceType = "Compliance", SourceId = "bundle", Summary = "Context bundle" }
            },
            facts,
            currentConfidence: 0.8m);

        Assert.False(result.IsFlagged);
        Assert.Equal(0.8m, result.AdjustedConfidence);
    }

    [Fact]
    public void Flags_Id_Not_In_Grounding()
    {
        var citations = new List<AISourceReference>
        {
            new()
            {
                SourceType = "CAPA",
                SourceId = "capa-100",
                DocumentId = "capa-100",
                Summary = "Overdue CAPA capa-100"
            }
        };

        var result = _guard.Evaluate(
            "Open CAPA-999 immediately.",
            citations,
            facts: null,
            currentConfidence: 0.75m);

        Assert.True(result.IsFlagged);
        Assert.Contains(result.UnsupportedClaims, c => c.Contains("999", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Ignores_Small_Prose_Integers()
    {
        var citations = new List<AISourceReference>
        {
            new() { SourceType = "Policy", SourceId = "p1", Summary = "Hand hygiene policy" }
        };

        var result = _guard.Evaluate(
            "There are 3 steps in the policy review.",
            citations,
            facts: null,
            currentConfidence: 0.7m);

        Assert.False(result.IsFlagged);
    }
}
