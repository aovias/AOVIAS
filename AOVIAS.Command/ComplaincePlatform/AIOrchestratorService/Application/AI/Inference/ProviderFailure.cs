namespace AIOrchestratorService.Application.AI.Inference;

public enum ProviderFailureReason
{
    Unknown = 0,
    InvalidJson = 1,
    Timeout = 2,
    RateLimited = 3,
    ServerError = 4,
    TransportError = 5
}

public sealed class ProviderFailureException : Exception
{
    public ProviderFailureException(
        string message,
        ProviderFailureReason reason,
        int? statusCode = null,
        bool retryable = false,
        Exception? innerException = null) : base(message, innerException)
    {
        Reason = reason;
        StatusCode = statusCode;
        Retryable = retryable;
    }

    public ProviderFailureReason Reason { get; }
    public int? StatusCode { get; }
    public bool Retryable { get; }
}
