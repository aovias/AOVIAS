namespace Aovias.DeploymentValidation.Models;

/// <summary>
/// Controls how much deployment simulation DRV performs.
/// </summary>
public enum DeploymentValidationMode
{
    /// <summary>No runtime simulation — package and image compatibility only.</summary>
    Fast = 0,

    /// <summary>Sequential simulation without full container startup.</summary>
    Balanced = 1,

    /// <summary>Complete validation including compose up and health probes.</summary>
    Full = 2
}
