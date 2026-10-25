namespace Aovias.DeploymentValidation.Models;

/// <summary>
/// Determines which pipeline may execute a validation stage.
/// </summary>
public enum ValidationExecutionScope
{
    Publishing,
    Deployment,
    Both
}

/// <summary>
/// High-level validation category for operator diagnostics.
/// </summary>
public enum ValidationCategory
{
    Build,
    Package,
    Deployment,
    Runtime,
    Infrastructure
}

/// <summary>
/// Metadata declared by each validation stage.
/// </summary>
public sealed record ValidationStageDescriptor(
    ValidationCategory Category,
    ValidationExecutionScope ExecutionScope,
    bool IsBlocking = true,
    bool IsOptional = false);
