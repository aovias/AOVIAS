using Microsoft.EntityFrameworkCore;
using OrganizationService.Infrastructure.Persistence;
using Shared.Infrastructure.Domain;

namespace OrganizationService.Application.Services;

/// <summary>
/// Catalogue CRUD for SubscriptionPlan / OrganizationSubscription metadata.
/// CRP Phase 6 — not operational authority; use LicenseAppService for commercial rights.
/// </summary>
public class SubscriptionAppService
{
    private readonly OrganizationDbContext _dbContext;
    private readonly LicenseAppService? _licenses;
    private readonly ILogger<SubscriptionAppService>? _logger;

    public SubscriptionAppService(OrganizationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public SubscriptionAppService(
        OrganizationDbContext dbContext,
        LicenseAppService licenses,
        ILogger<SubscriptionAppService> logger)
    {
        _dbContext = dbContext;
        _licenses = licenses;
        _logger = logger;
    }

    public async Task<OrganizationSubscription?> GetCurrentSubscriptionAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.OrganizationSubscriptions
            .Where(x => x.OrganizationId == organizationId && x.IsActive)
            .OrderByDescending(x => x.StartsAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <summary>Dual-read: prefer catalogue row; if missing, attempt sync from license then re-read.</summary>
    public async Task<object?> GetCurrentCatalogueOrLicenseMirrorAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        var current = await GetCurrentSubscriptionAsync(organizationId, cancellationToken);
        if (current != null)
        {
            return current;
        }

        if (_licenses == null)
        {
            return null;
        }

        try
        {
            var key = await _licenses.FindOperationalCommercialKeyAsync(organizationId, cancellationToken);
            if (key != null)
            {
                await _licenses.SyncCatalogFromLicenseAsync(key, cancellationToken);
                current = await GetCurrentSubscriptionAsync(organizationId, cancellationToken);
                if (current != null)
                {
                    return current;
                }

                var summary = await _licenses.GetLicensingSummaryAsync(organizationId, cancellationToken);
                return new
                {
                    id = Guid.Empty,
                    organizationId,
                    subscriptionPlanId = summary.License?.SubscriptionPlanId ?? Guid.Empty,
                    startsAt = DateTime.UtcNow,
                    endsAt = summary.License?.ExpiresAtUtc,
                    isTrial = string.Equals(summary.License?.LicenseType, "Trial", StringComparison.OrdinalIgnoreCase),
                    isActive = summary.License?.IsOperational ?? false,
                    currentUserCount = summary.License?.UsedSeats ?? 0,
                    currentFrameworkCount = 0,
                    source = "license-mirror",
                    deprecated = true,
                    message = "Catalogue row missing; mirrored from LicenseKey (SSOT)."
                };
            }
        }
        catch (Exception ex)
        {
            _logger?.LogDebug(ex, "Dual-read catalogue enrichment failed for {Org}", organizationId);
        }

        return null;
    }

    /// <summary>Catalogue metadata assign only — does not activate or issue a LicenseKey.</summary>
    public async Task<OrganizationSubscription> AssignSubscriptionAsync(
        Guid organizationId,
        Guid subscriptionPlanId,
        bool isTrial,
        CancellationToken cancellationToken = default)
    {
        // Deactivate existing active subscriptions for this organization
        var existing = await _dbContext.OrganizationSubscriptions
            .Where(x => x.OrganizationId == organizationId && x.IsActive)
            .ToListAsync(cancellationToken);

        foreach (var sub in existing)
        {
            sub.IsActive = false;
            sub.EndsAt = DateTime.UtcNow;
        }

        // Validate plan exists and is published catalogue
        var plan = await _dbContext.SubscriptionPlans
            .FirstOrDefaultAsync(x => x.Id == subscriptionPlanId && x.IsActive, cancellationToken);

        if (plan == null)
        {
            throw new InvalidOperationException("Subscription plan not found or inactive.");
        }

        var now = DateTime.UtcNow;

        var newSub = new OrganizationSubscription
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            SubscriptionPlanId = subscriptionPlanId,
            StartsAt = now,
            IsTrial = isTrial,
            IsActive = true,
            CurrentUserCount = 0,
            CurrentFrameworkCount = 0,
            CreatedAt = now
        };

        await _dbContext.OrganizationSubscriptions.AddAsync(newSub, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger?.LogInformation(
            "Catalogue OrganizationSubscription {Id} assigned for org {Org} (metadata only; LicenseKey remains SSOT)",
            newSub.Id, organizationId);

        return newSub;
    }

    public async Task<OrganizationSubscription> UpgradeSubscriptionAsync(
        Guid organizationId,
        Guid targetPlanId,
        CancellationToken cancellationToken = default)
    {
        // Catalogue plan change only — does not convert or reissue LicenseKey.
        return await AssignSubscriptionAsync(organizationId, targetPlanId, isTrial: false, cancellationToken);
    }
}

