using ComplianceService.Domain;

namespace ComplianceService.Application.Contracts;

/// <summary>
/// Validates Copilot responses for safety compliance.
/// Ensures responses do not imply approval, predict outcomes, or claim authority.
/// </summary>
public interface ICopilotSafetyValidator
{
    /// <summary>
    /// Validates a Copilot response for safety compliance.
    /// </summary>
    /// <param name="response">The response to validate.</param>
    /// <returns>Safety validation result with violations and warnings if any.</returns>
    CopilotSafetyResult Validate(CopilotResponse response);
}
