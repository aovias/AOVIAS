namespace Gateway.DTOs;

public sealed record OnboardingQuestionnaireQuestionDto(
    Guid Id,
    Guid ComplianceNodeId,
    string QuestionText,
    string HelpText,
    string Category,
    decimal Weight);

public sealed record OnboardingQuestionnaireSectionDto(
    Guid ChapterId,
    string ChapterName,
    IReadOnlyList<OnboardingQuestionnaireQuestionDto> Questions);

public sealed record FrameworkQuestionnaireDto(
    Guid FrameworkId,
    string FrameworkName,
    IReadOnlyList<OnboardingQuestionnaireSectionDto> Sections);
