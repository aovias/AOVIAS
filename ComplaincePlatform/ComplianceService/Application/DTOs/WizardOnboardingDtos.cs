namespace ComplianceService.Application.DTOs;

public sealed class WizardAssessmentAnswerItemDto
{
    public string QuestionId { get; set; } = string.Empty;
    public string Question { get; set; } = string.Empty;
    public string Answer { get; set; } = string.Empty;

    /// <summary>
    /// OE Code (e.g. "AAC.1.a") from the KB ComplianceNode.
    /// Populated by the frontend from CoreAssessmentQuestionDto.QuestionTitle prefix.
    /// Null when the answer was saved before this field was added (legacy data).
    /// When non-null, enables direct code-based obligation lookup and avoids a
    /// runtime KB HTTP call for resolution.
    /// </summary>
    public string? ObjectiveElementCode { get; set; }
}

public sealed class SaveWizardAssessmentRequestDto
{
    public Guid OrganizationId { get; set; }
    public Guid HospitalId { get; set; }
    public int WizardVersion { get; set; }
    public List<WizardAssessmentAnswerItemDto> AssessmentAnswers { get; set; } = new();
}

public sealed class WizardOnboardingAssessmentResponseDto
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid HospitalId { get; set; }
    public int WizardVersion { get; set; }
    public IReadOnlyList<WizardAssessmentAnswerItemDto> AssessmentAnswers { get; set; } = Array.Empty<WizardAssessmentAnswerItemDto>();
    public DateTime CreatedAt { get; set; }
}
