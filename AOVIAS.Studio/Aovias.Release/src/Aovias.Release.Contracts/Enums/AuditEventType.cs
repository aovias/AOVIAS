namespace Aovias.Release.Contracts.Enums;

/// <summary>
/// Immutable audit event types for release operations.
/// </summary>
public enum AuditEventType
{
    PackageUploaded = 0,
    PackageValidated = 1,
    DeploymentPlanned = 2,
    DeploymentStarted = 3,
    DeploymentCompleted = 4,
    RollbackStarted = 5,
    RollbackCompleted = 6,
    ValidationPassed = 7,
    PipelineStepStarted = 8,
    PipelineStepCompleted = 9,
    PipelineStepFailed = 10
}
