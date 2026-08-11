namespace Aovias.Deployment.Contracts.Admission;

/// <summary>
/// Decides whether a deployment may proceed (Sprint M2.4 D1). This is the one and only place
/// that decision is made — the Deployment Planner (D2) never runs for a rejected request, and
/// Runtime provisioners never evaluate requirements or capabilities independently (Final
/// Engineering Rule 1: "Admission decides if deployment is allowed").
/// </summary>
public interface IDeploymentAdmissionEngine
{
    /// <summary>
    /// Evaluates <paramref name="request"/> deterministically: the same request always produces
    /// the same <see cref="AdmissionResult"/> (Engineering Principle: "Admission failures are
    /// deterministic"). Performs no I/O — every fact required is already present on the request.
    /// </summary>
    AdmissionResult Evaluate(AdmissionRequest request);
}
