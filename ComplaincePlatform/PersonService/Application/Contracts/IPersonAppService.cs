using PersonService.Application.DTOs;
using PersonService.Domain.Entities;
using Shared.Infrastructure.DTO;

namespace PersonService.Application.Contracts;

public interface IPersonAppService
{
    Task<InternalCreatePersonResponse> CreateAsync(CreatePersonRequest request, CancellationToken cancellationToken = default);
    Task<IntegrationUpsertResponse> UpsertFromIntegrationAsync(IntegrationUpsertRequest request, CancellationToken cancellationToken = default);
    Task<bool> RollbackIntegrationAsync(Guid organizationId, Guid personId, CancellationToken cancellationToken = default);
    Task<PersonDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PagedResultDto<PersonListItemDto>> ListAsync(int page, int pageSize, string? status, CancellationToken cancellationToken = default);
    Task<PagedResultDto<PersonListItemDto>> SearchAsync(string query, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<PersonDto?> UpdateAsync(Guid id, UpdatePersonRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, Guid? actorUserId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PersonContactDto>> GetContactsAsync(Guid personId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PersonAddressDto>> GetAddressesAsync(Guid personId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PersonTimelineDto>> GetTimelineAsync(Guid personId, CancellationToken cancellationToken = default);
    Task<PersonDocumentDto?> AddDocumentAsync(Guid personId, PersonDocumentDto request, CancellationToken cancellationToken = default);
    Task<bool> RemoveDocumentAsync(Guid personId, Guid documentId, Guid? actorUserId, CancellationToken cancellationToken = default);
    Task<bool> UploadPhotoAsync(Guid personId, Stream fileStream, string fileName, string contentType, Guid? actorUserId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LookupValueDto>> GetLookupsAsync(string categoryCode, CancellationToken cancellationToken = default);
    Task<bool> HasDuplicateEmailAsync(string email, Guid? excludePersonId, CancellationToken cancellationToken = default);
}

public interface IPersonAuditService
{
    Task RecordAsync(Guid personId, string eventCode, string action, Guid? actorUserId, string? oldValue, string? newValue, string? correlationId, CancellationToken cancellationToken = default);
    Task RecordTimelineAsync(Guid personId, string eventType, string summary, Guid? actorUserId, string? oldJson, string? newJson, string? correlationId, CancellationToken cancellationToken = default);
}
