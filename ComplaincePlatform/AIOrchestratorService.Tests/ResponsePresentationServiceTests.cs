using AIOrchestratorService.Application.AI.Classification;
using AIOrchestratorService.Application.AI.Inference;
using AIOrchestratorService.Application.AI.Models;
using AIOrchestratorService.Application.AI.Presentation;
using AIOrchestratorService.Application.AI.Validation;
using Xunit;

namespace AIOrchestratorService.Tests;

public class ResponsePresentationServiceTests
{
    private readonly ResponsePresentationService _service = new(new ResponseSanitizer());

    private static AIRequest Request(string prompt = "What affects my compliance score?") => new()
    {
        Prompt = prompt,
        HospitalId = Guid.NewGuid(),
        UseCase = AIUseCase.QueryCompliance
    };

    private static AIResponse Response(string hint = "Executive summary text.") => new()
    {
        ComplianceHint = hint,
        ConfidenceScore = 0.8m,
        CompositeConfidenceScore = 0.82m,
        ContextVersionId = "ctx-1"
    };

    [Fact]
    public void Build_MapsStructuredModelFields_IntoCards()
    {
        var model = new AIModelResponse
        {
            Hint = "Two high-impact gaps were identified.",
            ExecutiveSummary = "Two high-impact gaps were identified.",
            ExpectedOutcome = "Completing the first two actions improves audit readiness.",
            InsightType = ComplianceInsightType.ComplianceScore,
            Findings =
            {
                new ComplianceFinding { Title = "Access Control Review Missing", Severity = ComplianceSeverity.High, Impact = "Reduces score", Detail = "No review on file." }
            },
            Risks =
            {
                new ComplianceRisk { Title = "Unmonitored access", Severity = ComplianceSeverity.Critical, AffectedControls = { "ACC.1", "HRM.2" } }
            },
            Recommendations =
            {
                new RecommendedAction { Priority = 2, Action = "Attach vulnerability assessment", ExpectedOutcome = "Closes gap" },
                new RecommendedAction { Priority = 1, Action = "Upload access control review", ExpectedOutcome = "Improves readiness" }
            }
        };

        var result = _service.Build(Request(), Response(), model, AssistantAnswerMode.OrganizationSpecific);

        Assert.Equal(ComplianceInsightType.ComplianceScore, result.InsightType);
        Assert.Equal("Two high-impact gaps were identified.", result.ExecutiveSummary);
        Assert.Single(result.Findings);
        Assert.Equal(ComplianceSeverity.High, result.Findings[0].Severity);
        Assert.Single(result.Risks);
        Assert.Equal(2, result.Risks[0].AffectedControls.Count);
        Assert.Equal(2, result.Recommendations.Count);
        // Recommendations are re-numbered sequentially after ordering by priority.
        Assert.Equal("Upload access control review", result.Recommendations[0].Action);
        Assert.Equal(1, result.Recommendations[0].Priority);
        Assert.Equal(2, result.Recommendations[1].Priority);
        Assert.Equal(82, result.Technical.Confidence);
    }

    [Fact]
    public void Build_NoStructuredContent_FallsBackToHintSummary()
    {
        var model = new AIModelResponse { Hint = "Plain answer.", Confidence = 0.5m };

        var result = _service.Build(Request("Explain NABH"), Response("Plain answer."), model, AssistantAnswerMode.ComplianceKnowledge);

        Assert.Equal("Plain answer.", result.ExecutiveSummary);
        Assert.Empty(result.Findings);
        Assert.Empty(result.Risks);
        Assert.Empty(result.Recommendations);
    }

    [Fact]
    public void Build_ScrubsForbiddenTerminology_FromCards()
    {
        var model = new AIModelResponse
        {
            ExecutiveSummary = "Your readiness is improving. The context does not contain access records.",
            Findings =
            {
                new ComplianceFinding { Title = "Vector search failed for evidence.", Severity = ComplianceSeverity.Medium }
            }
        };

        var result = _service.Build(Request(), Response(), model, AssistantAnswerMode.OrganizationSpecific);

        Assert.DoesNotContain("context does not contain", result.ExecutiveSummary, StringComparison.OrdinalIgnoreCase);
        // The only finding's title was entirely forbidden language, so it is dropped.
        Assert.Empty(result.Findings);
    }

    [Fact]
    public void Build_HidesSystemAndFallbackSources_AndNeverLeaksRawIds()
    {
        var response = Response();
        response.SourceReferences =
        [
            new AISourceReference { SourceType = "System", SourceId = "fallback" },
            new AISourceReference { SourceType = "Finding", SourceId = Guid.NewGuid().ToString(), Summary = "Open finding on fire safety" },
            new AISourceReference { SourceType = "Model Knowledge", SourceId = "llm" }
        ];

        var model = new AIModelResponse { ExecutiveSummary = "Summary." };
        var result = _service.Build(Request(), response, model, AssistantAnswerMode.Hybrid);

        Assert.DoesNotContain(result.SupportingEvidence, e => e.SourceType.Contains("System", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.SupportingEvidence, e => e.Label == "Open finding on fire safety");
        Assert.Contains(result.SupportingEvidence, e => e.Label == "General compliance knowledge");
        // No raw GUID is surfaced as a label.
        Assert.DoesNotContain(result.SupportingEvidence, e => Guid.TryParse(e.Label, out _));
    }

    [Fact]
    public void Build_DerivesInsightType_FromPromptWhenModelOmitsIt()
    {
        var model = new AIModelResponse { ExecutiveSummary = "Risk overview." };

        var result = _service.Build(Request("What are the major risks?"), Response(), model, AssistantAnswerMode.OrganizationSpecific);

        Assert.Equal(ComplianceInsightType.Risk, result.InsightType);
    }
}
