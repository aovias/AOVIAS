namespace ComplianceServiceManager.Models;

public enum FailureCategory
{
    Unknown,
    BuildFailure,
    RuntimeException,
    PortConflict,
    MissingAppSettings,
    MissingSecrets,
    EfMigrationIssue,
    DependencyInjectionFailure,
    NuGetRestoreIssue,
    DockerFailure,
    HttpsCertificateIssue,
    MissingEnvironmentVariable,
    ConfigurationError,
    HealthCheckFailure,
    KestrelBindingFailure,
    SqlConnectivityIssue,
    CosmosConnectivityIssue
}
