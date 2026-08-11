using EnterpriseDirectoryService.Application.Contracts;

namespace EnterpriseDirectoryService.Application.Directory.Platform;

public interface IDirectoryProjectionHandler
{
    string EventType { get; }
    Task HandleAsync(DirectoryEventEnvelope envelope, CancellationToken ct = default);
}

public interface IDirectoryProjectionRegistry
{
    Task ProcessAsync(DirectoryEventEnvelope envelope, CancellationToken ct = default);
}

public interface IDirectorySearchEngine
{
    Task<DirectorySearchResultDto> SearchAsync(DirectoryQuery query, GovernedDirectoryContext auth, CancellationToken ct = default);
    Task<IReadOnlyList<DirectorySuggestionDto>> SuggestAsync(string prefix, GovernedDirectoryContext auth, Guid organizationId, CancellationToken ct = default);
    Task<IReadOnlyDictionary<string, IReadOnlyList<FacetValueDto>>> GetFacetsAsync(GovernedDirectoryContext auth, Guid organizationId, CancellationToken ct = default);
}

public interface IDirectoryIndexProjector
{
    Task<int> RebuildOrganizationAsync(Guid organizationId, string correlationId, CancellationToken ct = default);
    Task HandlePersonUpdatedAsync(Guid personId, string payload, CancellationToken ct = default);
    Task HandleEmploymentEventAsync(string eventType, Guid employmentId, string payload, CancellationToken ct = default);
    Task HandleProfessionalEventAsync(Guid profileId, string payload, CancellationToken ct = default);
}

public interface IDirectoryOutboxWriter
{
    Task EnqueueAsync(string type, Guid aggregateId, object payload, CancellationToken ct = default);
}
