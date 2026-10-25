using ComplianceService.Application.Contracts;
using ComplianceService.Domain;
using ComplianceService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ComplianceService.Infrastructure.Repositories;

public class ControlRepository : Repository<Control>, IControlRepository
{
    public ControlRepository(ComplianceDbContext db, IUserContextService? userContext = null) 
        : base(db, userContext) { }

    public async Task<List<Control>> GetByProgramIdAsync(Guid programId)
    {
        var query = _db.Controls.AsQueryable();
        
        // Apply hospital filter (Control doesn't have HospitalId, but we filter through Program)
        // For now, just filter by programId - hospital filtering happens at program level
        return await query
            .Where(x => x.ProgramId == programId)
            .ToListAsync();
    }
}
