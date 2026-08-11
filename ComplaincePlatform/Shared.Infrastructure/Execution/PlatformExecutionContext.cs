namespace Shared.Infrastructure.Execution;

/// <summary>
/// How the current unit of work was initiated. Determines which identifiers are expected and how
/// the tenant was established.
/// </summary>
public enum ExecutionMode
{
    /// <summary>An authenticated end-user request. Tenant comes from the token.</summary>
    User = 0,

    /// <summary>Service-to-service call. Tenant comes from the propagated execution context.</summary>
    InternalApi = 1,

    /// <summary>Recurring or queued background work. Tenant is chosen explicitly per iteration.</summary>
    BackgroundJob = 2,

    /// <summary>Cron-driven work. Same rules as <see cref="BackgroundJob"/>.</summary>
    ScheduledTask = 3,

    /// <summary>Real-time hub connection. Tenant is fixed at connection time.</summary>
    SignalR = 4,

    /// <summary>Platform-internal maintenance (migrations, seeding). Cross-tenant by nature.</summary>
    System = 5,
}

/// <summary>
/// The authoritative tenant and correlation scope for one unit of work.
///
/// <para>
/// **Why this exists.** Tenant scope used to be inferred — from the authenticated user in some
/// services, from a client-supplied query parameter in others, and from an EF global query filter
/// that registered itself only when a user happened to be present. That last behaviour meant
/// internal API-key calls ran with no filter at all: isolation silently disabled itself exactly
/// where it was least observable. See ADR-002.
/// </para>
///
/// <para>
/// **Rules.** <see cref="OrganizationId"/> is mandatory and is the tenant boundary; it is never
/// defaulted, never inferred from another identifier, and never taken from client input outside
/// explicitly privileged cross-tenant endpoints. The context is immutable for the lifetime of the
/// request — services read it, they do not reshape it.
/// </para>
///
/// <para>
/// Named <c>PlatformExecutionContext</c> rather than <c>ExecutionContext</c> to avoid colliding
/// with <see cref="System.Threading.ExecutionContext"/> at every call site.
/// </para>
/// </summary>
public sealed record PlatformExecutionContext
{
    /// <summary>The tenant boundary. Mandatory — <see cref="IsValid"/> is false without it.</summary>
    public required Guid OrganizationId { get; init; }

    /// <summary>Scopes within a tenant. Not a tenant boundary in its own right.</summary>
    public Guid? HospitalId { get; init; }

    /// <summary>A hospital's enrollment in a framework. Distinct from <see cref="FrameworkId"/>.</summary>
    public Guid? JourneyId { get; init; }

    /// <summary>The accreditation framework. Distinct from <see cref="JourneyId"/>.</summary>
    public Guid? FrameworkId { get; init; }

    /// <summary>Null for <see cref="ExecutionMode.BackgroundJob"/> and <see cref="ExecutionMode.System"/>.</summary>
    public Guid? UserId { get; init; }

    public required ExecutionMode Mode { get; init; }

    /// <summary>Stable across every hop of one causal chain.</summary>
    public required string CorrelationId { get; init; }

    /// <summary>Unique to this hop.</summary>
    public required string RequestId { get; init; }

    /// <summary>Originating service, e.g. <c>Gateway</c>.</summary>
    public string Source { get; init; } = string.Empty;

    /// <summary>Originating principal or job name, for audit trails.</summary>
    public string Caller { get; init; } = string.Empty;

    /// <summary>
    /// True when a tenant boundary is established. Callers must fail closed when this is false —
    /// returning no data is a visible bug; returning every tenant's data is a breach.
    /// </summary>
    public bool IsValid => OrganizationId != Guid.Empty;

    /// <summary>
    /// Cross-tenant execution, permitted only for platform maintenance. Distinguished from an
    /// *invalid* context so the two are never conflated at a call site.
    /// </summary>
    public bool IsCrossTenant => Mode == ExecutionMode.System;

    /// <summary>
    /// Context for background/scheduled work against a known tenant. Explicit by construction —
    /// there is no ambient state for a job to fall back on.
    /// </summary>
    public static PlatformExecutionContext ForBackgroundJob(
        Guid organizationId,
        string jobName,
        Guid? hospitalId = null,
        string? correlationId = null) => new()
        {
            OrganizationId = organizationId,
            HospitalId = hospitalId,
            Mode = ExecutionMode.BackgroundJob,
            CorrelationId = correlationId ?? Guid.NewGuid().ToString("N"),
            RequestId = Guid.NewGuid().ToString("N"),
            Source = "BackgroundJob",
            Caller = jobName,
        };

    /// <summary>
    /// Cross-tenant maintenance context (migrations, seeding). Deliberately verbose to call, and
    /// every use should be justified in review.
    /// </summary>
    public static PlatformExecutionContext ForSystem(string reason) => new()
    {
        OrganizationId = Guid.Empty,
        Mode = ExecutionMode.System,
        CorrelationId = Guid.NewGuid().ToString("N"),
        RequestId = Guid.NewGuid().ToString("N"),
        Source = "System",
        Caller = reason,
    };
}
