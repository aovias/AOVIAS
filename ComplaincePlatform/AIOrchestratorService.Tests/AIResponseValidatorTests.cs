using AIOrchestratorService.Application.AI.Classification;
using AIOrchestratorService.Application.AI.Governance;
using AIOrchestratorService.Application.AI.Models;
using AIOrchestratorService.Application.AI.Validation;
using Xunit;

namespace AIOrchestratorService.Tests;

public class AIResponseValidatorTests
{
    private readonly AIResponseValidator _validator = new(new ResponseSanitizer(), new HallucinationGuard());

    private static AIRequest Request() => new() { Prompt = "What is NABH?" };

    [Fact]
    public void GeneralKnowledge_EmptySources_GetsModelKnowledge_NotSystemFallback()
    {
        var response = new AIResponse
        {
            ComplianceHint = "NABH is India's hospital accreditation body.",
            ConfidenceScore = 0.9m,
            AnswerMode = AssistantAnswerMode.GeneralKnowledge,
            SourceReferences = new List<AISourceReference>()
        };

        var result = _validator.ValidateAndNormalize(Request(), response);

        Assert.Single(result.SourceReferences);
        Assert.Equal("Model Knowledge", result.SourceReferences[0].SourceType);
        Assert.DoesNotContain(result.SourceReferences, s => s.SourceType == "System" && s.SourceId == "fallback");
    }

    [Fact]
    public void OrganizationSpecific_EmptySources_StaysEmpty()
    {
        var response = new AIResponse
        {
            ComplianceHint = "I couldn't find matching records in your workspace.",
            ConfidenceScore = 0.5m,
            AnswerMode = AssistantAnswerMode.OrganizationSpecific,
            SourceReferences = new List<AISourceReference>()
        };

        var result = _validator.ValidateAndNormalize(Request(), response);

        Assert.Empty(result.SourceReferences);
        Assert.DoesNotContain(result.SourceReferences, s => s.SourceType == "System" && s.SourceId == "fallback");
    }

    [Fact]
    public void ExistingSources_ArePreserved()
    {
        var response = new AIResponse
        {
            ComplianceHint = "Your NABH readiness is 82%.",
            ConfidenceScore = 0.8m,
            AnswerMode = AssistantAnswerMode.Hybrid,
            SourceReferences = new List<AISourceReference>
            {
                new()
                {
                    SourceType = "KnowledgeBase",
                    SourceId = "kb-1",
                    Summary = "Hospital readiness is 82%.",
                    Excerpt = "Readiness 82%",
                    RelevanceScore = 0.85m
                }
            }
        };

        var result = _validator.ValidateAndNormalize(Request(), response);

        Assert.Single(result.SourceReferences);
        Assert.Equal("KnowledgeBase", result.SourceReferences[0].SourceType);
    }
}
