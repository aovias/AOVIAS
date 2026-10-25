namespace Aovias.Configuration.Deployment;

/// <summary>
/// Single source of truth for every deployment and runtime filesystem location (Sprint 11.5).
/// </summary>
public interface IDeploymentPathResolver
{
    string DeploymentRoot { get; }

    string IncomingRoot { get; }

    string ReleaseRoot { get; }

    string TempRoot { get; }

    string ArchiveRoot { get; }

    string StagingRoot { get; }

    string ExtractionRoot { get; }

    string ActiveRoot { get; }

    string RollbackRoot { get; }

    string AuditRoot { get; }

    string RuntimeRoot { get; }

    string ConfigRoot { get; }

    string LogsRoot { get; }

    string PackagesRoot { get; }

    string CertificatesRoot { get; }

    string ProfilesRoot { get; }

    string SecretsRoot { get; }

    string ResolveUnderDeploymentRoot(string relativeSegment);

    void EnsureDirectory(string path);

    DeploymentStorageValidationResult ValidateStorageLayout(bool createMissing = false);

    DeploymentStorageValidationResult ValidateWritable(string path, string label, string phase, string operation);
}
