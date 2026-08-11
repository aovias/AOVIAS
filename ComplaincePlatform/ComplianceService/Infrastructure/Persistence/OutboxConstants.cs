namespace ComplianceService.Infrastructure.Persistence;

/// <summary>
/// Shared outbox retry ceilings for projection/relay processors and poison admin APIs.
/// </summary>
public static class OutboxConstants
{
    /// <summary>Messages at or above this retry count are considered poison and skipped until replay.</summary>
    public const int MaxRetries = 5;
}
