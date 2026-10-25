using Aovias.Deployment.Contracts.Capabilities;

namespace Aovias.Deployment.Contracts.Admission;

/// <summary>
/// Sprint M4.4 — the environment-level Deployment Ready evaluator. Pure and deterministic, exactly
/// like <see cref="RequirementCapabilityMatcher"/> and <see cref="DeploymentAdmissionEngine"/>
/// before it (Sprint M2.3/M2.4): the same profile id and the same requirement set always produce
/// the same report ("Admission must be deterministic", "Readiness must be reproducible" — M4.4
/// rules). No I/O, no probing — every fact is supplied by the caller.
///
/// Deliberately reuses two already-existing pieces rather than re-implementing capability
/// evaluation ("No duplicated capability evaluation" — M4.4 rule):
/// <list type="bullet">
/// <item><see cref="EnvironmentCapabilityProfileLoader.ResolveCapabilities"/> (Sprint M2.3 D6) —
/// the authoritative resolution of a declared profile id to the <see cref="Capability"/> objects it
/// provides. This, not the Sprint M4.3 heartbeat-synced flat platform-feature capability strings
/// (<c>RuntimeCapability.Runtime</c>/<c>Publishing</c>/etc. — a disjoint vocabulary describing what
/// the Runtime Agent process itself supports, not what infrastructure the host provides), is what
/// "authoritative capabilities" means for this evaluator ("Admission must consume authoritative
/// capabilities only" — M4.4 rule).</item>
/// <item><see cref="RequirementCapabilityMatcher.Evaluate"/> (Sprint M2.3 D4) — the one and only
/// requirement/capability matching algorithm in the platform.</item>
/// </list>
/// </summary>
public static class EnvironmentAdmissionEvaluator
{
    /// <param name="capabilityProfileId">The environment's declared <see cref="EnvironmentCapabilityProfile"/> id.</param>
    /// <param name="requirementsByServiceId">
    /// Every requirement this environment has been asked to satisfy so far, keyed by the service
    /// that declared it. An empty map (a freshly registered environment with nothing deployed yet)
    /// is valid input and deterministically yields <see cref="EnvironmentAdmissionReport.IsReady"/>
    /// = <see langword="true"/> with zero diagnostics — there is nothing yet to be unready for.
    /// </param>
    public static EnvironmentAdmissionReport Evaluate(
        string capabilityProfileId,
        IReadOnlyDictionary<string, IReadOnlyList<Requirement>> requirementsByServiceId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(capabilityProfileId);
        ArgumentNullException.ThrowIfNull(requirementsByServiceId);

        var capabilities = EnvironmentCapabilityProfileLoader.ResolveCapabilities(capabilityProfileId);

        var diagnostics = new List<EnvironmentRequirementDiagnostic>();
        foreach (var serviceId in requirementsByServiceId.Keys.OrderBy(id => id, StringComparer.OrdinalIgnoreCase))
        {
            var matchReport = RequirementCapabilityMatcher.Evaluate(requirementsByServiceId[serviceId], capabilities);
            foreach (var result in matchReport.Results)
            {
                diagnostics.Add(new EnvironmentRequirementDiagnostic
                {
                    ServiceId = serviceId,
                    RequirementId = result.Requirement.Id,
                    Category = result.Requirement.Category,
                    Status = Classify(result),
                    Message = result.Message
                });
            }
        }

        return new EnvironmentAdmissionReport
        {
            CapabilityProfileId = capabilityProfileId,
            IsReady = diagnostics.All(d => d.Status != EnvironmentRequirementStatus.Missing),
            Diagnostics = diagnostics,
            ServiceIdsEvaluated = requirementsByServiceId.Keys.OrderBy(id => id, StringComparer.OrdinalIgnoreCase).ToList()
        };
    }

    private static EnvironmentRequirementStatus Classify(RequirementMatchResult result)
    {
        if (result.Status == RequirementMatchStatus.Satisfied)
        {
            return EnvironmentRequirementStatus.Satisfied;
        }

        if (RequirementCategories.ExternallyProvisionedCategories.Contains(result.Requirement.Category, StringComparer.OrdinalIgnoreCase))
        {
            return EnvironmentRequirementStatus.External;
        }

        return result.Requirement.Mandatory
            ? EnvironmentRequirementStatus.Missing
            : EnvironmentRequirementStatus.Deferrable;
    }
}
