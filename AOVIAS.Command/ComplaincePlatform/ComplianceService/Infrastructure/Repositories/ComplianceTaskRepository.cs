using ComplianceService.Application.Contracts;
using ComplianceService.Domain;
using ComplianceService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ComplianceService.Infrastructure.Repositories;

public class ComplianceTaskRepository : Repository<ComplianceTask>, IComplianceTaskRepository
{
    public ComplianceTaskRepository(ComplianceDbContext db, IUserContextService? userContext = null) 
        : base(db, userContext) { }

    public async Task<List<ComplianceTask>> GetByProgramIdAsync(Guid programId)
    {
        var query = _db.Set<ComplianceTask>().AsQueryable();
        
        // Apply hospital filter
        query = ApplyHospitalFilter(query);
        
        var program = await _db.Set<ComplianceProgram>().FindAsync(programId);
        if (program == null)
        {
            return new List<ComplianceTask>();
        }

        return await query
            .Where(x => x.HospitalId == program.HospitalId)
            .ToListAsync();
    }
}
