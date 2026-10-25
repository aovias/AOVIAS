using Aovias.Deployment.Contracts.Capabilities;
using Aovias.Deployment.Contracts.Migration;

namespace Aovias.Deployment.Contracts.Admission;

/// <summary>
/// The single implementation of the Sprint M2.4 D4 admission pipeline: Package Integrity →
/// Schema Compatibility → Runtime Compatibility → Requirement Matching → Capability Matching →
/// Infrastructure Readiness → Environment Validation → Approval, evaluated in that exact order.
///
/// Evaluation stops at the first category that produces a <see cref="AdmissionSeverity.Blocking"/>
/// diagnostic (D4: "Failure immediately stops planning") — a rejection's <see cref="AdmissionResult.Diagnostics"/>
/// are therefore always about the one real cause, never a mixture of a root failure and whatever
/// unrelated noise a later, never-reached category might otherwise have reported. Every category
/// already evaluated (including non-blocking warnings) is preserved in the result either way.
///
/// Reuses, rather than re-implements, Sprint M2.2's <see cref="InfrastructureContractCompatibility"/>
/// constants and Sprint M2.3's <see cref="RequirementCapabilityMatcher"/> — Admission is a new
/// decision layer on top of the existing contract and capability model, not a parallel one.
/// </summary>
public sealed class DeploymentAdmissionEngine : IDeploymentAdmissionEngine
{
    public AdmissionResult Evaluate(AdmissionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var diagnostics = new List<AdmissionDiagnostic>();
        var evaluated = new List<AdmissionCategory>();

        var approved =
            EvaluatePackageIntegrity(request, diagnostics, evaluated) &&
            EvaluateSchemaCompatibility(request, diagnostics, evaluated) &&
            EvaluateRuntimeCompatibility(request, diagnostics, evaluated) &&
            EvaluateRequirementAndCapabilityMatching(request, diagnostics, evaluated) &&
            EvaluateInfrastructureReadiness(request, diagnostics, evaluated) &&
            EvaluateEnvironmentValidation(request, diagnostics, evaluated);

        return new AdmissionResult
        {
            Decision = approved ? AdmissionDecision.Approved : AdmissionDecision.Rejected,
            CorrelationId = request.CorrelationId,
            Diagnostics = diagnostics,
            EvaluatedCategories = evaluated
        };
    }

    private static bool EvaluatePackageIntegrity(AdmissionRequest request, List<AdmissionDiagnostic> diagnostics, List<AdmissionCategory> evaluated)
    {
        evaluated.Add(AdmissionCategory.PackageIntegrity);
        if (request.PackageIntegrity.Verified)
        {
            return true;
        }

        diagnostics.Add(new AdmissionDiagnostic
        {
            Category = AdmissionCategory.PackageIntegrity,
            Reason = request.PackageIntegrity.FailureReason ?? "Package integrity verification failed.",
            AffectedService = request.ServiceId,
            Remediation = "Re-publish or re-transfer the deployment package. Do not deploy a package that failed checksum verification.",
            Severity = AdmissionSeverity.Blocking,
            CorrelationId = request.CorrelationId
        });
        return false;
    }

    private static bool EvaluateSchemaCompatibility(AdmissionRequest request, List<AdmissionDiagnostic> diagnostics, List<AdmissionCategory> evaluated)
    {
        evaluated.Add(AdmissionCategory.SchemaCompatibility);
        var contract = request.Contract;

        if (contract.SchemaVersion != ContractSchemaVersions.Current)
        {
            diagnostics.Add(new AdmissionDiagnostic
            {
                Category = AdmissionCategory.SchemaCompatibility,
                Reason = $"Contract schema version {contract.SchemaVersion} does not match this runtime's current schema version {ContractSchemaVersions.Current}.",
                AffectedService = request.ServiceId,
                Remediation = "This contract reached Admission without going through DeploymentPackageContractReader's migration path. Re-read the package through the reader (which migrates to the current schema) before admission.",
                Severity = AdmissionSeverity.Blocking,
                CorrelationId = request.CorrelationId
            });
            return false;
        }

        if (!ContractSchemaVersions.Supported.Contains(contract.Compatibility.MaximumSupportedSchemaVersion))
        {
            diagnostics.Add(new AdmissionDiagnostic
            {
                Category = AdmissionCategory.SchemaCompatibility,
                Reason = $"Contract declares a maximum supported schema version ({contract.Compatibility.MaximumSupportedSchemaVersion}) this runtime does not recognize.",
                AffectedService = request.ServiceId,
                Remediation = "Upgrade this runtime to a version that recognizes the declared schema range, or re-publish the package against a supported schema version.",
                Severity = AdmissionSeverity.Blocking,
                CorrelationId = request.CorrelationId
            });
            return false;
        }

        return true;
    }

