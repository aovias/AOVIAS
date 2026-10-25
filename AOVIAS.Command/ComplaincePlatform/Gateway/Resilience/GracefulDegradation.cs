namespace Gateway.Resilience;

/// <summary>
/// Phase 7.5 — pattern for optional / non-critical downstream dependencies.
/// Catch transient failures, log a warning, and return a safe fallback instead of failing the request.
/// Use for enrichment reads (dashboard widgets, analytics overlays, AI suggestions).
/// Do not use for auth, writes, lifecycle transitions, or entitlement checks.
/// </summary>
public static class GracefulDegradation
{
    /// <summary>
    /// Executes <paramref name="action"/>; on failure (except cancellation) returns <paramref name="fallback"/>.
    /// </summary>
    public static async Task<T> ExecuteAsync<T>(
        Func<CancellationToken, Task<T>> action,
        T fallback,
        ILogger logger,
        string operationName,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(logger);

        try
        {
            return await action(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(
                ex,
                "Graceful degradation for {Operation}; returning fallback",
                operationName);
            return fallback;
        }
    }

    /// <summary>
    /// Synchronous variant for already-materialized fallbacks after a failed probe.
    /// </summary>
    public static T FromFallback<T>(T fallback, ILogger logger, string operationName, Exception? cause = null)
    {
        ArgumentNullException.ThrowIfNull(logger);

        if (cause is null)
        {
            logger.LogWarning("Graceful degradation for {Operation}; returning fallback", operationName);
        }
        else
        {
            logger.LogWarning(cause, "Graceful degradation for {Operation}; returning fallback", operationName);
        }

        return fallback;
    }
}
