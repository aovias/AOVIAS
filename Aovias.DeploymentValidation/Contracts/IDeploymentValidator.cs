using Aovias.DeploymentValidation.Models;
using Aovias.Publishing.Contracts;

namespace Aovias.DeploymentValidation.Contracts;

/// <summary>
/// Orchestrates deployment readiness validation for published packages.
/// </summary>
public interface IDeploymentValidator
{
    Task<DeploymentReadinessReport> ValidateAsync(
        DeploymentValidationContext context,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Individual deployment readiness validation stage.
/// </summary>
public interface IDeploymentValidationStage
{
    string StageName { get; }

    ValidationStageDescriptor Descriptor { get; }

    Task<ValidationStageResult> ValidateAsync(
        DeploymentValidationContext context,
        CancellationToken cancellationToken = default);
}
