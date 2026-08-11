namespace ComplianceServiceManager.Operations;

/// <summary>
/// The only thing an operation implements: its static <see cref="Descriptor"/> and how it runs
/// against an <see cref="OperationContext"/>. Bootstrap, Publish, Deploy, Rollback, Repair, Upgrade,
/// and Reconcile are all, eventually, one of these — the engine itself knows nothing about any of
/// them.
///
/// Contract: <see cref="ExecuteAsync"/> must drive every stage in <see cref="Descriptor"/>'s stage
/// list to a terminal per-stage status (<see cref="OperationStageStatus.Completed"/>,
/// <see cref="OperationStageStatus.Warning"/>, <see cref="OperationStageStatus.Skipped"/>, or
/// <see cref="OperationStageStatus.Failed"/>) in order, stopping at the first failure — mirroring
/// exactly how <c>BootstrapOrchestrator</c>'s own phase loop already behaves. Throwing instead of
/// calling <see cref="OperationContext.FailStage"/> is tolerated (the engine catches it and fails
/// whichever stage was left <see cref="OperationStageStatus.Running"/>, or the operation as a whole
/// if none was) but is a safety net, not the primary failure-reporting path.
/// </summary>
public interface IOperationExecutor
{
    OperationDescriptor Descriptor { get; }

    Task ExecuteAsync(OperationContext context);
}
