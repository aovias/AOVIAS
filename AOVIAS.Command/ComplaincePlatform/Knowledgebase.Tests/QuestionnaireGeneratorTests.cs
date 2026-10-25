using KnowledgeBase.Application.DTOs;
using KnowledgeBase.Application.Questionnaire;
using KnowledgeBase.Domain.Entities;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Knowledgebase.Tests;

public sealed class QuestionnaireGeneratorTests
{
    private readonly IQuestionnaireGenerator _generator =
        new QuestionnaireGenerator(NullLogger<QuestionnaireGenerator>.Instance);

    [Fact]
    public void Generate_maps_one_question_per_objective_element()
    {
        var fwId = Guid.NewGuid();
        var chapterId = Guid.NewGuid();
        var standardId = Guid.NewGuid();
        var oe1 = Guid.NewGuid();
        var oe2 = Guid.NewGuid();

        var nodes = new List<ComplianceNode>
        {
            Node(chapterId, fwId, null, "Chapter", "AAC", "Access Assessment", 1),
            Node(standardId, fwId, chapterId, "Standard", "AAC.1", "Standard title", 1,
                requirement: new RequirementDetail
                {
                    Id = Guid.NewGuid(),
                    NodeId = standardId,
                    Intent = "Ensure access is assessed",
                    MeasurableElements = "Document policy and review annually",
                    Classification = "CORE"
                },
                score: new RequirementScore
                {
                    Id = Guid.NewGuid(),
                    NodeId = standardId,
                    MaxScore = 5,
                    Weight = 2.5m,
                    ClassificationId = Guid.NewGuid()
                }),
            Node(oe1, fwId, standardId, "ObjectiveElement", "AAC.1.a", "OE title one", 1,
                knowledge: new KnowledgeItem
                {
                    Id = Guid.NewGuid(),
                    NodeId = oe1,
                    HowToComply = "Maintain access register",
                    IsActive = true,
                    Version = 1
                }),
            Node(oe2, fwId, standardId, "ObjectiveElement", "AAC.1.b", "OE title two", 2,
                knowledge: new KnowledgeItem
                {
                    Id = Guid.NewGuid(),
                    NodeId = oe2,
                    HowToComply = "Review quarterly",
                    IsActive = true,
                    Version = 1
                })
        };

        var result = _generator.Generate(new QuestionnaireInput
        {
            FrameworkId = fwId,
            FrameworkName = "NABH",
            Nodes = nodes
        });

        Assert.Equal(fwId, result.FrameworkId);
        Assert.Equal("NABH", result.FrameworkName);
        Assert.Single(result.Sections);
        Assert.Equal(chapterId, result.Sections[0].ChapterId);
        Assert.Equal("Access Assessment", result.Sections[0].ChapterName);
        Assert.Equal(2, result.Sections[0].Questions.Count);

        var q1 = result.Sections[0].Questions[0];
        Assert.Equal(oe1, q1.Id);
        Assert.Equal(oe1, q1.ComplianceNodeId);
        Assert.Equal("OE title one — Ensure access is assessed", q1.QuestionText);
        Assert.Contains("Maintain access register", q1.HelpText);
        Assert.Contains("Document policy and review annually", q1.HelpText);
        Assert.Equal("CORE", q1.Category);
        Assert.Equal(2.5m, q1.Weight);

        var q2 = result.Sections[0].Questions[1];
        Assert.Equal(oe2, q2.ComplianceNodeId);
        Assert.Equal("OE title two — Ensure access is assessed", q2.QuestionText);
    }

    [Fact]
    public void Generate_orders_sections_and_questions_by_sequence()
    {
        var fwId = Guid.NewGuid();
        var chapter1 = Guid.NewGuid();
        var chapter2 = Guid.NewGuid();
        var std1 = Guid.NewGuid();
        var std2 = Guid.NewGuid();
        var oeLate = Guid.NewGuid();
        var oeEarly = Guid.NewGuid();
        var oeChapter2 = Guid.NewGuid();

        var nodes = new List<ComplianceNode>
        {
            Node(chapter2, fwId, null, "Chapter", "CH2", "Second chapter", 2),
            Node(chapter1, fwId, null, "Chapter", "CH1", "First chapter", 1),
            Node(std1, fwId, chapter1, "Standard", "S1", "Std 1", 1),
            Node(std2, fwId, chapter2, "Standard", "S2", "Std 2", 1),
            Node(oeLate, fwId, std1, "ObjectiveElement", "OE2", "Late OE", 2),
            Node(oeEarly, fwId, std1, "ObjectiveElement", "OE1", "Early OE", 1),
            Node(oeChapter2, fwId, std2, "ObjectiveElement", "OE3", "Chapter 2 OE", 1)
        };

        var result = _generator.Generate(new QuestionnaireInput
        {
            FrameworkId = fwId,
            FrameworkName = "Test",
            Nodes = nodes
        });

        Assert.Equal(2, result.Sections.Count);
        Assert.Equal(chapter1, result.Sections[0].ChapterId);
        Assert.Equal(chapter2, result.Sections[1].ChapterId);
        Assert.Equal(oeEarly, result.Sections[0].Questions[0].Id);
        Assert.Equal(oeLate, result.Sections[0].Questions[1].Id);
    }

    [Fact]
    public void Generate_skips_objective_elements_without_chapter_or_standard()
    {
        var fwId = Guid.NewGuid();
        var orphan = Guid.NewGuid();

        var nodes = new List<ComplianceNode>
        {
            Node(orphan, fwId, null, "ObjectiveElement", "ORPHAN", "Orphan OE", 1)
        };

        var result = _generator.Generate(new QuestionnaireInput
        {
            FrameworkId = fwId,
            FrameworkName = "Test",
            Nodes = nodes
        });

        Assert.Empty(result.Sections);
    }

    [Fact]
    public void Generate_uses_chapter_code_when_standard_classification_missing()
    {
        var fwId = Guid.NewGuid();
        var chapterId = Guid.NewGuid();
        var standardId = Guid.NewGuid();
        var oeId = Guid.NewGuid();

        var nodes = new List<ComplianceNode>
        {
            Node(chapterId, fwId, null, "Chapter", "AAC", "Chapter", 1),
            Node(standardId, fwId, chapterId, "Standard", "AAC.1", "Standard", 1),
            Node(oeId, fwId, standardId, "ObjectiveElement", "AAC.1.a", "OE", 1)
        };

        var result = _generator.Generate(new QuestionnaireInput
        {
            FrameworkId = fwId,
            FrameworkName = "Test",
            Nodes = nodes
        });

        Assert.Equal("AAC", result.Sections[0].Questions[0].Category);
        Assert.Equal(1.0m, result.Sections[0].Questions[0].Weight);
    }

    private static ComplianceNode Node(
        Guid id,
        Guid frameworkId,
        Guid? parentId,
        string nodeType,
        string code,
        string title,
        int sequence,
        RequirementDetail? requirement = null,
        RequirementScore? score = null,
        KnowledgeItem? knowledge = null)
    {
        var node = new ComplianceNode
        {
            Id = id,
            FrameworkId = frameworkId,
            ParentId = parentId,
            Level = parentId is null ? 1 : 2,
            Code = code,
            Title = title,
            Description = string.Empty,
            NodeType = nodeType,
            Sequence = sequence,
            RequirementDetail = requirement,
            RequirementScore = score
        };

        if (requirement is not null)
            requirement.NodeId = id;

        if (score is not null)
            score.NodeId = id;

        if (knowledge is not null)
        {
            knowledge.NodeId = id;
            node.KnowledgeItems.Add(knowledge);
        }

        return node;
    }
}
