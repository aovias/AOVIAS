namespace ComplianceService.Domain.Copilot;

/// <summary>
/// Represents the operating persona of the Compliance Copilot.
/// A single Copilot with multiple mode configurations — not multiple copilots.
/// </summary>
public enum CopilotPersona
{
    AccreditationCoach       = 0,
    QualityManagerAssistant  = 1,
    EvidenceAdvisor          = 2,
    CapaAdvisor              = 3,
    ExecutiveAdvisor         = 4,
    DepartmentHeadAssistant  = 5,
    AuditorAssistant         = 6,
}
