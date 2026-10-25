using Microsoft.EntityFrameworkCore;
using OrganizationService.Application.Contracts;
using OrganizationService.Infrastructure.Persistence;
using Shared.Infrastructure.Domain;

namespace OrganizationService.Infrastructure.Repositories;

public sealed class HospitalMasterDepartmentRepository : IHospitalMasterDepartmentRepository
{
    private readonly OrganizationDbContext _db;

    public HospitalMasterDepartmentRepository(OrganizationDbContext db)
    {
        _db = db;
    }

    public async Task ReplaceHospitalSelectionsAsync(Guid hospitalId, IReadOnlyList<Guid> masterDepartmentIds, CancellationToken cancellationToken = default)
    {
        var existing = await _db.HospitalMasterDepartments
            .Where(x => x.HospitalId == hospitalId)
            .ToListAsync(cancellationToken);

        _db.HospitalMasterDepartments.RemoveRange(existing);

        foreach (var mid in masterDepartmentIds.Distinct())
        {
            _db.HospitalMasterDepartments.Add(new HospitalMasterDepartment
            {
                HospitalId = hospitalId,
                MasterDepartmentId = mid
            });
        }
    }
}
