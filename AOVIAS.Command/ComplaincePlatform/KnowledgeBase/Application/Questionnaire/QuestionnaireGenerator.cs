using KnowledgeBase.Application.DTOs;
using KnowledgeBase.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace KnowledgeBase.Application.Questionnaire;

public sealed class QuestionnaireGenerator : IQuestionnaireGenerator
{
    private const string ChapterType = "Chapter";
    private const string StandardType = "Standard";
    private const string ObjectiveElementType = "ObjectiveElement";
    private const decimal DefaultWeight = 1.0m;

    private readonly ILogger<QuestionnaireGenerator> _logger;

    public QuestionnaireGenerator(ILogger<QuestionnaireGenerator> logger) => _logger = logger;

    public FrameworkQuestionnaireDto Generate(QuestionnaireInput input)
    {
        var nodeById = input.Nodes.ToDictionary(n => n.Id);
        var sectionQuestions = new Dictionary<Guid, List<(int Sequence, QuestionnaireQuestionDto Question)>>();

        foreach (var oe in input.Nodes.Where(n =>
                     string.Equals(n.NodeType, ObjectiveElementType, StringComparison.OrdinalIgnoreCase)))
        {
            var standardId = FindAncestorOfType(oe.Id, nodeById, StandardType);
            var chapterId = FindAncestorOfType(oe.Id, nodeById, ChapterType);

            if (standardId is null || chapterId is null)
            {
                _logger.LogWarning(
                    "Skipping objective element {ObjectiveElementId} — missing Standard or Chapter ancestor",
                    oe.Id);
                continue;
            }

            var standardNode = nodeById[standardId.Value];
            var question = MapQuestion(oe, standardNode, nodeById[chapterId.Value]);

            if (!sectionQuestions.TryGetValue(chapterId.Value, out var list))
            {
                list = new List<(int, QuestionnaireQuestionDto)>();
                sectionQuestions[chapterId.Value] = list;
            }

            list.Add((oe.Sequence, question));
        }

        var sections = sectionQuestions
            .Select(kv =>
            {
                var chapterNode = nodeById[kv.Key];
                var questions = kv.Value
                    .OrderBy(x => x.Sequence)
                    .Select(x => x.Question)
                    .ToList();
                return (chapterNode.Sequence, Section: new QuestionnaireSectionDto(
                    chapterNode.Id,
                    chapterNode.Title,
                    questions));
            })
            .OrderBy(x => x.Sequence)
            .Select(x => x.Section)
            .ToList();

        return new FrameworkQuestionnaireDto(input.FrameworkId, input.FrameworkName, sections);
    }

    private static QuestionnaireQuestionDto MapQuestion(
        ComplianceNode objectiveElement,
        ComplianceNode standardNode,
        ComplianceNode chapterNode)
    {
        var questionText = BuildQuestionText(objectiveElement, standardNode);
        var helpText = BuildHelpText(objectiveElement, standardNode);
        var category = standardNode.RequirementDetail?.Classification;
        if (string.IsNullOrWhiteSpace(category))
            category = chapterNode.Code;

        var weight = standardNode.RequirementScore?.Weight ?? DefaultWeight;

        return new QuestionnaireQuestionDto(
            objectiveElement.Id,
            objectiveElement.Id,
            questionText,
            helpText,
            category,
            weight);
    }

    private static string BuildQuestionText(ComplianceNode objectiveElement, ComplianceNode standardNode)
    {
        var title = objectiveElement.Title.Trim();
        var intent = standardNode.RequirementDetail?.Intent?.Trim();
        if (string.IsNullOrWhiteSpace(intent))
            return title;

        return string.IsNullOrWhiteSpace(title) ? intent : $"{title} — {intent}";
    }

    private static string BuildHelpText(ComplianceNode objectiveElement, ComplianceNode standardNode)
    {
        var parts = new List<string>();

        var howToComply = objectiveElement.KnowledgeItems
            .Where(k => k.IsActive)
            .OrderByDescending(k => k.Version)
            .Select(k => k.HowToComply?.Trim())
            .FirstOrDefault(s => !string.IsNullOrWhiteSpace(s));

        if (!string.IsNullOrWhiteSpace(howToComply))
            parts.Add(howToComply!);

        var measurableElements = standardNode.RequirementDetail?.MeasurableElements?.Trim();
        if (!string.IsNullOrWhiteSpace(measurableElements))
            parts.Add($"Requirement details: {measurableElements}");

        return string.Join("\n\n", parts);
    }

    private static Guid? FindAncestorOfType(
        Guid nodeId,
        IReadOnlyDictionary<Guid, ComplianceNode> nodeById,
        string ancestorType)
    {
        var current = nodeId;
        while (nodeById.TryGetValue(current, out var node))
        {
            if (string.Equals(node.NodeType, ancestorType, StringComparison.OrdinalIgnoreCase))
                return node.Id;

            if (node.ParentId is null)
                return null;

            current = node.ParentId.Value;
        }

        return null;
    }
}
