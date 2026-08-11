using ComplianceService.Domain.Copilot;

namespace ComplianceService.Application.Copilot.Contracts;

/// <summary>
/// Determines the active Copilot persona from the current session context.
/// Rule-based routing — no LLM required.
/// </summary>
public interface ICopilotPersonaRouter
{
    /// <summary>
    /// Evaluates the decision hierarchy:
    ///   1. ManualOverride (from context.PersonaOverride)
    ///   2. Page context mapping
    ///   3. Entity type / intent detection
    ///   4. Default: AccreditationCoach
    /// </summary>
    CopilotPersona DeterminePersona(CopilotContext context);
}
