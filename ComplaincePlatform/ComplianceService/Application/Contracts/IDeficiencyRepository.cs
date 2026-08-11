using ComplianceService.Domain;
using ComplianceService.Domain.Enums;

namespace ComplianceService.Application.Contracts;

public interface IDeficiencyRepository
{
    Task AddAsync(ComplianceDeficiency deficiency, CancellationToken ct = default);

    /// <summary>
    /// Finds an open (non-Closed) deficiency for the given obligation and type.
    /// Returns null when no match exists.
    /// </summary>
    Task<ComplianceDeficiency?> FindOpenAsync(
        Guid obligationId,
        DeficiencyType type,
        CancellationToken ct = default);

    Task<IReadOnlyList<ComplianceDeficiency>> GetByHospitalFrameworkAsync(
        Guid hospitalId,
        Guid frameworkId,
        CancellationToken ct = default);

    Task<IReadOnlyList<ComplianceDeficiency>> GetOpenByObligationAsync(
        Guid obligationId,
        CancellationToken ct = default);

    Task<ComplianceDeficiency?> GetByIdAsync(Guid id, CancellationToken ct = default);
}
