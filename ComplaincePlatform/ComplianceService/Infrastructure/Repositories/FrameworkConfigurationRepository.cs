using ComplianceService.Application.Contracts;
using ComplianceService.Domain;
using ComplianceService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ComplianceService.Infrastructure.Repositories;

public class FrameworkConfigurationRepository : IFrameworkConfigurationRepository
{
    private readonly ComplianceDbContext _dbContext;

    public FrameworkConfigurationRepository(ComplianceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<FrameworkConfiguration?> GetByFrameworkIdAsync(
        Guid frameworkId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.FrameworkConfigurations
            .Include(x => x.Framework)
            .FirstOrDefaultAsync(x => x.FrameworkId == frameworkId, cancellationToken);
    }

    public async Task<FrameworkConfiguration> GetOrCreateDefaultAsync(
        Guid frameworkId,
        CancellationToken cancellationToken = default)
    {
        var config = await GetByFrameworkIdAsync(frameworkId, cancellationToken);
        
        if (config == null)
        {
            var framework = await _dbContext.ComplianceFrameworks
                .AsNoTracking()
                .FirstOrDefaultAsync(f => f.Id == frameworkId, cancellationToken);
            if (framework == null)
            {
                throw new InvalidOperationException(
                    $"Cannot create default framework configuration: ComplianceFramework {frameworkId} was not found.");
            }

            if (string.IsNullOrWhiteSpace(framework.PlatformId))
            {
                throw new InvalidOperationException(
                    $"Cannot create default framework configuration: ComplianceFramework {frameworkId} has no PlatformId.");
            }

            config = new FrameworkConfiguration(
                platformId: framework.PlatformId,
                frameworkId: frameworkId,
                defaultValidityMonths: 12,
                minimumReadinessScore: 70m,
                renewalDaysBeforeExpiry: 90,
                reapplyWaitingDays: 30,
                majorNcResolutionDays: 60);

            await AddAsync(config, cancellationToken);
        }

        return config;
    }

    public async Task AddAsync(FrameworkConfiguration configuration, CancellationToken cancellationToken = default)
    {
        await _dbContext.FrameworkConfigurations.AddAsync(configuration, cancellationToken);
    }

    public async Task UpdateAsync(FrameworkConfiguration configuration, CancellationToken cancellationToken = default)
    {
        _dbContext.FrameworkConfigurations.Update(configuration);
        await Task.CompletedTask;
    }
}
