namespace AIOrchestratorService.Infrastructure.Clients;

public interface IComplianceClient
{
    Task<Guid> CreateProgramAsync(string regulation, Guid organizationId);
    Task LoadControlsAsync(Guid programId, string regulation);
    Task AssignTasksAsync(Guid programId, Guid organizationId);

    /// <summary>Safe READ-only tool execution against ComplianceService internal AI tools API.</summary>
    Task<IReadOnlyDictionary<string, string>> ExecuteReadOnlyToolAsync(
        Guid hospitalId,
        Guid? organizationId,
        Guid? frameworkId,
        string toolName,
        IReadOnlyDictionary<string, string>? parameters = null,
        CancellationToken cancellationToken = default);
}
