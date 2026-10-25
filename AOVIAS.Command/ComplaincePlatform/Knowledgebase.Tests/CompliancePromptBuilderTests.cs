using KnowledgeBase.Application.Services;
using KnowledgeBase.Domain.Entities;
using Xunit;

namespace Knowledgebase.Tests;

public sealed class CompliancePromptBuilderTests
{
    [Fact]
    public void Prompt_contains_node_code_knowledge_and_past_decision()
    {
        var b = new CompliancePromptBuilder();
        var fw = new Framework
        {
            Id = Guid.NewGuid(),
            Code = "T",
            Name = "Test",
            Version = "1",
            Description = "",
            IsActive = true
        };
        var node = new ComplianceNode
        {
            Id = Guid.NewGuid(),
            FrameworkId = fw.Id,
            Code = "AAC.1.a",
            Title = "Title",
            Description = "Desc",
            NodeType = "OE",
            Level = 3,
            Sequence = 1
        };
        var k = new KnowledgeItem
        {
            Id = Guid.NewGuid(),
            NodeId = node.Id,
            Explanation = "Explain this",
            WhyItMatters = "",
            HowToComply = "Do X",
            EvidenceRequired = "",
            AuditChecklist = "",
            RiskLevel = "",
            ImpactLevel = "",
            Tags = "",
            Version = 1,
            IsActive = true
        };
        var d = new DecisionLog
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            FrameworkId = fw.Id,
            NodeId = node.Id,
            AIInteractionId = Guid.NewGuid(),
            ProblemStatement = "P",
            SuggestedAction = "S",
            FinalDecision = "We chose Q",
            Outcome = "Success",
            ConfidenceScore = 1,
            CreatedAt = DateTime.UtcNow
        };

        var prompt = b.BuildAskPrompt(fw, node, new[] { k }, new[] { d }, "Hello?");
        Assert.Contains("AAC.1.a", prompt);
        Assert.Contains("Explain this", prompt);
        Assert.Contains("We chose Q", prompt);
    }
}
