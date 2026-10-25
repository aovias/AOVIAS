using ComplianceService.Domain;

namespace ComplianceService.Application.Contracts;

public interface IRegulationRepository : IRepository<Regulation>
{
    Task<List<Regulation>> GetByHospitalAsync(Guid hospitalId);
}
