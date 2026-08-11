namespace Aovias.Deployment.Contracts.Reconciliation;

/// <summary>The result of one <see cref="ReconciliationSafetyGate.Evaluate"/> call (Sprint M2.6 D11).</summary>
public sealed class SafetyCheckResult
{
    public required bool Safe { get; init; }

    public string? Reason { get; init; }

    public static SafetyCheckResult Ok() => new() { Safe = true };

    public static SafetyCheckResult Unsafe(string reason) => new() { Safe = false, Reason = reason };
}

/// <summary>The facts a safety check needs about the current run, gathered by the caller (Sprint M2.6 D11) — the gate itself performs no I/O.</summary>
public sealed class SafetyContext
{
    /// <summary>True when the resource/action's required capability (e.g. a Docker provider) is currently available.</summary>
    public required bool CapabilityAvailable { get; init; }

    /// <summary>True when the <see cref="DesiredState"/> this cycle used still matches the latest approved <see cref="Planning.DeploymentPlan"/> — false means a newer plan exists and this cycle's desired state is stale.</summary>
    public required bool DeploymentPlanIsCurrent { get; init; }

    /// <summary>True when the package this repair would act on has passed integrity verification (e.g. the same check Admission's Package Integrity category already performed).</summary>
    public required bool PackageIntegrityVerified { get; init; }

    /// <summary>
    /// True when an operator has placed this environment into maintenance mode (Sprint M2.7,
    /// e.g. via the Operations Command Center). Maintenance mode does not stop reconciliation
    /// from running — Desired/Observed State are still compared and drift is still detected and
    /// recorded — it only suspends automatic repair, exactly like every other Safety Framework
    /// condition: downgraded to requiring an operator, never silently skipped.
    /// </summary>
    public bool MaintenanceModeActive { get; init; }
}

/// <summary>
/// The last line of defense before any repair runs automatically (Sprint M2.6 D11): even a repair
/// an <see cref="IRepairPolicyEvaluator"/> approved is downgraded to requiring operator approval if any of
/// the five conditions the brief names are true. This is deliberately a separate, final gate from
/// policy evaluation — a policy answers "is this kind of repair generally allowed here?"; this
/// gate answers "is it safe to run *this specific instance*, *right now*?" — so a transient
/// condition (e.g. a provider that just became unavailable) can block an otherwise-approved
/// repair without requiring any policy reconfiguration.
/// </summary>
public static class ReconciliationSafetyGate
{
    public static SafetyCheckResult Evaluate(RepairTask task, SafetyContext context)
    {
        ArgumentNullException.ThrowIfNull(task);
        ArgumentNullException.ThrowIfNull(context);

        if (context.MaintenanceModeActive)
        {
            return SafetyCheckResult.Unsafe("This environment is in maintenance mode — automatic repairs are suspended until maintenance is resumed.");
        }

        if (!context.DeploymentPlanIsCurrent)
        {
            return SafetyCheckResult.Unsafe("The Deployment Plan this cycle's Desired State was built from is no longer the latest approved plan.");
        }

        if (!context.CapabilityAvailable)
        {
            return SafetyCheckResult.Unsafe($"The capability required to execute '{task.ActionType}' is currently unavailable.");
        }

        if (!context.PackageIntegrityVerified)
        {
            return SafetyCheckResult.Unsafe("Package integrity could not be verified for the resource this repair would act on.");
        }

        if (task.ActionType == RepairActionType.Unknown)
        {
            return SafetyCheckResult.Unsafe("No single, unambiguous repair strategy is known for this drift — multiple strategies could apply, so none is chosen automatically.");
        }

        return SafetyCheckResult.Ok();
    }
}
