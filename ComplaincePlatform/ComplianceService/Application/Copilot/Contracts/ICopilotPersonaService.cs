using ComplianceService.Application.Copilot.DTOs;
using ComplianceService.Domain.Copilot;

namespace ComplianceService.Application.Copilot.Contracts;

/// <summary>
/// Manages active Copilot persona resolution and user overrides.
/// </summary>
public interface ICopilotPersonaService
{
    /// <summary>Resolves the active persona for the given context using the router's decision hierarchy.</summary>
    CopilotPersona GetActivePersona(CopilotContext context);

    /// <summary>Applies a manual persona override for the current session context.</summary>
    CopilotPersona OverridePersona(CopilotContext context, CopilotPersona override_);

    /// <summary>Resets manual override; returns to automatic routing result.</summary>
    CopilotPersona ResetPersona(CopilotContext context);

    /// <summary>Returns the full catalog of available personas with display metadata.</summary>
    IReadOnlyList<PersonaResponseDto> GetPersonaCatalog();
}
