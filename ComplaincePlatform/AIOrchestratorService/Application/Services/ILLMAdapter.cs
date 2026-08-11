using AIOrchestratorService.Application.Services;

namespace AIOrchestratorService.Application.Services;

/// <summary>
/// Adapter interface for LLM providers (Claude, Azure OpenAI, etc.).
/// This adapter is replaceable and handles only the LLM communication.
/// No business logic or persistence.
/// </summary>
public interface ILLMAdapter
{
    /// <summary>
    /// Generates draft text from a drafting request.
    /// </summary>
    /// <param name="request">The structured drafting request containing all context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing the generated draft text or error information.</returns>
    Task<LLMResult> GenerateDraftAsync(DraftingRequest request, CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of an LLM generation operation.
/// </summary>
public class LLMResult
{
    /// <summary>
    /// Indicates whether the operation was successful.
    /// </summary>
    public bool IsSuccess { get; init; }

    /// <summary>
    /// The generated draft text (only present if IsSuccess is true).
    /// </summary>
    public string? DraftText { get; init; }

    /// <summary>
    /// Error message (only present if IsSuccess is false).
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Exception that occurred (if any).
    /// </summary>
    public Exception? Exception { get; init; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static LLMResult Success(string draftText)
    {
        return new LLMResult
        {
            IsSuccess = true,
            DraftText = draftText
        };
    }

    /// <summary>
    /// Creates a failure result.
    /// </summary>
    public static LLMResult Failure(string errorMessage, Exception? exception = null)
    {
        return new LLMResult
        {
            IsSuccess = false,
            ErrorMessage = errorMessage,
            Exception = exception
        };
    }
}
