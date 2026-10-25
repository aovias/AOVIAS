using IdentityService.Application.DTOs;

namespace IdentityService.Application.Contracts;

public interface IOrganizationServiceClient
{
    Task<UserAuthContextDto> GetUserAuthContextAsync(
        Guid userId,
        Guid? hospitalId = null,
        CancellationToken cancellationToken = default);

    [Obsolete("Use GetUserAuthContextAsync")]
    Task<(Guid organizationId, Guid hospitalId)> GetUserOrganizationAndHospitalAsync(Guid userId);
}
