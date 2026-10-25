using Aovias.DeploymentValidation.Models;

namespace Aovias.DeploymentValidation.Services;

/// <summary>
/// Resolves whether a validation stage should execute for the active pipeline scope.
/// </summary>
public static class ValidationScopePolicy
{
    private static readonly IReadOnlyDictionary<string, string> DeferredStageLabels =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Runtime Bootstrap"] = "Runtime Verification",
            ["Runtime Verification"] = "Runtime Verification",
            ["Runtime"] = "Runtime Startup",
            ["Health"] = "Health Endpoint",
            ["Infrastructure"] = "Infrastructure Provisioning",
            ["SigningCredential"] = "Signing Credential"
        };

    public static bool ShouldExecute(ValidationStageDescriptor descriptor, ValidationExecutionScope contextScope)
        => contextScope switch
        {
            ValidationExecutionScope.Publishing => descriptor.ExecutionScope is ValidationExecutionScope.Publishing or ValidationExecutionScope.Both,
            ValidationExecutionScope.Deployment => descriptor.ExecutionScope is ValidationExecutionScope.Deployment or ValidationExecutionScope.Both,
            _ => true
        };

    public static bool ShouldDefer(ValidationStageDescriptor descriptor, ValidationExecutionScope contextScope)
        => contextScope == ValidationExecutionScope.Publishing
           && descriptor.ExecutionScope == ValidationExecutionScope.Deployment;

    public static string MapDeferredStageName(string stageName)
        => DeferredStageLabels.TryGetValue(stageName, out var mapped) ? mapped : stageName;

    public static string DeferredReason(string stageName)
        => $"{MapDeferredStageName(stageName)} runs during deployment.";
}
