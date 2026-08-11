using System.Text.Json;

namespace Aovias.Deployment.Contracts.Provisioning;

/// <summary>
/// Validates required secrets declared by a deployment contract exist on the host before deployment.
/// </summary>
public static class ContractSecretValidator
{
    public static SecretValidationReport Validate(string configRoot, SecretContract contract)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(configRoot);
        ArgumentNullException.ThrowIfNull(contract);

        var errors = new List<string>();
        foreach (var secret in contract.Secrets.Where(item => item.Required))
        {
            if (string.IsNullOrWhiteSpace(secret.Name))
            {
                errors.Add($"Secret '{secret.Id}' is missing a name.");
                continue;
            }

            if (!TryResolveSecretValue(configRoot, secret.Name, out _))
            {
                if (!TryResolveSecretCandidates(configRoot, secret, out _))
                {
                    errors.Add(
                        $"Required secret '{secret.Name}' is missing from settings.secrets.json " +
                        $"(maps to '{secret.ConfigurationKey}').");
                }
            }
        }

        return errors.Count == 0
            ? SecretValidationReport.Success()
            : SecretValidationReport.Failure(errors);
    }

    public static bool TryResolveSecretValue(string configRoot, string secretName, out string value)
    {
        value = string.Empty;
        var secretsRoot = Path.Combine(configRoot, "secrets");
        if (!Directory.Exists(secretsRoot))
        {
            return false;
        }

        foreach (var secretsFile in Directory.EnumerateFiles(secretsRoot, "settings.secrets.json", SearchOption.AllDirectories))
        {
            try
            {
                using var document = JsonDocument.Parse(File.ReadAllText(secretsFile));
                if (document.RootElement.TryGetProperty(secretName, out var valueElement)
                    && valueElement.ValueKind == JsonValueKind.String)
                {
                    var resolved = valueElement.GetString();
                    if (!string.IsNullOrWhiteSpace(resolved))
                    {
                        value = resolved;
                        return true;
                    }
                }
            }
            catch (JsonException)
            {
                // Ignore malformed secrets files and continue searching.
            }
        }

        return false;
    }

    private static bool TryResolveSecretCandidates(string configRoot, SecretRequirementContract secret, out string value)
    {
        foreach (var candidate in BuildSecretCandidates(secret))
        {
            if (TryResolveSecretValue(configRoot, candidate, out value))
            {
                return true;
            }
        }

        value = string.Empty;
        return false;
    }

    private static IEnumerable<string> BuildSecretCandidates(SecretRequirementContract secret)
    {
        yield return secret.Id.Replace('-', '_');
        yield return secret.ConfigurationKey;
        if (secret.ConfigurationKey.Contains(':', StringComparison.Ordinal))
        {
            yield return secret.ConfigurationKey.Split(':').Last();
        }
    }
}
