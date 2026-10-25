using Aovias.Release.Contracts.Enums;

namespace Aovias.ReleaseManager.Services;

/// <summary>
/// Authoritative release-state predicates for the release lifecycle state machine.
/// </summary>
internal static class ReleaseStatePolicy
{
    /// <summary>
    /// States that have already passed package validation and compatibility promotion.
    /// Excludes <see cref="ReleaseState.Failed"/> and terminal rollback states that must re-validate.
    /// </summary>
    public static bool HasCompletedPackageValidation(ReleaseState state)
        => state is ReleaseState.Compatible
            or ReleaseState.Staged
            or ReleaseState.Deploying
            or ReleaseState.HealthCheck
            or ReleaseState.Activated
            or ReleaseState.Completed;

    /// <summary>States allowed to enter deployment planning.</summary>
    public static bool IsReadyForPlanning(ReleaseState state)
        => state == ReleaseState.Compatible;
}
