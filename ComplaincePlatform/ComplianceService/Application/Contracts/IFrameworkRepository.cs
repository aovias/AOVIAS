using Shared.Infrastructure.DTO;
using ComplianceService.Domain;

namespace ComplianceService.Application.Contracts;

/// <summary>
/// Repository interface for framework read operations.
/// </summary>
public interface IFrameworkRepository
{
    /// <summary>
    /// Gets all active compliance frameworks.
    /// </summary>
    Task<List<FrameworkDto>> GetAllActiveFrameworksAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Gets frameworks where a hospital is actively enrolled.
    /// Returns only InProgress and Certified (Approved) frameworks.
    /// </summary>
    Task<List<HospitalFrameworkDto>> GetHospitalFrameworksAsync(Guid hospitalId, CancellationToken cancellationToken);
}
