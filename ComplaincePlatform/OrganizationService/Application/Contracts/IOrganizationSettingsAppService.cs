using OrganizationService.Application.DTOs;

namespace OrganizationService.Application.Contracts;

public interface IOrganizationSettingsAppService
{
    Task<OrganizationSettingsDto?> GetAsync(Guid organizationId, Guid actorUserId, bool canEdit, CancellationToken ct = default);
    Task<OrganizationSettingsDto> UpdateAsync(Guid organizationId, UpdateOrganizationSettingsRequest request, Guid actorUserId, CancellationToken ct = default);
    Task<OrganizationSettingsDto> UploadLogoAsync(Guid organizationId, Stream fileStream, string fileName, string contentType, long length, Guid actorUserId, CancellationToken ct = default);
    Task<OrganizationSettingsDto> RemoveLogoAsync(Guid organizationId, Guid actorUserId, CancellationToken ct = default);
    Task<(Stream Stream, string ContentType)?> OpenLogoAsync(Guid organizationId, CancellationToken ct = default);

    Task<IReadOnlyList<OrgHolidayDto>> ListHolidaysAsync(Guid organizationId, int? year = null, CancellationToken ct = default);
    Task<OrgHolidayDto> AddHolidayAsync(Guid organizationId, CreateOrgHolidayRequest request, Guid actorUserId, CancellationToken ct = default);
    Task<bool> RemoveHolidayAsync(Guid organizationId, Guid holidayId, CancellationToken ct = default);
    Task<BusinessCalendarDto?> GetBusinessCalendarAsync(Guid organizationId, int? year = null, CancellationToken ct = default);
}
