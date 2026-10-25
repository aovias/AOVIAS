namespace ComplianceServiceManager.Services;

public interface IGitSafetyService
{
    Task<string> CreateCheckpointAsync(string repositoryPath, CancellationToken cancellationToken);

    Task RollbackAsync(string repositoryPath, string checkpointReference, CancellationToken cancellationToken);

    Task<IReadOnlyList<string>> GetModifiedFilesAsync(string repositoryPath, CancellationToken cancellationToken);
}
