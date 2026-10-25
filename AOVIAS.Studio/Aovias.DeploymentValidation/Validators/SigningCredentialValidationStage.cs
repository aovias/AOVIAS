using System.Diagnostics;
using System.Text.Json;
using Aovias.Deployment.Contracts.Provisioning;
using Aovias.DeploymentValidation.Contracts;
using Aovias.DeploymentValidation.Models;
using Aovias.Publishing.Contracts;

namespace Aovias.DeploymentValidation.Validators;

/// <summary>
/// Simulates Identity signing-credential startup checks on the host before container startup.
/// </summary>
public sealed class SigningCredentialValidationStage : IDeploymentValidationStage
{
    public string StageName => "SigningCredential";

    public ValidationStageDescriptor Descriptor { get; } = new(
        ValidationCategory.Infrastructure,
        ValidationExecutionScope.Deployment,
        IsOptional: true);

    public Task<ValidationStageResult> ValidateAsync(
        DeploymentValidationContext context,
        CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        var stopwatch = Stopwatch.StartNew();
        var bundle = context.ResolveDeploymentBundle();
        if (bundle is null)
        {
            return Task.FromResult(ValidationStageResult.Fail(StageName, stopwatch.Elapsed, ["Unable to resolve deployment contracts."]));
        }

        if (bundle.Certificate.Certificates.All(item => !item.Required))
        {
            stopwatch.Stop();
            return Task.FromResult(ValidationStageResult.Pass(
                StageName,
                stopwatch.Elapsed,
                recommendations: ["No signing certificates declared by contract."]));
        }

        var configRoot = context.Workspace?.ValidationConfigRoot ?? context.ValidationConfigRoot;
        if (string.IsNullOrWhiteSpace(configRoot) || !Directory.Exists(configRoot))
        {
            return Task.FromResult(ValidationStageResult.Fail(
                StageName,
                stopwatch.Elapsed,
                ["Validation config root is unavailable for signing-credential checks."]));
        }

        ContractCertificateProvisioner.EnsureProvisioned(configRoot, bundle.Certificate);

        var certificateReport = ContractCertificateValidator.Validate(configRoot, bundle.Certificate);
        var secretReport = ContractSecretValidator.Validate(configRoot, bundle.Secret);
        var errors = new List<string>();

        foreach (var result in certificateReport.Results.Where(item => !item.Passed))
        {
            errors.AddRange(result.Errors.Select(error => $"Signing credential ({result.CertificateId}): {error}"));
        }

        errors.AddRange(secretReport.Errors.Select(error => $"Secret contract: {error}"));

        if (string.Equals(bundle.Manifest.ServiceId, "identity-service", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(ResolveProfileSetting(configRoot, "jwtIssuer")))
            {
                errors.Add("JWT issuer is not configured (profiles/*/settings.security.json → jwtIssuer).");
            }

            if (string.IsNullOrWhiteSpace(ResolveProfileSetting(configRoot, "jwtAudience")))
            {
                errors.Add("JWT audience is not configured (profiles/*/settings.security.json → jwtAudience).");
            }
        }

        stopwatch.Stop();
        if (errors.Count > 0)
        {
            return Task.FromResult(ValidationStageResult.Fail(StageName, stopwatch.Elapsed, errors));
        }

        var recommendations = certificateReport.Results
            .Where(item => item.Passed)
            .Select(item => $"Signing credential '{item.CertificateId}' validated (thumbprint={item.Thumbprint}).")
            .ToList();

        return Task.FromResult(ValidationStageResult.Pass(StageName, stopwatch.Elapsed, recommendations: recommendations));
    }

    private static string? ResolveProfileSetting(string configRoot, string propertyName)
    {
        var profilesRoot = Path.Combine(configRoot, "profiles");
        if (!Directory.Exists(profilesRoot))
        {
            return null;
        }

        foreach (var settingsFile in Directory.EnumerateFiles(profilesRoot, "settings.security.json", SearchOption.AllDirectories))
        {
            try
            {
                using var document = JsonDocument.Parse(File.ReadAllText(settingsFile));
                if (document.RootElement.TryGetProperty(propertyName, out var valueElement)
                    && valueElement.ValueKind == JsonValueKind.String)
                {
                    var value = valueElement.GetString();
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        return value;
                    }
                }
            }
            catch (JsonException)
            {
                // Ignore malformed profile settings.
            }
        }

        return null;
    }
}
