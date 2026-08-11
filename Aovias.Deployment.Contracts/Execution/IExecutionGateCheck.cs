namespace Aovias.Deployment.Contracts.Execution;

/// <summary>The result of one gate check (Sprint M2.5 D11).</summary>
public sealed class GateCheckResult
{
    public required bool Passed { get; init; }

    public string? FailureReason { get; init; }

    public static GateCheckResult Pass() => new() { Passed = true };

    public static GateCheckResult Fail(string reason) => new() { Passed = false, FailureReason = reason };
}

/// <summary>
/// Verifies a single named gate — a health probe or a certification check (Sprint M2.5 D11).
/// Deliberately abstracted away from any concrete health/docker/HTTP client: the shared
/// <c>Aovias.Deployment.Contracts</c> library has no dependency on Runtime Agent's
/// <c>IHealthProvider</c>/<c>IDockerProvider</c>, so the real implementation is supplied by
/// Runtime Agent at composition time (see <c>RuntimeAgentExecutionGateCheck</c> in
/// <c>Aovias.Runtime.Agent.Infrastructure</c>), while this library only depends on the
/// abstraction. Health verification must be deterministic per the M2.5 brief — the same gate,
/// checked against the same running system state, should not flip between calls without the
/// underlying system actually changing.
/// </summary>
public interface IExecutionGateCheck
{
    Task<GateCheckResult> CheckAsync(string gateId, DeploymentExecutionContext context, CancellationToken cancellationToken);
}

/// <summary>
/// A gate check that always passes. Intended for tests and for any composition that has not yet
/// wired a real check — never appropriate as a real production default, since it would silently
/// pretend health/certification verification happened when it did not. Runtime Agent's real
/// wiring (Sprint M2.5 D6/integration) must supply an implementation backed by the actual
/// <c>IHealthProvider</c>/<c>IDockerProvider</c>, not this one.
/// </summary>
public sealed class AlwaysPassGateCheck : IExecutionGateCheck
{
    public Task<GateCheckResult> CheckAsync(string gateId, DeploymentExecutionContext context, CancellationToken cancellationToken)
        => Task.FromResult(GateCheckResult.Pass());
}
