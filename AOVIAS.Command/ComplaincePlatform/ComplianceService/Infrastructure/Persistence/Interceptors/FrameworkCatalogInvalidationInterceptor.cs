using ComplianceService.Application.Journey;
using ComplianceService.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace ComplianceService.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Invalidates cached framework catalog snapshots whenever master-structure entities change.
/// </summary>
public sealed class FrameworkCatalogInvalidationInterceptor : SaveChangesInterceptor
{
    private readonly IFrameworkCatalogCache _catalogCache;

    public FrameworkCatalogInvalidationInterceptor(IFrameworkCatalogCache catalogCache)
    {
        _catalogCache = catalogCache;
    }

    public override ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        InvalidateIfNeeded(eventData.Context);
        return base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        InvalidateIfNeeded(eventData.Context);
        return base.SavedChanges(eventData, result);
    }

    private void InvalidateIfNeeded(DbContext? dbContext)
    {
        if (dbContext is null)
        {
            return;
        }

        var entries = dbContext.ChangeTracker.Entries()
            .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .ToList();
        if (entries.Count == 0)
        {
            return;
        }

        var frameworks = new HashSet<Guid>();
        var touchedGlobal = false;

        foreach (var entry in entries)
        {
            switch (entry.Entity)
            {
                case ComplianceFramework framework:
                    frameworks.Add(framework.Id);
                    break;
                case ComplianceChapter chapter:
                    frameworks.Add(chapter.FrameworkId);
                    break;
                case ComplianceStandard:
                case ObjectiveElement:
                    // These entities do not always carry framework id directly.
                    touchedGlobal = true;
                    break;
            }
        }

        if (touchedGlobal)
        {
            _catalogCache.InvalidateAll();
            return;
        }

        foreach (var frameworkId in frameworks)
        {
            _catalogCache.InvalidateFramework(frameworkId);
        }
    }
}

