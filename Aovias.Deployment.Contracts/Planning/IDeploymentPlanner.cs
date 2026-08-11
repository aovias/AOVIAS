using Aovias.Deployment.Contracts.Admission;
using Aovias.Deployment.Contracts.Capabilities;
using Aovias.Deployment.Contracts.Infrastructure;

namespace Aovias.Deployment.Contracts.Planning;

/// <summary>
/// Everything <see cref="IDeploymentPlanner.CreatePlan"/> needs to build a <see cref="DeploymentPlan"/>
/// (Sprint M2.4 D2). Requires an already-<see cref="AdmissionDecision.Approved"/>
/// <see cref="AdmissionResult"/> — planning never runs for a rejected request (Final Engineering
/// Rule: "Admission precedes planning").
/// </summary>
public sealed class DeploymentPlanRequest
{
    public required ServiceDeploymentBundle Bundle { get; init; }

    public required InfrastructureContract Contract { get; init; }

    public required AdmissionResult Admission { get; init; }

    public required string EnvironmentId { get; init; }

    public string? CapabilityProfileId { get; init; }

    public required IReadOnlyList<Capability> AvailableCapabilities { get; init; }

    /// <summary>Supplied by the caller from a fixed point in time — never read from the system clock inside the planner (see <see cref="DeploymentPlanMetadata.GeneratedUtc"/>).</summary>
    public required DateTimeOffset GeneratedUtc { get; init; }
}

/// <summary>
/// Decides <em>how</em> an approved deployment should occur (Sprint M2.4 D2). Never decides
/// <em>whether</em> it should occur — that is <see cref="IDeploymentAdmissionEngine"/>'s job, and
/// this planner requires an already-approved <see cref="AdmissionResult"/> as an explicit
/// precondition (Final Engineering Rule 2: "The Planner decides how deployment should occur").
/// </summary>
public interface IDeploymentPlanner
{
    /// <summary>
    /// Produces a deterministic <see cref="DeploymentPlan"/>: the same approved request always
    /// produces a plan with the same content hash (Sprint M2.4 D10), regardless of when or how
    /// many times it is called. Performs no execution.
    /// </summary>
    DeploymentPlan CreatePlan(DeploymentPlanRequest request);
}
