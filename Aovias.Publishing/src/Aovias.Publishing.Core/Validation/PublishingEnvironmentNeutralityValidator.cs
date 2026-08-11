using Aovias.DeploymentValidation.Rules;
using Aovias.Publishing.Contracts;

namespace Aovias.Publishing.Core.Validation;

/// <summary>
/// Validates generated publishing artifacts for environment neutrality before packaging.
/// </summary>
public static class PublishingEnvironmentNeutralityValidator
{
    public static IReadOnlyList<string> Validate(PublishingContext context)
    {
        var errors = new List<string>();

        if (!string.IsNullOrWhiteSpace(context.EnvTemplateContent))
        {
            errors.AddRange(EnvTemplateValidationRules.ValidateNeutralTemplate(
                context.EnvTemplateContent,
                DeployPackageLayout.EnvTemplateFile));
        }

        if (!string.IsNullOrWhiteSpace(context.ComposeContent))
        {
            errors.AddRange(EnvironmentNeutralRules.ScanComposeFile(
                context.ComposeContent,
                DeployPackageLayout.ComposeFile));

            foreach (var token in new[]
                     {
                         "${AOVIAS_CONFIG_ROOT}",
                         "${AOVIAS_LOGS_ROOT}",
                         "${AOVIAS_DEPLOYMENTS_ROOT}",
                         "${AOVIAS_DOCKER_NETWORK}"
                     })
            {
                if (!context.ComposeContent.Contains(token, StringComparison.Ordinal))
                {
                    errors.Add($"{DeployPackageLayout.ComposeFile} must reference '{token}'.");
                }
            }
        }

        if (!string.IsNullOrWhiteSpace(context.ConfigurationTemplateJson))
        {
            errors.AddRange(EnvironmentNeutralRules.ScanPackageArtifacts(
                context.ConfigurationTemplateJson,
                DeployPackageLayout.ConfigTemplateFile));
        }

        return errors;
    }
}
