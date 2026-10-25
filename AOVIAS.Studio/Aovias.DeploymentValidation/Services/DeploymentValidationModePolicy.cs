namespace Aovias.DeploymentValidation.Services;

/// <summary>
/// Resolves which DRV stages run for a validation mode.
/// </summary>
public static class DeploymentValidationModePolicy
{
    public static bool ShouldRunStage(string stageName, Models.DeploymentValidationMode mode)
        => mode switch
        {
            Models.DeploymentValidationMode.Fast => stageName is "Package" or "Contracts" or "Compatibility" or "Rollback" or "Environment Neutral",
            Models.DeploymentValidationMode.Balanced => stageName is "Package" or "Contracts" or "Compatibility" or "Docker Compose" or "Bootstrap Manifest" or "Configuration" or "Rollback" or "Environment Neutral",
            _ => true
        };
}
