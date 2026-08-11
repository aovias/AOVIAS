using ComplianceService.Application.Contracts;
using ComplianceService.Application.DTOs;
using ComplianceService.Domain;
using ComplianceService.Domain.Enums;
using ComplianceService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ComplianceService.Application.Services;

/// <summary>
/// Catalog lifecycle for compliance frameworks (Draft → Published → Active → Retired → Archived).
/// Extends existing <see cref="ComplianceFramework"/>; not Framework Studio.
/// </summary>
public sealed class FrameworkLifecycleService
{
    private readonly ComplianceDbContext _db;
    private readonly ILogger<FrameworkLifecycleService> _logger;
    private readonly IComplianceNotificationPublisher _notifications;

    public FrameworkLifecycleService(
        ComplianceDbContext db,
        ILogger<FrameworkLifecycleService> logger,
        IComplianceNotificationPublisher notifications)
    {
        _db = db;
        _logger = logger;
        _notifications = notifications;
    }

    public async Task<IReadOnlyList<FrameworkDto>> ListCatalogAsync(
        bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        var query = _db.ComplianceFrameworks.AsNoTracking().AsQueryable();

        if (!includeInactive)
        {
            query = query.Where(f => f.Status == FrameworkCatalogStatus.Active);
        }

        var rows = await query
            .OrderBy(f => f.Name)
            .ThenBy(f => f.Version)
            .ToListAsync(cancellationToken);

        return rows.Select(Map).ToList();
    }

    public async Task<FrameworkDto> PublishAsync(Guid frameworkId, CancellationToken cancellationToken = default)
    {
        var framework = await LoadMutableAsync(frameworkId, cancellationToken);
        EnsureNotArchived(framework);
        EnsureTransition(framework, FrameworkCatalogStatus.Draft, FrameworkCatalogStatus.Published);

        framework.Status = FrameworkCatalogStatus.Published;
        framework.PublishedAt ??= DateTime.UtcNow;
        framework.SyncIsActiveFromStatus();

        await _db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Framework {FrameworkId} published", frameworkId);
        return Map(framework);
    }

    public async Task<FrameworkDto> ActivateAsync(Guid frameworkId, CancellationToken cancellationToken = default)
    {
        var framework = await LoadMutableAsync(frameworkId, cancellationToken);
        EnsureNotArchived(framework);

        if (framework.Status is not (FrameworkCatalogStatus.Published or FrameworkCatalogStatus.Retired))
        {
            throw new InvalidOperationException(
                $"Cannot activate framework in status {framework.Status}. Expected Published or Retired.");
        }

        // Single active version per framework family (Name)
        var siblings = await _db.ComplianceFrameworks
            .Where(f => f.Name == framework.Name
                        && f.Id != framework.Id
                        && f.Status == FrameworkCatalogStatus.Active)
            .ToListAsync(cancellationToken);

        var now = DateTime.UtcNow;
        foreach (var sibling in siblings)
        {
            sibling.Status = FrameworkCatalogStatus.Retired;
            sibling.RetiredAt ??= now;
            sibling.SyncIsActiveFromStatus();
            _logger.LogInformation(
                "Retired sibling framework {SiblingId} ({Name} {Version}) to keep single active version",
                sibling.Id, sibling.Name, sibling.Version);
        }

        framework.Status = FrameworkCatalogStatus.Active;
        framework.PublishedAt ??= now;
        framework.RetiredAt = null;
        framework.SyncIsActiveFromStatus();

        await _db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Framework {FrameworkId} activated", frameworkId);

        await _notifications.PublishAsync(new ComplianceNotificationMessage
        {
            EventType = "framework.activated",
            Title = "Framework activated",
            Body = $"{framework.Name} {framework.Version} is now Active (single-active-version enforced).",
            Data = new Dictionary<string, string>
            {
                ["frameworkId"] = framework.Id.ToString(),
                ["name"] = framework.Name,
                ["version"] = framework.Version ?? string.Empty,
                ["status"] = framework.Status.ToString(),
            },
        }, cancellationToken);

        return Map(framework);
    }

    public async Task<FrameworkDto> RetireAsync(Guid frameworkId, CancellationToken cancellationToken = default)
    {
        var framework = await LoadMutableAsync(frameworkId, cancellationToken);
        EnsureNotArchived(framework);

        if (framework.Status is not (FrameworkCatalogStatus.Active or FrameworkCatalogStatus.Published))
        {
            throw new InvalidOperationException(
                $"Cannot retire framework in status {framework.Status}. Expected Active or Published.");
        }

        framework.Status = FrameworkCatalogStatus.Retired;
        framework.RetiredAt ??= DateTime.UtcNow;
        framework.SyncIsActiveFromStatus();

        await _db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Framework {FrameworkId} retired", frameworkId);
        return Map(framework);
    }

    public async Task<FrameworkDto> ArchiveAsync(Guid frameworkId, CancellationToken cancellationToken = default)
    {
        var framework = await LoadMutableAsync(frameworkId, cancellationToken);
        EnsureNotArchived(framework);

        if (framework.Status != FrameworkCatalogStatus.Retired)
        {
            throw new InvalidOperationException(
                $"Cannot archive framework in status {framework.Status}. Expected Retired.");
        }

        framework.Status = FrameworkCatalogStatus.Archived;
        framework.ArchivedAt ??= DateTime.UtcNow;
        framework.SyncIsActiveFromStatus();

        await _db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Framework {FrameworkId} archived (immutable)", frameworkId);
        return Map(framework);
    }

    private async Task<ComplianceFramework> LoadMutableAsync(Guid frameworkId, CancellationToken cancellationToken)
    {
        if (frameworkId == Guid.Empty)
        {
            throw new ArgumentException("FrameworkId is required.", nameof(frameworkId));
        }

        return await _db.ComplianceFrameworks
                   .FirstOrDefaultAsync(f => f.Id == frameworkId, cancellationToken)
               ?? throw new InvalidOperationException($"Framework {frameworkId} not found.");
    }

    private static void EnsureNotArchived(ComplianceFramework framework)
    {
        if (framework.Status == FrameworkCatalogStatus.Archived)
        {
            throw new InvalidOperationException(
                $"Framework {framework.Id} is Archived and immutable.");
        }
    }

    private static void EnsureTransition(
        ComplianceFramework framework,
        FrameworkCatalogStatus expected,
        FrameworkCatalogStatus target)
    {
        if (framework.Status != expected)
        {
            throw new InvalidOperationException(
                $"Cannot transition framework from {framework.Status} to {target}. Expected {expected}.");
        }
    }

    private static FrameworkDto Map(ComplianceFramework f) => new()
    {
        FrameworkId = f.Id,
        Name = f.Name,
        Authority = f.Authority ?? string.Empty,
        Code = f.PlatformId,
        Version = f.Version ?? string.Empty,
        ValidityPeriodMonths = 12,
        IsActive = f.IsActive,
        Status = f.Status.ToString(),
        PublishedAt = f.PublishedAt,
        RetiredAt = f.RetiredAt,
        ArchivedAt = f.ArchivedAt
    };
}
