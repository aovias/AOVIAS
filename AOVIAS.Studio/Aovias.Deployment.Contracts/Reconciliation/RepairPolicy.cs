namespace Aovias.Deployment.Contracts.Reconciliation;

/// <summary>
/// A minimal, environment-awareness enum purpose-built for repair policy evaluation (Sprint M2.6
/// D7) — deliberately not a reference to <c>Aovias.Release.Contracts.RemoteDeployment.EnvironmentType</c>,
/// since that type lives in a different project this shared library does not otherwise depend on
/// (mirroring the same "introduce a new, differently-named type rather than reach across an
/// out-of-scope project" pattern <c>Execution.IExecutionGateCheck</c> established in Sprint M2.5).
/// Runtime Agent maps its own environment concept onto this one at composition time.
/// </summary>
public enum EnvironmentClass
{
    Unknown,
    Development,
    Demo,
    Qa,
    Uat,
    Production
}

/// <summary>The four literal policy types the brief names, plus two environment/time-scoped variants (Sprint M2.6 D7).</summary>
public enum RepairPolicyType
{
    AlwaysRepair,
    AskOperator,
    NeverRepair,
    MaintenanceWindowOnly,
    ProductionOnly,
    DemoOnly
}

/// <summary>What a policy evaluation resolves to for one repair task (Sprint M2.6 D7).</summary>
public enum PolicyDecision
{
    Approve,
    Deny,
    RequireOperatorApproval
}

/// <summary>
/// One configurable rule (Sprint M2.6 D7). Rules are matched most-specific-first by
/// <see cref="RepairPolicyEvaluator"/>: an <see cref="ActionType"/>+<see cref="Severity"/> match
/// beats an <see cref="ActionType"/>-only match, which beats a <see cref="Severity"/>-only match,
/// which beats the policy set's <see cref="RepairPolicySet.DefaultPolicy"/>.
/// </summary>
public sealed class RepairPolicyRule
{
    /// <summary><see langword="null"/> matches every action type.</summary>
    public RepairActionType? ActionType { get; init; }

    /// <summary><see langword="null"/> matches every severity.</summary>
    public DriftSeverity? Severity { get; init; }

    public required RepairPolicyType PolicyType { get; init; }
}

/// <summary>
/// A complete, environment-aware policy configuration (Sprint M2.6 D7). One
/// <see cref="RepairPolicySet"/> exists per environment (or environment class); Runtime Agent
/// composes the one used for its own environment.
/// </summary>
public sealed class RepairPolicySet
{
    public required EnvironmentClass Environment { get; init; }

    public IReadOnlyList<RepairPolicyRule> Rules { get; init; } = [];

    /// <summary>Used only by <see cref="RepairPolicyType.MaintenanceWindowOnly"/> rules — the daily window (in UTC, start may be after end to represent an overnight window) during which such repairs are allowed to run automatically.</summary>
    public TimeOnly? MaintenanceWindowStartUtc { get; init; }

    public TimeOnly? MaintenanceWindowEndUtc { get; init; }

    /// <summary>The fail-safe default when no rule matches (D7/D11): require an operator, never silently approve or deny.</summary>
    public RepairPolicyType DefaultPolicy { get; init; } = RepairPolicyType.AskOperator;
}

/// <summary>Evaluates one <see cref="RepairTask"/> against a <see cref="RepairPolicySet"/> to decide whether it may run automatically (Sprint M2.6 D7). The sole implementation is <see cref="RepairPolicyEvaluator"/>.</summary>
public interface IRepairPolicyEvaluator
{
    PolicyDecision Evaluate(RepairTask task, DriftFinding finding, RepairPolicySet policySet, DateTimeOffset nowUtc);
}

/// <summary>
/// The single <see cref="IRepairPolicyEvaluator"/> implementation (Sprint M2.6 D7). Evaluates
/// rules most-specific-first; unmatched action types/severities fall through to
/// <see cref="RepairPolicySet.DefaultPolicy"/>, which itself defaults to
/// <see cref="RepairPolicyType.AskOperator"/> — an unconfigured environment never silently
/// auto-repairs anything (D11: fail safely).
/// </summary>
public sealed class RepairPolicyEvaluator : IRepairPolicyEvaluator
{
    public PolicyDecision Evaluate(RepairTask task, DriftFinding finding, RepairPolicySet policySet, DateTimeOffset nowUtc)
    {
        ArgumentNullException.ThrowIfNull(task);
        ArgumentNullException.ThrowIfNull(finding);
        ArgumentNullException.ThrowIfNull(policySet);

        var rule = policySet.Rules.FirstOrDefault(r => r.ActionType == task.ActionType && r.Severity == finding.Severity)
            ?? policySet.Rules.FirstOrDefault(r => r.ActionType == task.ActionType && r.Severity is null)
            ?? policySet.Rules.FirstOrDefault(r => r.ActionType is null && r.Severity == finding.Severity);

        var policyType = rule?.PolicyType ?? policySet.DefaultPolicy;

        var decision = policyType switch
        {
            RepairPolicyType.AlwaysRepair => PolicyDecision.Approve,
            RepairPolicyType.NeverRepair => PolicyDecision.Deny,
            RepairPolicyType.AskOperator => PolicyDecision.RequireOperatorApproval,
            RepairPolicyType.ProductionOnly => policySet.Environment == EnvironmentClass.Production ? PolicyDecision.Approve : PolicyDecision.RequireOperatorApproval,
            RepairPolicyType.DemoOnly => policySet.Environment == EnvironmentClass.Demo ? PolicyDecision.Approve : PolicyDecision.RequireOperatorApproval,
            RepairPolicyType.MaintenanceWindowOnly => IsWithinMaintenanceWindow(policySet, nowUtc) ? PolicyDecision.Approve : PolicyDecision.RequireOperatorApproval,
            _ => PolicyDecision.RequireOperatorApproval
        };

        // The matrix's own fixed opinion always wins over a policy trying to loosen it: a repair
        // the matrix says always requires approval (e.g. CertificateReplacement) can never be
        // downgraded to Approve by policy (Final Engineering Rule: "Unsafe repairs always require
        // explicit operator approval"). A matrix opinion of `false` (never requires approval) can
        // still be tightened to Deny or RequireOperatorApproval by policy, since policy is always
        // allowed to be more conservative, never less.
        if (task.RequiresApprovalByDefault == true && decision == PolicyDecision.Approve)
        {
            return PolicyDecision.RequireOperatorApproval;
        }

        return decision;
    }

    private static bool IsWithinMaintenanceWindow(RepairPolicySet policySet, DateTimeOffset nowUtc)
    {
        if (policySet.MaintenanceWindowStartUtc is not { } start || policySet.MaintenanceWindowEndUtc is not { } end)
        {
            return false;
        }

        var now = TimeOnly.FromDateTime(nowUtc.UtcDateTime);
        return start <= end
            ? now >= start && now <= end
            : now >= start || now <= end; // overnight window (e.g. 22:00-04:00)
    }
}
