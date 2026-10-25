namespace Shared.Infrastructure.Execution;

/// <summary>
/// Wire contract for propagating <see cref="PlatformExecutionContext"/> between services.
///
/// <para>
/// These headers are written **only** by the Gateway (from validated token claims) or by a
/// background host that constructed its context explicitly. They are stripped from inbound
/// public traffic at the edge — otherwise a client could assert its own tenant simply by setting
/// a header, which is the vulnerability this contract exists to close.
/// </para>
/// </summary>
public static class ExecutionContextHeaders
{
    public const string OrganizationId = "X-Aovias-Organization-Id";
    public const string HospitalId = "X-Aovias-Hospital-Id";
    public const string JourneyId = "X-Aovias-Journey-Id";
    public const string FrameworkId = "X-Aovias-Framework-Id";
    public const string UserId = "X-Aovias-User-Id";
    public const string ExecutionMode = "X-Aovias-Execution-Mode";
    public const string CorrelationId = "X-Correlation-Id";
    public const string RequestId = "X-Aovias-Request-Id";
    public const string Source = "X-Aovias-Source";
    public const string Caller = "X-Aovias-Caller";

    /// <summary>
    /// Every header this contract owns. Used at the edge to strip client-supplied values before
    /// the Gateway writes its own.
    /// </summary>
    public static readonly IReadOnlyList<string> All = new[]
    {
        OrganizationId, HospitalId, JourneyId, FrameworkId,
        UserId, ExecutionMode, RequestId, Source, Caller,
        // CorrelationId is intentionally absent: a client may legitimately supply one to correlate
        // its own traces, and it carries no authority.
    };
}
