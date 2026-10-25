using AIOrchestratorService.Application.AI.Classification;
using Xunit;

namespace AIOrchestratorService.Tests;

public class AssistantQueryClassifierTests
{
    private readonly AssistantQueryClassifier _classifier = new();

    [Theory]
    [InlineData("What is NABH?")]
    [InlineData("What is CAPA?")]
    [InlineData("Explain FMS chapter.")]
    [InlineData("What is ISO 27001?")]
    [InlineData("Define accreditation")]
    public void GeneralKnowledge_Questions_ClassifyAsGeneralOrCompliance(string prompt)
    {
        var mode = _classifier.Classify(prompt, hasRetrievedData: false);
        Assert.True(
            mode is AssistantAnswerMode.GeneralKnowledge or AssistantAnswerMode.ComplianceKnowledge,
            $"Expected general/compliance knowledge but got {mode}");
        Assert.True(AssistantQueryClassifier.PermitsGeneralKnowledge(mode));
        Assert.False(AssistantQueryClassifier.RequiresOrgData(mode));
    }

    [Theory]
    [InlineData("Show overdue tasks")]
    [InlineData("Which chapters are below 60%?")]
    [InlineData("What audits are upcoming?")]
    [InlineData("Show missing evidence")]
    [InlineData("How many open findings do we have right now?")]
    public void OrganizationSpecific_Questions_RequireOrgData(string prompt)
    {
        var mode = _classifier.Classify(prompt, hasRetrievedData: false);
        Assert.Equal(AssistantAnswerMode.OrganizationSpecific, mode);
        Assert.True(AssistantQueryClassifier.RequiresOrgData(mode));
        Assert.False(AssistantQueryClassifier.PermitsGeneralKnowledge(mode));
    }

    [Theory]
    [InlineData("What is NABH and how compliant are we?")]
    [InlineData("Explain FMS and show our current FMS readiness")]
    public void Hybrid_Questions_ClassifyAsHybrid(string prompt)
    {
        var mode = _classifier.Classify(prompt, hasRetrievedData: false);
        Assert.Equal(AssistantAnswerMode.Hybrid, mode);
        Assert.True(AssistantQueryClassifier.PermitsGeneralKnowledge(mode));
    }

    [Theory]
    [InlineData("Hello")]
    [InlineData("Hi there")]
    [InlineData("Thanks")]
    [InlineData("Good morning")]
    public void SmallTalk_ClassifiesAsSmallTalk(string prompt)
    {
        var mode = _classifier.Classify(prompt, hasRetrievedData: false);
        Assert.Equal(AssistantAnswerMode.SmallTalk, mode);
    }

    [Fact]
    public void GeneralConcept_WithRetrievedData_UpgradesToHybrid()
    {
        var mode = _classifier.Classify("What is NABH?", hasRetrievedData: true);
        Assert.Equal(AssistantAnswerMode.Hybrid, mode);
    }

    [Fact]
    public void EmptyPrompt_DefaultsToGeneralKnowledge()
    {
        Assert.Equal(AssistantAnswerMode.GeneralKnowledge, _classifier.Classify("", false));
        Assert.Equal(AssistantAnswerMode.GeneralKnowledge, _classifier.Classify(null, false));
    }

    [Fact]
    public void UnknownGeneralQuestion_DefaultsToGeneralKnowledge_NotRefusal()
    {
        var mode = _classifier.Classify("How do I improve patient handover communication?", hasRetrievedData: false);
        Assert.True(AssistantQueryClassifier.PermitsGeneralKnowledge(mode));
    }
}
