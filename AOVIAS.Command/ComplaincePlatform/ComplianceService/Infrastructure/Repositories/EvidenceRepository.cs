using ComplianceService.Application.Contracts;
using ComplianceService.Domain;
using ComplianceService.Infrastructure.Persistence;

namespace ComplianceService.Infrastructure.Repositories;

public class EvidenceRepository : Repository<Evidence>, IEvidenceRepository
{
    public EvidenceRepository(ComplianceDbContext db, IUserContextService? userContext = null) 
        : base(db, userContext) { }
}
