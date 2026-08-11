using Shared.Infrastructure.DTO;

namespace ComplianceService.Application.Contracts;

/// <summary>
/// Service interface for framework query operations.
/// </summary>
public interface IFrameworkQueryService
{
    /// <summary>
    /// Gets all active compliance frameworks.
    /// </summary>
    Task<List<FrameworkDto>> GetAllFrameworksAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Gets frameworks where a hospital is actively enrolled.
    /// </summary>
    Task<List<HospitalFrameworkDto>> GetHospitalFrameworksAsync(Guid hospitalId, CancellationToken cancellationToken);
}
