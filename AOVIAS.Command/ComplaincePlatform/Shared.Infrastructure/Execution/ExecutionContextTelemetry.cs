using Microsoft.Extensions.Logging;

namespace Shared.Infrastructure.Execution;

/// <summary>
/// Structured telemetry for tenant-context events.
///
/// <para>
/// **Why this is a prerequisite, not a nicety.** Once query filters are mandatory, a request that
/// fails to establish a tenant returns *no rows* rather than every tenant's rows. That is the right
/// failure mode, but it is silent: the user sees an empty screen and the service reports success.
/// Without <see cref="FailClosed"/> logging, the first report of a missing propagation path comes
/// from a customer rather than from a dashboard.
/// </para>
///
/// <para>
/// Every method uses structured properties (not interpolated strings) so a log pipeline can filter
/// and alert on <c>OrganizationId</c>, <c>CorrelationId</c> and <c>Event</c> directly.
/// </para>
/// </summary>
public static class ExecutionContextTelemetry
{
    public const string EventFailClosed = "ExecutionContext.FailClosed";
    public const string EventMissing = "ExecutionContext.Missing";
    public const string EventCrossTenantRejected = "ExecutionContext.CrossTenantRejected";
    public const string EventPropagationFailure = "ExecutionContext.PropagationFailure";
    public const string EventFilterBypass = "ExecutionContext.FilterBypass";

    /// <summary>
    /// A query executed with no tenant scope and therefore returned nothing.
    ///
    /// Logged at Warning: this is not an error for the request, but it is almost always a bug in
    /// propagation, and it is the single most useful signal when rolling
    /// <c>Tenancy:MandatoryQueryFilters</c> out to a new environment.
    /// </summary>
    public static void FailClosed(ILogger logger, string service, string operation, string? correlationId) =>
        logger.LogWarning(
            "{Event} service={Service} operation={Operation} correlationId={CorrelationId} — " +
            "no tenant scope resolved; query returned no rows. Check execution-context propagation for this path.",
            EventFailClosed, service, operation, correlationId ?? "(none)");

    /// <summary>An internal request arrived without execution-context headers.</summary>
    public static void MissingContext(ILogger logger, string service, string path, string? correlationId) =>
        logger.LogWarning(
            "{Event} service={Service} path={Path} correlationId={CorrelationId} — " +
            "no execution context on an internal request.",
            EventMissing, service, path, correlationId ?? "(none)");

    /// <summary>
    /// A caller attempted to act on an organization other than its own. A security event: logged
    /// with both ids so the attempt is attributable.
    /// </summary>
    public static void CrossTenantRejected(
        ILogger logger, string path, Guid authenticatedOrganizationId, Guid requestedOrganizationId, string? caller) =>
        logger.LogWarning(
            "{Event} path={Path} authenticatedOrganizationId={AuthenticatedOrganizationId} " +
            "requestedOrganizationId={RequestedOrganizationId} caller={Caller} — rejected.",
            EventCrossTenantRejected, path, authenticatedOrganizationId, requestedOrganizationId, caller ?? "(unknown)");

    /// <summary>The Gateway could not build a context to propagate (no organization claim).</summary>
    public static void PropagationFailure(ILogger logger, string targetService, string endpoint, string reason) =>
        logger.LogWarning(
            "{Event} targetService={TargetService} endpoint={Endpoint} reason={Reason} — " +
            "downstream call will run without tenant scope.",
            EventPropagationFailure, targetService, endpoint, reason);

    /// <summary>
    /// A deliberate <c>IgnoreQueryFilters()</c> bypass ran. Logged at Information so the remaining
    /// bypasses can be inventoried from production traffic rather than from static analysis —
    /// static analysis over-counted them by a wide margin in Wave 2.2A.
    /// </summary>
    public static void FilterBypass(ILogger logger, string service, string operation, string justification) =>
        logger.LogInformation(
            "{Event} service={Service} operation={Operation} justification={Justification}",
            EventFilterBypass, service, operation, justification);

    /// <summary>
    /// One line summarising the context a request executed under. Emitted at Debug so it can be
    /// enabled per-environment for cross-service troubleshooting without cost in normal operation.
    /// </summary>
    public static void RequestExecuted(
        ILogger logger, PlatformExecutionContext context, string service, string endpoint, long elapsedMs) =>
        logger.LogDebug(
            "ExecutionContext.Request service={Service} endpoint={Endpoint} mode={Mode} " +
            "organizationId={OrganizationId} hospitalId={HospitalId} journeyId={JourneyId} " +
            "frameworkId={FrameworkId} userId={UserId} correlationId={CorrelationId} " +
            "requestId={RequestId} elapsedMs={ElapsedMs}",
            service, endpoint, context.Mode, context.OrganizationId, context.HospitalId,
            context.JourneyId, context.FrameworkId, context.UserId,
            context.CorrelationId, context.RequestId, elapsedMs);
}
