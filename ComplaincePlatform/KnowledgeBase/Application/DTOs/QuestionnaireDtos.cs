namespace KnowledgeBase.Application.DTOs;

public sealed record QuestionnaireQuestionDto(
    Guid Id,
    Guid ComplianceNodeId,
    string QuestionText,
    string HelpText,
    string Category,
    decimal Weight);

public sealed record QuestionnaireSectionDto(
    Guid ChapterId,
    string ChapterName,
    IReadOnlyList<QuestionnaireQuestionDto> Questions);

public sealed record FrameworkQuestionnaireDto(
    Guid FrameworkId,
    string FrameworkName,
    IReadOnlyList<QuestionnaireSectionDto> Sections);
