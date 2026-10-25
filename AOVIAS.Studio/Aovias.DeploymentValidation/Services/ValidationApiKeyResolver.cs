using System.Text.Json;
using Aovias.DeploymentValidation.Models;

namespace Aovias.DeploymentValidation.Services;

/// <summary>
/// Resolves the validation API key used to probe secured deployment endpoints during DRV.
/// </summary>
public static class ValidationApiKeyResolver
{
    public static string? Resolve(DeploymentValidationContext context)
    {
        var configRoot = context.Workspace?.ValidationConfigRoot ?? context.ValidationConfigRoot;
        if (string.IsNullOrWhiteSpace(configRoot))
        {
            return null;
        }

        foreach (var file in Directory.EnumerateFiles(configRoot, "settings.security.json", SearchOption.AllDirectories))
        {
            try
            {
                using var document = JsonDocument.Parse(File.ReadAllText(file));
                if (document.RootElement.TryGetProperty("apiKey", out var apiKey)
                    && apiKey.ValueKind == JsonValueKind.String)
                {
                    var value = apiKey.GetString();
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        return value;
                    }
                }
            }
            catch (JsonException)
            {
                // Ignore malformed files.
            }
        }

        return null;
    }
}
