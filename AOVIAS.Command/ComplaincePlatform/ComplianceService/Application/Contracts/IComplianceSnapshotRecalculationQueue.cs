namespace ComplianceService.Application.Contracts;

/// <summary>
/// Fire-and-forget queue for snapshot recalculation (does not block HTTP).
/// </summary>
public interface IComplianceSnapshotRecalculationQueue
{
    /// <summary>
    /// Schedules recalculation for the hospital+framework pair (typically matches one compliance journey).
    /// </summary>
    void EnqueueHospitalFramework(Guid hospitalId, Guid frameworkId);

    /// <summary>
    /// Schedules a Compliance Maturity Index (CMI) recalculation after OE completion.
    /// Triggered post-wizard to keep the Compliance Maturity Dashboard current.
    /// </summary>
    void EnqueueMaturityRecalc(Guid hospitalId, Guid organizationId);
}
