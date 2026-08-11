using ComplianceService.Domain;

namespace ComplianceService.Application.Contracts;

public interface IControlRepository : IRepository<Control>
{
    Task<List<Control>> GetByProgramIdAsync(Guid programId);
}
