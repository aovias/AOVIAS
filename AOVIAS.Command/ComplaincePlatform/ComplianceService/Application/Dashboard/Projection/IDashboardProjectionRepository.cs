using ComplianceService.Domain;

namespace ComplianceService.Application.Dashboard.Projection;

public interface IDashboardProjectionRepository
{
    Task<DashboardProjection?> GetAsync(
        DashboardProjectionScope scope,
        CancellationToken cancellationToken = default);

    Task<DashboardProjection> GetOrCreateAsync(
        DashboardProjectionScope scope,
        CancellationToken cancellationToken = default);

    Task SaveAsync(DashboardProjection projection, CancellationToken cancellationToken = default);

    Task MarkCorruptedAsync(
        DashboardProjectionScope scope,
        string reason,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DashboardProjection>> ListByHospitalAsync(
        Guid organizationId,
        Guid hospitalId,
        CancellationToken cancellationToken = default);
}
