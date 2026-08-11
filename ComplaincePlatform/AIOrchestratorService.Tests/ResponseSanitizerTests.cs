using AIOrchestratorService.Application.AI.Classification;
using AIOrchestratorService.Application.AI.Validation;
using Xunit;

namespace AIOrchestratorService.Tests;

public class ResponseSanitizerTests
{
    private readonly ResponseSanitizer _sanitizer = new();

    [Theory]
    [InlineData("The context does not contain any information about NABH.")]
    [InlineData("Facts object is empty.")]
    [InlineData("Retrieval returned zero results.")]
    [InlineData("Vector search failed for this query.")]
    public void DetectsForbiddenPhrases(string text)
    {
        Assert.True(_sanitizer.ContainsForbiddenPhrase(text));
    }

    [Fact]
    public void Sanitize_FullRefusal_ReplacedWithGeneralGuidance()
    {
        var result = _sanitizer.Sanitize(
            "The context does not contain any information about NABH. Facts object is empty.",
            AssistantAnswerMode.GeneralKnowledge);

        Assert.False(_sanitizer.ContainsForbiddenPhrase(result));
        Assert.NotEmpty(result);
    }

    [Fact]
    public void Sanitize_KeepsUsefulSentences_DropsOffendingOnes()
    {
        var result = _sanitizer.Sanitize(
            "NABH is India's national hospital accreditation body and sets quality standards. The context does not contain any information about your scores.",
            AssistantAnswerMode.GeneralKnowledge);

        Assert.Contains("NABH", result);
        Assert.False(_sanitizer.ContainsForbiddenPhrase(result));
    }

    [Fact]
    public void Sanitize_CleanAnswer_PreservedVerbatim()
    {
        const string clean = "CAPA stands for Corrective and Preventive Action, a structured process to address non-conformities.";
        var result = _sanitizer.Sanitize(clean, AssistantAnswerMode.ComplianceKnowledge);
        Assert.Equal(clean, result);
    }

    [Fact]
    public void Sanitize_OrgSpecificEmpty_GivesWorkspaceMessage()
    {
        var result = _sanitizer.Sanitize("Retrieval returned zero results.", AssistantAnswerMode.OrganizationSpecific);
        Assert.False(_sanitizer.ContainsForbiddenPhrase(result));
        Assert.Contains("workspace", result, System.StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("The retrieved context is empty, with no facts or sources related to open findings.")]
    [InlineData("Without organization-specific data, it is impossible to summarize findings.")]
    public void DetectsRefusalReasoningPhrases(string text)
    {
        Assert.True(_sanitizer.ContainsForbiddenPhrase(text));
    }

    [Fact]
    public void SanitizeReasoning_DropsLeakyBullets_KeepsCleanOnes()
    {
        var bullets = new[]
        {
            "The retrieved context is empty, with no facts or sources related to open findings.",
            "Without organization-specific data, it is impossible to summarize findings.",
            "NABH findings are typically categorized by severity and chapter."
        };

        var result = _sanitizer.SanitizeReasoning(bullets);

        Assert.Single(result);
        Assert.Contains("severity", result[0]);
        Assert.DoesNotContain(result, _sanitizer.ContainsForbiddenPhrase);
    }

    [Fact]
    public void SanitizeReasoning_NullSafe()
    {
        Assert.Empty(_sanitizer.SanitizeReasoning(null));
    }
}
