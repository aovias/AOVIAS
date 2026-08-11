using ComplianceService.Domain;

namespace ComplianceService.Application.Contracts;

public interface IHospitalComplianceRepository
{
    Task<HospitalComplianceAggregate> LoadAsync(Guid hospitalId, Guid frameworkId, CancellationToken ct);
    Task SaveAsync(HospitalComplianceAggregate aggregate, CancellationToken ct);
}
