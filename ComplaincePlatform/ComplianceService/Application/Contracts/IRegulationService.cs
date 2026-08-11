using ComplianceService.Application.DTOs;

namespace ComplianceService.Application.Contracts;

public interface IRegulationService
{
    Task<Guid> EnrollAsync(EnrollRegulationRequest request, CancellationToken ct);
}
