using KnowledgeBase.Application.DTOs;
using KnowledgeBase.Application.GapAnalysis;
using KnowledgeBase.Domain.Entities;
using Xunit;

namespace Knowledgebase.Tests;

public sealed class GapRecommendationBuilderTests
{
    private readonly IGapRecommendationBuilder _builder = new GapRecommendationBuilder();

    [Fact]
    public void Compliant_returns_empty_recommendation()
    {
        var knowledge = new KnowledgeItem { HowToComply = "Do something", IsActive = true, Version = 1 };
        Assert.Equal(string.Empty, _builder.Build(knowledge, GapCategory.Compliant));
    }

    [Fact]
    public void Prefers_how_to_comply_for_non_compliant()
    {
        var knowledge = new KnowledgeItem
        {
            HowToComply = "Maintain records",
            EvidenceRequired = "Policy doc",
            IsActive = true,
            Version = 1
        };

        Assert.Equal("Maintain records", _builder.Build(knowledge, GapCategory.Critical));
    }

    [Fact]
    public void Falls_back_to_evidence_and_checklist_when_how_to_comply_missing()
    {
        var knowledge = new KnowledgeItem
        {
            HowToComply = "",
            EvidenceRequired = "Signed policy",
            AuditChecklist = "Verify annual review",
            IsActive = true,
            Version = 1
        };

        var result = _builder.Build(knowledge, GapCategory.Major);
        Assert.Contains("Signed policy", result);
        Assert.Contains("Verify annual review", result);
    }

    [Fact]
    public void Missing_knowledge_uses_default_guidance()
    {
        var result = _builder.Build(null, GapCategory.Minor);
        Assert.Contains("corrective actions", result, StringComparison.OrdinalIgnoreCase);
    }
}
