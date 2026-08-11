using ComplianceService.Domain;

namespace ComplianceService.Application.Contracts;

/// <summary>
/// Repository for FrameworkConfiguration.
/// </summary>
public interface IFrameworkConfigurationRepository
{
    Task<FrameworkConfiguration?> GetByFrameworkIdAsync(
        Guid frameworkId,
        CancellationToken cancellationToken = default);
    Task<FrameworkConfiguration> GetOrCreateDefaultAsync(
        Guid frameworkId,
        CancellationToken cancellationToken = default);
    Task AddAsync(FrameworkConfiguration configuration, CancellationToken cancellationToken = default);
    Task UpdateAsync(FrameworkConfiguration configuration, CancellationToken cancellationToken = default);
}
