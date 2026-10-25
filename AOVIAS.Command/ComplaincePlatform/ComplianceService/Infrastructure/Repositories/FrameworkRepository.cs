using ComplianceService.Application.Contracts;
using Shared.Infrastructure.DTO;
using ComplianceService.Domain.Enums;
using ComplianceService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ComplianceService.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for framework read operations.
/// Contains all EF Core queries and database interactions.
/// </summary>
public class FrameworkRepository : IFrameworkRepository
{
    private readonly ComplianceDbContext _dbContext;
    private readonly ILogger<FrameworkRepository> _logger;

    public FrameworkRepository(
        ComplianceDbContext dbContext,
        ILogger<FrameworkRepository> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<List<FrameworkDto>> GetAllActiveFrameworksAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Repository: Retrieving all active frameworks from database");

        var frameworks = await _dbContext.ComplianceFrameworks
            .Where(f => f.IsActive)
            .OrderBy(f => f.Name)
            .ThenBy(f => f.Version)
            .Select(f => new FrameworkDto
            {
                FrameworkId = f.Id,
                Name = f.Name,
                Authority = f.Authority ?? string.Empty,
                Code = f.PlatformId, // Use PlatformId as Code
                Version = f.Version ?? string.Empty,
                ValidityPeriodMonths = 12, // Default, should come from FrameworkConfiguration
                IsActive = f.IsActive,
                Status = f.Status.ToString(),
                PublishedAt = f.PublishedAt,
                RetiredAt = f.RetiredAt,
                ArchivedAt = f.ArchivedAt
            })
            .ToListAsync(cancellationToken);

        _logger.LogInformation("Repository: Retrieved {Count} active frameworks from database", frameworks.Count);

        return frameworks;
    }

    public async Task<List<HospitalFrameworkDto>> GetHospitalFrameworksAsync(Guid hospitalId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Repository: Retrieving frameworks for hospital {HospitalId} from database", hospitalId);

        // Query from ComplianceSnapshot (Level 2 read model) joined with ComplianceFramework
        // Filter by JourneyState: Only include InProgress and Certified (Approved)
        // Exclude Expired (and all other states)
        var hospitalFrameworks = await (
            from snapshot in _dbContext.ComplianceSnapshots
            join framework in _dbContext.ComplianceFrameworks
                on snapshot.FrameworkId equals framework.Id
            where snapshot.HospitalId == hospitalId
                && (snapshot.JourneyState == JourneyState.InProgress 
                    || snapshot.JourneyState == JourneyState.Certified) // Approved = Certified
                && framework.IsActive
            orderby framework.Name, framework.Version
                   select new HospitalFrameworkDto
                   {
                       FrameworkId = framework.Id,
                       FrameworkName = framework.Name,
                       Status = snapshot.JourneyState.ToString(),
                       ExpiryDate = null, // Not available in current schema - would need to be calculated or stored
                       EnrolledOn = snapshot.CalculatedAt // Use calculated date as proxy for enrollment
                   })
            .ToListAsync(cancellationToken);

        _logger.LogInformation("Repository: Retrieved {Count} frameworks for hospital {HospitalId} from database", 
            hospitalFrameworks.Count, hospitalId);

        return hospitalFrameworks;
    }
}
