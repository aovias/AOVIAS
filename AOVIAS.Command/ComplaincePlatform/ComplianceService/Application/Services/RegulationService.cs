using ComplianceService.Application.Contracts;
using ComplianceService.Application.DTOs;
using ComplianceService.Domain;
using ComplianceService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Services;

namespace ComplianceService.Application.Services;

public class RegulationService : IRegulationService
{
    private readonly ComplianceDbContext _dbContext;
    private readonly IPlatformIdGenerator _idGenerator;
    private readonly IUserContextService _userContext;

    public RegulationService(ComplianceDbContext dbContext, IPlatformIdGenerator idGenerator, IUserContextService userContext)
    {
        _dbContext = dbContext;
        _idGenerator = idGenerator;
        _userContext = userContext;
    }

    public async Task<Guid> EnrollAsync(EnrollRegulationRequest request, CancellationToken ct)
    {
        var hospitalId = request.HospitalId ?? _userContext.HospitalId;
        
        var exists = await _dbContext.Regulations
            .AnyAsync(r => r.HospitalId == hospitalId 
                && r.ComplianceFrameworkId == request.ComplianceFrameworkId, ct);

        if (exists)
        {
            throw new InvalidOperationException(
                $"A regulation already exists for HospitalId {hospitalId} and ComplianceFrameworkId {request.ComplianceFrameworkId}");
        }

        var regulation = new Regulation
        {
            Id = Guid.NewGuid(),
            PlatformId = await _idGenerator.GenerateAsync("REG", ct),
            HospitalId = hospitalId,
            ComplianceFrameworkId = request.ComplianceFrameworkId,
            Status = "Planned",
            StartDate = DateTime.UtcNow,
            TargetDate = request.TargetDate,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Regulations.Add(regulation);
        await _dbContext.SaveChangesAsync(ct);

        return regulation.Id;
    }

    public async Task<Regulation> CreateAsync(Regulation regulation)
    {
        await _dbContext.Regulations.AddAsync(regulation);
        await _dbContext.SaveChangesAsync();
        return regulation;
    }

    public async Task<List<Regulation>> GetByHospitalAsync(Guid hospitalId)
    {
        return await _dbContext.Regulations
            .Where(r => r.HospitalId == hospitalId)
            .ToListAsync();
    }
}
