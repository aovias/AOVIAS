using ComplianceService.Domain;

namespace ComplianceService.Application.Contracts;

/// <summary>
/// Interface for routing user questions to appropriate copilot question types.
/// Uses simple, deterministic rule-based mapping without ML/NLP or AI calls.
/// </summary>
public interface ICopilotIntentRouter
{
    /// <summary>
    /// Routes a user question to a copilot question type and extracts entity identifiers.
    /// </summary>
    /// <param name="userQuestion">The user's question text.</param>
    /// <param name="programId">Optional program identifier.</param>
    /// <param name="controlId">Optional control identifier.</param>
    /// <param name="taskId">Optional task identifier.</param>
    /// <param name="auditId">Optional audit identifier.</param>
    /// <returns>A CopilotIntent containing the question type, target entity type, and extracted entity identifiers.</returns>
    CopilotIntent Route(
        string userQuestion,
        Guid? programId = null,
        Guid? controlId = null,
        Guid? taskId = null,
        Guid? auditId = null);
}
