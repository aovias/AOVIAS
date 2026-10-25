namespace ComplianceServiceManager.Services;

/// <summary>
/// Tracks which service errors have already been addressed by an AI fix, keyed by a stable
/// error fingerprint. Lets the orchestrator distinguish errors that still need fixing from
/// errors that were resolved in a previous session, and remember when a service was last fixed.
/// </summary>
public interface IFixedErrorStore
{
    /// <summary>UTC timestamp of the last successful fix for a service, if any.</summary>
    DateTimeOffset? GetLastFixedAt(string serviceName);

    /// <summary>Fingerprints of errors already resolved for a service.</summary>
    IReadOnlyCollection<string> GetResolvedFingerprints(string serviceName);

    /// <summary>Records that the supplied error fingerprints were resolved at the given time.</summary>
    void MarkResolved(string serviceName, IEnumerable<string> fingerprints, DateTimeOffset fixedAt);
}
