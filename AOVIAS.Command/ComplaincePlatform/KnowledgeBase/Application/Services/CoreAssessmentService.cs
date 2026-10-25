using KnowledgeBase.Application.Contracts;
using KnowledgeBase.Application.DTOs;

namespace KnowledgeBase.Application.Services;

/// <summary>
/// Assembles the risk-selected Core maturity question list for a framework.
/// </summary>
public sealed class CoreAssessmentService
{
    private readonly IFrameworkRepository _frameworks;
    private readonly IWizardQuestionRepository _wizardQuestions;

    public CoreAssessmentService(
        IFrameworkRepository frameworks,
        IWizardQuestionRepository wizardQuestions)
    {
        _frameworks = frameworks;
        _wizardQuestions = wizardQuestions;
    }

    public async Task<CoreAssessmentDto?> GetCoreQuestionsAsync(
        Guid frameworkId,
        int count = 40,
        CancellationToken ct = default)
    {
        var framework = await _frameworks.GetByIdAsync(frameworkId, ct);
        if (framework is null)
            return null;

        var questions = await _wizardQuestions.GetCoreAssessmentQuestionsAsync(frameworkId, count, ct);

        var questionDtos = questions
            .Select(q => new CoreAssessmentQuestionDto(
                WizardQuestionId:    q.Id,
                ObjectiveElementId:  q.ObjectiveElementId,
                QuestionTitle:       q.QuestionTitle,
                QuestionText:        q.QuestionText,
                HelpText:            q.HelpText,
                AssessmentGuidance:  q.AssessmentGuidance,
                ExpectedEvidence:    q.ExpectedEvidence,
                Category:            q.Category,
                Weight:              q.Weight,
                IsMandatory:         q.IsMandatory,
                IsCritical:          q.IsCritical,
                DisplayPriority:     q.DisplayPriority))
            .ToList();

        return new CoreAssessmentDto(
            FrameworkId:   framework.Id,
            FrameworkName: framework.Name,
            MaturityLevel: "Core",
            Questions:     questionDtos);
    }
}
