namespace Aovias.DeploymentValidation.Exceptions;

/// <summary>
/// Raised when deployment readiness validation cannot continue.
/// </summary>
public sealed class DeploymentValidationException : InvalidOperationException
{
    public DeploymentValidationException(string message)
        : base(message)
    {
    }

    public DeploymentValidationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
