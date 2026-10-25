using ComplianceService.Domain;

namespace ComplianceService.Application.Contracts;

public interface IComplianceTaskRepository : IRepository<ComplianceTask>
{
    Task<List<ComplianceTask>> GetByProgramIdAsync(Guid programId);
}
