using ComplianceService.Application.Contracts;
using ComplianceService.Domain;
using ComplianceService.Infrastructure.Persistence;

namespace ComplianceService.Infrastructure.Repositories;

public class ComplianceProgramRepository : Repository<ComplianceProgram>, IComplianceProgramRepository
{
    public ComplianceProgramRepository(ComplianceDbContext db, IUserContextService? userContext = null) 
        : base(db, userContext) { }
}
