using ComplianceService.Application.Aggregation.Models;
using ComplianceService.Application.Journey;
using ComplianceService.Domain;
using Xunit;

namespace ComplianceService.Tests.Journey;

public class NabhAccreditationEvaluatorTests
{
    private static FrameworkAggregateResult BaselineAggregate(decimal overallPercent = 85m)
    {
        var chId = Guid.NewGuid();
        var stId = Guid.NewGuid();
        return new FrameworkAggregateResult
        {
            FrameworkId = Guid.NewGuid(),
            CompliancePercent = overallPercent,
            AverageScore = 4.5m,
            Chapters =
            [
                new ChapterAggregateResult
                {
                    ChapterId = chId,
                    AverageScore = 4.5m,
                    CompliancePercent = 90m,
                    Standards =
                    [
                        new ComplianceStandardAggregateResult
                        {
                            ComplianceStandardId = stId,
                            AverageScore = 4.5m,
                            CompliancePercent = 90m
                        }
                    ]
                }
            ]
        };
    }

    [Fact]
    public void When_all_rules_pass_then_eligible()
    {
        var oeId = Guid.NewGuid();
        var aggregate = BaselineAggregate();
        var scores = new Dictionary<Guid, ObjectiveElementScoreState>
        {
            [oeId] = new ObjectiveElementScoreState(Score: 4, IsApplicable: true)
        };
        var oes = new Dictionary<Guid, ObjectiveElement>
        {
            [oeId] = new ObjectiveElement { Id = oeId, Code = "OE1", Classification = ObjectiveElementClassification.Core }
        };

        var result = NabhAccreditationEvaluator.Evaluate(aggregate, scores, oes);

        Assert.True(result.Eligible);
        Assert.Empty(result.Violations);
    }

    [Fact]
    public void When_overall_below_80_then_not_eligible()
    {
        var oeId = Guid.NewGuid();
        var aggregate = BaselineAggregate(overallPercent: 79m);
        var scores = new Dictionary<Guid, ObjectiveElementScoreState>
        {
            [oeId] = new ObjectiveElementScoreState(Score: 5, IsApplicable: true)
        };
        var oes = new Dictionary<Guid, ObjectiveElement>
        {
            [oeId] = new ObjectiveElement { Id = oeId, Code = "OE1", Classification = ObjectiveElementClassification.Achievement }
        };

        var result = NabhAccreditationEvaluator.Evaluate(aggregate, scores, oes);

        Assert.False(result.Eligible);
        Assert.Contains(result.Violations, v => v.Contains("80", StringComparison.Ordinal));
    }

    [Fact]
    public void When_chapter_average_below_4_then_not_eligible()
    {
        var oeId = Guid.NewGuid();
        var aggregate = BaselineAggregate(85m);
        aggregate.Chapters[0].AverageScore = 3.5m;
        var scores = new Dictionary<Guid, ObjectiveElementScoreState>
        {
            [oeId] = new ObjectiveElementScoreState(Score: 5, IsApplicable: true)
        };
        var oes = new Dictionary<Guid, ObjectiveElement>
        {
            [oeId] = new ObjectiveElement { Id = oeId, Code = "OE1", Classification = ObjectiveElementClassification.Achievement }
        };

        var result = NabhAccreditationEvaluator.Evaluate(aggregate, scores, oes);

        Assert.False(result.Eligible);
        Assert.Contains(result.Violations, v => v.Contains("Chapter", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void When_standard_average_below_4_then_not_eligible()
    {
        var oeId = Guid.NewGuid();
        var aggregate = BaselineAggregate(85m);
        aggregate.Chapters[0].Standards[0].AverageScore = 3m;
        var scores = new Dictionary<Guid, ObjectiveElementScoreState>
        {
            [oeId] = new ObjectiveElementScoreState(Score: 5, IsApplicable: true)
        };
        var oes = new Dictionary<Guid, ObjectiveElement>
        {
            [oeId] = new ObjectiveElement { Id = oeId, Code = "OE1", Classification = ObjectiveElementClassification.Achievement }
        };

        var result = NabhAccreditationEvaluator.Evaluate(aggregate, scores, oes);

        Assert.False(result.Eligible);
        Assert.Contains(result.Violations, v => v.Contains("Standard", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void When_core_oe_score_below_4_then_not_eligible()
    {
        var oeId = Guid.NewGuid();
        var aggregate = BaselineAggregate();
        var scores = new Dictionary<Guid, ObjectiveElementScoreState>
        {
            [oeId] = new ObjectiveElementScoreState(Score: 3, IsApplicable: true)
        };
        var oes = new Dictionary<Guid, ObjectiveElement>
        {
            [oeId] = new ObjectiveElement { Id = oeId, Code = "C1", Classification = ObjectiveElementClassification.Core }
        };

        var result = NabhAccreditationEvaluator.Evaluate(aggregate, scores, oes);

        Assert.False(result.Eligible);
        Assert.Contains(result.Violations, v => v.Contains("Core", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void When_more_than_one_non_na_oe_has_score_at_most_2_then_not_eligible()
    {
        var oe1 = Guid.NewGuid();
        var oe2 = Guid.NewGuid();
        var aggregate = BaselineAggregate();
        var scores = new Dictionary<Guid, ObjectiveElementScoreState>
        {
            [oe1] = new ObjectiveElementScoreState(Score: 2, IsApplicable: true),
            [oe2] = new ObjectiveElementScoreState(Score: 1, IsApplicable: true)
        };
        var oes = new Dictionary<Guid, ObjectiveElement>
        {
            [oe1] = new ObjectiveElement { Id = oe1, Code = "A", Classification = ObjectiveElementClassification.Achievement },
            [oe2] = new ObjectiveElement { Id = oe2, Code = "B", Classification = ObjectiveElementClassification.Achievement }
        };

        var result = NabhAccreditationEvaluator.Evaluate(aggregate, scores, oes);

        Assert.False(result.Eligible);
        Assert.Contains(result.Violations, v => v.Contains("More than one", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Na_excluded_oes_do_not_count_toward_low_score_cap()
    {
        var low1 = Guid.NewGuid();
        var na = Guid.NewGuid();
        var aggregate = BaselineAggregate();
        var scores = new Dictionary<Guid, ObjectiveElementScoreState>
        {
            [low1] = new ObjectiveElementScoreState(Score: 2, IsApplicable: true),
            [na] = new ObjectiveElementScoreState(Score: 1, IsApplicable: false)
        };
        var oes = new Dictionary<Guid, ObjectiveElement>
        {
            [low1] = new ObjectiveElement { Id = low1, Code = "A", Classification = ObjectiveElementClassification.Achievement },
            [na] = new ObjectiveElement { Id = na, Code = "B", Classification = ObjectiveElementClassification.Achievement }
        };

        var result = NabhAccreditationEvaluator.Evaluate(aggregate, scores, oes);

        Assert.True(result.Eligible);
    }
}
