using ComplianceService.Domain;

namespace ComplianceService.Application.Contracts;

/// <summary>
/// Assembles compliance context by loading relevant data from persistence
/// and validating relationships. Fails safely if data is incomplete.
/// No AI calls, no inference or guessing - pure data loading and validation.
/// </summary>
public interface IComplianceContextAssembler
{
    /// <summary>
    /// Assembles compliance context based on CopilotIntent.
    /// Loads relevant entities and validates relationships.
    /// </summary>
    /// <param name="intent">The copilot intent containing entity identifiers to load.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>ComplianceContext with loaded data. Missing optional data is marked as incomplete with clear reasons.</returns>
    Task<ComplianceContext> AssembleAsync(CopilotIntent intent, CancellationToken ct = default);
}
