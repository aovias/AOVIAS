using Aovias.Publishing.Contracts;
using Aovias.Publishing.Core.Pipeline;
using Xunit;

namespace Aovias.Publishing.Core.Tests;

public sealed class PublishingPipeline8DTests
{
    [Fact]
    public void CanonicalOrder_ContainsRequiredStages()
    {
        var order = PublishingStageExecutor.CanonicalOrder;
        Assert.Contains(PublishingPipelineStageId.Discovery, order);
        Assert.Contains(PublishingPipelineStageId.PackageVerification, order);
        Assert.Contains(PublishingPipelineStageId.DeploymentCertification, order);
        Assert.Equal(PublishingPipelineStageId.Discovery, order[0]);
        Assert.Equal(PublishingPipelineStageId.DeploymentCertification, order[^1]);
    }

    [Theory]
    [InlineData(PublishingPipelineStageId.DockerBuild, PublishingFailureCategory.Docker)]
    [InlineData(PublishingPipelineStageId.ContractGeneration, PublishingFailureCategory.Contracts)]
    [InlineData(PublishingPipelineStageId.PackageVerification, PublishingFailureCategory.Verification)]
    [InlineData(PublishingPipelineStageId.DeploymentReadinessValidation, PublishingFailureCategory.Drv)]
    public void FailureClassifier_MapsStageToCategory(PublishingPipelineStageId stage, PublishingFailureCategory expected)
        => Assert.Equal(expected, PublishingFailureClassifier.Classify(stage));

    [Fact]
    public void ExecutionOptions_StartFromStage_FiltersLaterStages()
    {
        var options = new PublishingExecutionOptions { StartFromStage = PublishingPipelineStageId.PackageGeneration };
        var stages = PublishingStageExecutor.CanonicalOrder.Where(stage =>
        {
            var started = false;
            foreach (var candidate in PublishingStageExecutor.CanonicalOrder)
            {
                if (candidate == options.StartFromStage)
                {
                    started = true;
                }

                if (candidate == stage)
                {
                    return started;
                }
            }

            return false;
        }).ToList();

        Assert.DoesNotContain(PublishingPipelineStageId.Discovery, stages);
        Assert.Contains(PublishingPipelineStageId.PackageGeneration, stages);
    }

    [Fact]
    public void StageExecutionResult_RecordsStructuredFailure()
    {
        var result = PublishingStageExecutionResult.CreateRunning(PublishingPipelineStageId.PackageGeneration, "PackageGeneration")
            .Complete(PublishingStageState.Failed, TimeSpan.FromSeconds(2), "Checksum mismatch");

        Assert.False(result.Passed);
        Assert.Contains("Checksum mismatch", result.Errors);
    }
}
