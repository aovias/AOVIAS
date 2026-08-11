namespace Shared.Logging;

/// <summary>
/// Stable, well-known names used across every PRAMAN service so that logs are
/// uniformly structured and machine-parseable by the Compliance Service Manager
/// and AI diagnostics.
/// </summary>
public static class LoggingConstants
{
    /// <summary>HTTP header carrying the end-to-end correlation id.</summary>
    public const string CorrelationIdHeader = "X-Correlation-ID";

    /// <summary>Key used to stash the correlation id on <c>HttpContext.Items</c>.</summary>
    public const string CorrelationItemKey = "Praman.CorrelationId";

    /// <summary>Configuration key for the log file root directory.</summary>
    public const string FileRootConfigKey = "Logging:FileRoot";

    /// <summary>Environment variable fallback for the log file root directory.</summary>
    public const string FileRootEnvVar = "COMPLIANCE_LOG_ROOT";

    /// <summary>Structured log property names (enriched on every event).</summary>
    public static class Properties
    {
        public const string Service = "Service";
        public const string Environment = "Environment";
        public const string Machine = "Machine";
        public const string CorrelationId = "CorrelationId";
        public const string TraceId = "TraceId";
        public const string SpanId = "SpanId";
        public const string RequestId = "RequestId";
        public const string UserId = "UserId";
        public const string OrganizationId = "OrganizationId";
        public const string HospitalId = "HospitalId";
        public const string EventCode = "EventCode";
        public const string Category = "Category";
        public const string LogType = "LogType";
        public const string StatusCode = "StatusCode";
        public const string Elapsed = "Elapsed";
        public const string ExceptionType = "ExceptionType";
        public const string IsTransient = "IsTransient";
        public const string RemediationHint = "RemediationHint";
    }

    /// <summary>High-level categories used to group events for triage.</summary>
    public static class Categories
    {
        public const string Lifecycle = "Lifecycle";
        public const string Request = "Request";
        public const string Security = "Security";
        public const string Database = "Database";
        public const string Dependency = "Dependency";
        public const string Workflow = "Workflow";
        public const string Job = "Job";
        public const string Configuration = "Configuration";
        public const string Audit = "Audit";
    }

    /// <summary>Distinguishes the audit event stream from operational logs.</summary>
    public static class LogTypes
    {
        public const string Operational = "Operational";
        public const string Audit = "Audit";
    }
}

/// <summary>
/// Deterministic event codes. Each value maps 1:1 to a Compliance Service Manager
/// <c>FailureCategory</c> so AI diagnostics can classify without fragile string matching.
/// </summary>
public static class EventCode
{
    // Lifecycle
    public const string ApplicationStarted = "APPLICATION_STARTED";
    public const string ApplicationStopped = "APPLICATION_STOPPED";
    public const string StartupFailed = "STARTUP_FAILED";

    // Configuration / dependencies
    public const string ConfigMissing = "CONFIG_MISSING";
    public const string DiResolutionFailed = "DI_RESOLUTION_FAILED";
    public const string ExternalDependencyFailed = "EXTERNAL_DEP_FAILED";

    // Database
    public const string MigrationFailed = "MIGRATION_FAILED";
    public const string DbConnectionFailed = "DB_CONNECTION_FAILED";
    public const string DbCommandFailed = "DB_COMMAND_FAILED";
    public const string SlowQuery = "SLOW_QUERY";

    // Requests
    public const string ApiRequest = "API_REQUEST";
    public const string ApiResponse = "API_RESPONSE";

    // Security / audit
    public const string UserLogin = "USER_LOGIN";
    public const string UserLogout = "USER_LOGOUT";
    public const string AuthFailed = "AUTH_FAILED";
    public const string TokenIssued = "TOKEN_ISSUED";
    public const string TokenRefreshed = "TOKEN_REFRESHED";
    public const string PasswordChanged = "PASSWORD_CHANGED";
    public const string PasswordResetRequested = "PASSWORD_RESET_REQUESTED";
    public const string RoleChanged = "ROLE_CHANGED";
    public const string UserRegistered = "USER_REGISTERED";
    public const string EmailVerificationRequested = "EMAIL_VERIFICATION_REQUESTED";
    public const string EmailVerified = "EMAIL_VERIFIED";
    public const string EmailVerificationFailed = "EMAIL_VERIFICATION_FAILED";
    public const string UserUpdated = "USER_UPDATED";
    public const string UserSuspended = "USER_SUSPENDED";
    public const string UserActivated = "USER_ACTIVATED";
    public const string UserDisabled = "USER_DISABLED";
    public const string UserDeleted = "USER_DELETED";
    public const string UserUnlocked = "USER_UNLOCKED";
    public const string UserPasswordReset = "USER_PASSWORD_RESET";
    public const string UserForcePasswordChange = "USER_FORCE_PASSWORD_CHANGE";

    // Workflow / jobs
    public const string WorkflowStarted = "WORKFLOW_STARTED";
    public const string WorkflowCompleted = "WORKFLOW_COMPLETED";
    public const string JobStarted = "JOB_STARTED";
    public const string JobCompleted = "JOB_COMPLETED";
    public const string JobFailed = "JOB_FAILED";

    // Generic
    public const string UnhandledException = "UNHANDLED_EXCEPTION";
    public const string HealthCheckFailed = "HEALTH_CHECK_FAILED";
}
