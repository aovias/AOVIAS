using ComplianceService.Application.Contracts;
using ComplianceService.Infrastructure.Persistence;

namespace ComplianceService.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly ComplianceDbContext _db;

    public UnitOfWork(ComplianceDbContext db)
    {
        _db = db;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => 
        _db.SaveChangesAsync(cancellationToken);
}
