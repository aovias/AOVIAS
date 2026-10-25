using ComplianceService.Domain;

namespace ComplianceService.Application.Contracts;

public interface IComplianceProgramRepository : IRepository<ComplianceProgram>
{
    // Base IRepository<T> provides:
    // - Task<T?> GetAsync(Guid id)
    // - Task AddAsync(T entity)
    // - void Update(T entity)
    // - void Remove(T entity)
    // - IQueryable<T> Query()
}
