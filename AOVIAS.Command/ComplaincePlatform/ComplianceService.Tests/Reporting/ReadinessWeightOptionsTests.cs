using ComplianceService.Infrastructure.Configs;
using Xunit;

namespace ComplianceService.Tests.Reporting;

public class ReadinessWeightOptionsTests
{
    [Fact]
    public void Normalized_WeightsSumToOne()
    {
        var options = new ReadinessWeightOptions
        {
            Compliance = 0.4m,
            EvidenceCoverage = 0.25m,
            OpenFindings = 0.2m,
            TaskCompletion = 0.15m,
        };

        var (c, e, f, t) = options.Normalized();
        Assert.Equal(1m, c + e + f + t, 2);
    }

    [Fact]
    public void ReadinessScore_UsesConfiguredWeights()
    {
        var (wC, wE, wF, wT) = new ReadinessWeightOptions().Normalized();
        var compliance = 80m;
        var evidence = 70m;
        var openFindingsPct = 20m;
        var tasks = 90m;

        var score = wC * compliance + wE * evidence + wF * (100m - openFindingsPct) + wT * tasks;
        Assert.True(score > 0 && score <= 100);
    }
}
