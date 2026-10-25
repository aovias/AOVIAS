using ComplianceService.Application.Copilot.DTOs;

namespace ComplianceService.Application.Copilot.Models;

/// <summary>
/// Request model for POST /copilot/recommendations/refresh.
/// Triggers deterministic rule-engine re-evaluation for the caller's organization.
/// No writes to operational domains — recommendations only.
/// </summary>
public sealed class RefreshRecommendationsRequest
{
    /// <summary>Restrict refresh to a specific accreditation framework. If null, the active framework is resolved.</summary>
    public Guid? FrameworkId { get; init; }

    /// <summary>Optional: restrict refresh to rules relevant to a specific persona.</summary>
    public string? Persona { get; init; }

    /// <summary>Optional: frontend navigation context for deeper persona routing.</summary>
    public CopilotContextDto? Context { get; init; }
}
