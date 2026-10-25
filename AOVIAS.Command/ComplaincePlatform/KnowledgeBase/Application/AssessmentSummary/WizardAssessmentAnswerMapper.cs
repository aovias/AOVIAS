using KnowledgeBase.Application.DTOs;

namespace KnowledgeBase.Application.AssessmentSummary;

public static class WizardAssessmentAnswerMapper
{
    public static UpdateAssessmentItemRequestDto MapToUpdateItemDto(WizardAssessmentAnswerDto answer)
    {
        var normalized = answer.Answer?.Trim().ToLowerInvariant() ?? string.Empty;
        var remarks = answer.Question?.Trim() ?? string.Empty;

        return normalized switch
        {
            "yes" => new UpdateAssessmentItemRequestDto(5, "Met", remarks, false, true),
            "no" => new UpdateAssessmentItemRequestDto(1, "NotMet", remarks, true, false),
            "not_sure" => new UpdateAssessmentItemRequestDto(3, "Partial", remarks, true, false),
            _ => new UpdateAssessmentItemRequestDto(3, "Partial", remarks, true, false)
        };
    }
}
