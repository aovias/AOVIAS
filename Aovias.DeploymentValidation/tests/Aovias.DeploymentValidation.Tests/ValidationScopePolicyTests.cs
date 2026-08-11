using Aovias.DeploymentValidation.Models;
using Aovias.DeploymentValidation.Services;
using Xunit;

namespace Aovias.DeploymentValidation.Tests;

public sealed class ValidationScopePolicyTests
{
    [Theory]
    [InlineData(ValidationExecutionScope.Publishing, ValidationExecutionScope.Publishing, true)]
    [InlineData(ValidationExecutionScope.Publishing, ValidationExecutionScope.Deployment, false)]
    [InlineData(ValidationExecutionScope.Publishing, ValidationExecutionScope.Both, true)]
    [InlineData(ValidationExecutionScope.Deployment, ValidationExecutionScope.Deployment, true)]
    [InlineData(ValidationExecutionScope.Deployment, ValidationExecutionScope.Publishing, false)]
    public void ShouldExecute_respects_pipeline_scope(
        ValidationExecutionScope contextScope,
        ValidationExecutionScope stageScope,
        bool expected)
    {
        var descriptor = new ValidationStageDescriptor(ValidationCategory.Runtime, stageScope);
        Assert.Equal(expected, ValidationScopePolicy.ShouldExecute(descriptor, contextScope));
    }

    [Fact]
    public void ShouldDefer_marks_deployment_stages_during_publishing()
    {
        var descriptor = new ValidationStageDescriptor(
            ValidationCategory.Runtime,
            ValidationExecutionScope.Deployment);

        Assert.True(ValidationScopePolicy.ShouldDefer(descriptor, ValidationExecutionScope.Publishing));
        Assert.False(ValidationScopePolicy.ShouldDefer(descriptor, ValidationExecutionScope.Deployment));
    }

    [Fact]
    public void MapDeferredStageName_renames_runtime_bootstrap_for_operators()
        => Assert.Equal("Runtime Verification", ValidationScopePolicy.MapDeferredStageName("Runtime Bootstrap"));
}
