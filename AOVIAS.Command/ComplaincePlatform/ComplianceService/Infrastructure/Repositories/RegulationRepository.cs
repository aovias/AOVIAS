using ComplianceService.Application.Contracts;
using ComplianceService.Domain;
using ComplianceService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ComplianceService.Infrastructure.Repositories;

public class RegulationRepository : Repository<Regulation>, IRegulationRepository
{
    public RegulationRepository(ComplianceDbContext db, IUserContextService? userContext = null) 
        : base(db, userContext) { }

    public async Task<List<Regulation>> GetByHospitalAsync(Guid hospitalId)
    {
        var query = _db.Regulations.AsQueryable();
        
        // Apply hospital filter
        query = ApplyHospitalFilter(query);
        
        return await query
            .Where(x => x.HospitalId == hospitalId)
            .ToListAsync();
    }
}