    private static bool EvaluateRuntimeCompatibility(AdmissionRequest request, List<AdmissionDiagnostic> diagnostics, List<AdmissionCategory> evaluated)
    {
        evaluated.Add(AdmissionCategory.RuntimeCompatibility);

        if (string.IsNullOrWhiteSpace(request.MinimumRuntimeAgentVersion))
        {
            return true;
        }

        if (VersionConstraint.IsSatisfiedBy($">={request.MinimumRuntimeAgentVersion}", request.RuntimeAgentVersion))
        {
            return true;
        }

        diagnostics.Add(new AdmissionDiagnostic
        {
            Category = AdmissionCategory.RuntimeCompatibility,
            Reason = $"Runtime Agent version {request.RuntimeAgentVersion} is below the package's minimum required version {request.MinimumRuntimeAgentVersion}.",
            AffectedService = request.ServiceId,
            Remediation = $"Upgrade Runtime Agent to at least version {request.MinimumRuntimeAgentVersion} before retrying this deployment.",
            Severity = AdmissionSeverity.Blocking,
            CorrelationId = request.CorrelationId
        });
        return false;
    }

    private static bool EvaluateRequirementAndCapabilityMatching(AdmissionRequest request, List<AdmissionDiagnostic> diagnostics, List<AdmissionCategory> evaluated)
    {
        evaluated.Add(AdmissionCategory.RequirementMatching);
        evaluated.Add(AdmissionCategory.CapabilityMatching);

        var report = RequirementCapabilityMatcher.Evaluate(request.Contract.Requirements, request.AvailableCapabilities);
        var blocking = false;

        foreach (var result in report.Results.Where(result => result.Status != RequirementMatchStatus.Satisfied))
        {
            var category = result.Status == RequirementMatchStatus.Missing
                ? AdmissionCategory.RequirementMatching
                : AdmissionCategory.CapabilityMatching;
            var severity = result.Requirement.Mandatory ? AdmissionSeverity.Blocking : AdmissionSeverity.Warning;

            diagnostics.Add(new AdmissionDiagnostic
            {
                Category = category,
                Reason = result.Message,
                AffectedService = request.ServiceId,
                AffectedCapability = result.Requirement.Category,
                Remediation = ExtractRemediation(result.Message),
                Severity = severity,
                CorrelationId = request.CorrelationId
            });

            blocking |= severity == AdmissionSeverity.Blocking;
        }

        return !blocking;
    }

    private static bool EvaluateInfrastructureReadiness(AdmissionRequest request, List<AdmissionDiagnostic> diagnostics, List<AdmissionCategory> evaluated)
    {
        evaluated.Add(AdmissionCategory.InfrastructureReadiness);

        if (request.InfrastructureReadiness.Ready)
        {
            return true;
        }

        var blockers = request.InfrastructureReadiness.Blockers;
        if (blockers.Count == 0)
        {
            diagnostics.Add(new AdmissionDiagnostic
            {
                Category = AdmissionCategory.InfrastructureReadiness,
                Reason = "Infrastructure readiness reported not-ready without a specific blocker.",
                AffectedService = request.ServiceId,
                Remediation = "Inspect InfrastructureProvisioningPipeline stage results for the underlying cause.",
                Severity = AdmissionSeverity.Blocking,
                CorrelationId = request.CorrelationId
            });
            return false;
        }

        foreach (var blocker in blockers)
        {
            diagnostics.Add(new AdmissionDiagnostic
            {
                Category = AdmissionCategory.InfrastructureReadiness,
                Reason = blocker,
                AffectedService = request.ServiceId,
                Remediation = "Resolve the infrastructure provisioning blocker reported by InfrastructureProvisioningPipeline, then retry.",
                Severity = AdmissionSeverity.Blocking,
                CorrelationId = request.CorrelationId
            });
        }

        return false;
    }

    private static bool EvaluateEnvironmentValidation(AdmissionRequest request, List<AdmissionDiagnostic> diagnostics, List<AdmissionCategory> evaluated)
    {
        evaluated.Add(AdmissionCategory.EnvironmentValidation);
        var environment = request.Environment;

        if (!environment.Registered)
        {
            diagnostics.Add(new AdmissionDiagnostic
            {
                Category = AdmissionCategory.EnvironmentValidation,
                Reason = $"Environment '{environment.EnvironmentId}' is not registered.",
                AffectedService = request.ServiceId,
                Remediation = "Register the target environment before attempting deployment to it.",
                Severity = AdmissionSeverity.Blocking,
                CorrelationId = request.CorrelationId
            });
            return false;
        }

        if (!string.IsNullOrWhiteSpace(environment.CapabilityProfileId))
        {
            try
            {
                EnvironmentCapabilityProfileLoader.Load(environment.CapabilityProfileId);
            }
            catch (InvalidOperationException exception)
            {
                diagnostics.Add(new AdmissionDiagnostic
                {
                    Category = AdmissionCategory.EnvironmentValidation,
                    Reason = exception.Message,
                    AffectedService = request.ServiceId,
                    Remediation = "Declare a valid, registered environment capability profile id, or correct the typo in the requested profile id.",
                    Severity = AdmissionSeverity.Blocking,
                    CorrelationId = request.CorrelationId
                });
                return false;
            }
        }

        return true;
    }

    private static string ExtractRemediation(string matcherMessage)
    {
        const string marker = "Suggested remediation:";
        var index = matcherMessage.IndexOf(marker, StringComparison.Ordinal);
        return index >= 0
            ? matcherMessage[(index + marker.Length)..].Trim()
            : "Review the requirement and the target environment's declared capabilities.";
    }
}
