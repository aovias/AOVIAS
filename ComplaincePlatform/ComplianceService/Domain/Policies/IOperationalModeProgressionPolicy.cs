using Shared.Infrastructure.Domain.Enums;

namespace ComplianceService.Domain.Policies;

/// <summary>
/// Rules for changing <see cref="OperationalMode"/> on a journey (independent of compliance lifecycle state machine).
/// </summary>
public interface IOperationalModeProgressionPolicy
{
    /// <summary>
    /// Returns whether the journey may change operational mode (e.g. locked / read-only).
    /// </summary>
    bool CanChangeOperationalMode(global::ComplianceService.Domain.ComplianceJourney journey, out string? denyReason);

    /// <summary>
    /// Whether workspace/OE edits are read-only for API consumers (UI flags).
    /// </summary>
    bool IsWorkspaceReadOnly(global::ComplianceService.Domain.ComplianceJourney journey, OperationalMode mode);

    /// <summary>
    /// Snapshot/catalog treated as frozen (additional read-only flag for session).
    /// </summary>
    bool IsSnapshotReadOnly(global::ComplianceService.Domain.ComplianceJourney journey);
}
