using ComplianceService.Application.Contracts;
using ComplianceService.Application.DTOs;
using ComplianceService.Domain;
using ComplianceService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Domain.Enums;

namespace ComplianceService.Application.Services;

/// <summary>
/// Service for enterprise audit and compliance reporting.
/// Provides read-only access to journey history and transition audit reports.
/// </summary>
public class ComplianceAuditService
{
    private readonly IComplianceJourneyRepository _journeyRepository;
    private readonly IComplianceJourneyHistoryRepository _historyRepository;
    private readonly IFrameworkConfigurationRepository _frameworkConfigRepository;
    private readonly ComplianceDbContext _dbContext;
    private readonly ILogger<ComplianceAuditService> _logger;

    public ComplianceAuditService(
        IComplianceJourneyRepository journeyRepository,
        IComplianceJourneyHistoryRepository historyRepository,
        IFrameworkConfigurationRepository frameworkConfigRepository,
        ComplianceDbContext dbContext,
        ILogger<ComplianceAuditService> logger)
    {
        _journeyRepository = journeyRepository;
        _historyRepository = historyRepository;
        _frameworkConfigRepository = frameworkConfigRepository;
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Gets chronological transition history for a specific compliance journey.
    /// Enforces tenant isolation and returns history in ascending chronological order.
    /// </summary>
    public async Task<List<JourneyHistoryItemDto>> GetJourneyHistoryAsync(
        Guid journeyId,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        // First verify the journey exists and belongs to the tenant
        var journey = await _journeyRepository.GetByIdAsync(journeyId, cancellationToken);
        if (journey == null)
        {
            throw new InvalidOperationException($"Compliance journey {journeyId} not found");
        }

        // Enforce tenant isolation
        if (journey.TenantId != tenantId)
        {
            throw new UnauthorizedAccessException(
                $"Access denied: Journey {journeyId} does not belong to tenant {tenantId}");
        }

        // Get history entries for this journey
        var historyEntries = await _historyRepository.GetByJourneyIdAsync(journeyId, cancellationToken);

        // Map to DTOs (exclude SnapshotJson and TenantId)
        var historyItems = historyEntries.Select(h => new JourneyHistoryItemDto
        {
            From = h.OldStatus.ToString(),
            To = h.NewStatus.ToString(),
            ChangedBy = h.ChangedBy,
            Role = null, // Role not stored in history entity - would need to be added or extracted from SnapshotJson
            Remarks = h.Remarks,
            Timestamp = h.ChangedAt
        }).ToList();

        return historyItems;
    }

    /// <summary>
    /// Gets paginated transition audit report for a hospital and framework.
    /// Supports filtering by date range, states, and user.
    /// Enforces tenant isolation and validates hospital ownership.
    /// </summary>
    public async Task<PaginatedTransitionAuditDto> GetTransitionAuditReportAsync(
        Guid hospitalId,
        Guid frameworkId,
        Guid tenantId,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        ComplianceLifecycleState? fromState = null,
        ComplianceLifecycleState? toState = null,
        Guid? changedBy = null,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        // Validate pagination parameters
        if (pageNumber < 1)
        {
            throw new ArgumentException("Page number must be greater than 0", nameof(pageNumber));
        }

        if (pageSize < 1 || pageSize > 100)
        {
            throw new ArgumentException("Page size must be between 1 and 100", nameof(pageSize));
        }

        // Verify at least one journey exists for this hospital+framework+tenant combination
        // This ensures tenant isolation and validates hospital ownership
        var sampleJourney = await _dbContext.ComplianceJourneys
            .Where(j => j.HospitalId == hospitalId
                     && j.FrameworkId == frameworkId
                     && j.TenantId == tenantId)
            .FirstOrDefaultAsync(cancellationToken);

        if (sampleJourney == null)
        {
            // Return empty result rather than throwing - hospital might not have journeys yet
            return new PaginatedTransitionAuditDto
            {
                TotalCount = 0,
                PageNumber = pageNumber,
                PageSize = pageSize,
                Items = new List<TransitionAuditItemDto>()
            };
        }

        // Get paginated history entries
        var (historyEntries, totalCount) = await _historyRepository
            .GetTransitionsByHospitalAndFrameworkAsync(
                hospitalId,
                frameworkId,
                tenantId,
                fromDate,
                toDate,
                fromState,
                toState,
                changedBy,
                pageNumber,
                pageSize,
                cancellationToken);

        // Map to DTOs
        var items = historyEntries.Select(h => new TransitionAuditItemDto
        {
            JourneyId = h.ComplianceJourneyId,
            From = h.OldStatus.ToString(),
            To = h.NewStatus.ToString(),
            ChangedBy = h.ChangedBy,
            Role = null, // Role not stored in history entity
            Timestamp = h.ChangedAt
        }).ToList();

        return new PaginatedTransitionAuditDto
        {
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize,
            Items = items
        };
    }

    /// <summary>
    /// Gets hospital-level lifecycle summary with aggregated statistics.
    /// Uses read-optimized projection queries (no entity tracking).
    /// </summary>
    public async Task<JourneySummaryDto> GetJourneySummaryAsync(
        Guid hospitalId,
        Guid tenantId,
        Guid? frameworkId = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "GetJourneySummaryAsync called with HospitalId={HospitalId}, TenantId={TenantId}, FrameworkId={FrameworkId}",
            hospitalId, tenantId, frameworkId);

        // Use projection query with AsNoTracking for read-only performance
        var query = _dbContext.ComplianceJourneys
            .AsNoTracking()
            .Where(j => j.HospitalId == hospitalId && j.TenantId == tenantId);

        // Filter by frameworkId if provided
        if (frameworkId.HasValue)
        {
            query = query.Where(j => j.FrameworkId == frameworkId.Value);
        }

        var journeys = await query
            .Select(j => new
            {
                j.Id,
                j.Status,
                j.ExpiryDate,
                j.RiskScore,
                j.FrameworkId
            })
            .ToListAsync(cancellationToken);

        _logger.LogInformation(
            "Found {Count} journey(s) for HospitalId={HospitalId}, TenantId={TenantId}, FrameworkId={FrameworkId}",
            journeys.Count, hospitalId, tenantId, frameworkId);

        if (!journeys.Any())
        {
            // Log diagnostic information
            var allJourneysForHospital = await _dbContext.ComplianceJourneys
                .AsNoTracking()
                .Where(j => j.HospitalId == hospitalId)
                .Select(j => new { j.Id, j.TenantId, j.FrameworkId, j.Status })
                .ToListAsync(cancellationToken);

            _logger.LogWarning(
                "No journeys found matching criteria. Found {Count} total journey(s) for HospitalId={HospitalId} with TenantIds: {TenantIds}",
                allJourneysForHospital.Count,
                hospitalId,
                string.Join(", ", allJourneysForHospital.Select(j => j.TenantId.ToString()).Distinct()));

            return new JourneySummaryDto
            {
                TotalFrameworks = 0,
                Approved = 0,
                ExpiringIn90Days = 0,
                Suspended = 0,
                RiskIndex = 0
            };
        }

        var totalFrameworks = journeys.Count;
        var approved = journeys.Count(j => j.Status == ComplianceLifecycleState.Approved);
        var suspended = journeys.Count(j => j.Status == ComplianceLifecycleState.Suspended);

        // Calculate expiring in renewal window
        // Get renewal window days for each framework (default to 90 if not configured)
        var today = DateTime.UtcNow.Date;
        var expiringIn90Days = 0;

        // Batch load framework configurations to avoid N+1 queries
        var frameworkIds = journeys
            .Where(j => j.Status == ComplianceLifecycleState.Approved && j.ExpiryDate.HasValue)
            .Select(j => j.FrameworkId)
            .Distinct()
            .ToList();

        var configs = new Dictionary<Guid, int>();
        foreach (var fwId in frameworkIds)
        {
            var config = await _frameworkConfigRepository.GetOrCreateDefaultAsync(fwId, cancellationToken);
            configs[fwId] = Math.Min(90, config.RenewalDaysBeforeExpiry);
        }

        foreach (var journey in journeys.Where(j => j.Status == ComplianceLifecycleState.Approved && j.ExpiryDate.HasValue))
        {
            var renewalWindowDays = configs.GetValueOrDefault(journey.FrameworkId, 90);
            var renewalWindowStart = journey.ExpiryDate!.Value.Date.AddDays(-renewalWindowDays);

            if (today >= renewalWindowStart && today <= journey.ExpiryDate.Value.Date)
            {
                expiringIn90Days++;
            }
        }

        // Calculate weighted risk index
        // Critical (>70): weight 3, AtRisk (40-70): weight 2, Safe (<40): weight 1
        decimal totalWeightedRisk = 0;
        int totalWeight = 0;

        foreach (var journey in journeys)
        {
            int weight;
            if (journey.RiskScore > 70)
            {
                weight = 3; // Critical
            }
            else if (journey.RiskScore >= 40)
            {
                weight = 2; // AtRisk
            }
            else
            {
                weight = 1; // Safe
            }

            totalWeightedRisk += journey.RiskScore * weight;
            totalWeight += weight;
        }

        var riskIndex = totalWeight > 0 ? totalWeightedRisk / totalWeight : 0;

        return new JourneySummaryDto
        {
            TotalFrameworks = totalFrameworks,
            Approved = approved,
            ExpiringIn90Days = expiringIn90Days,
            Suspended = suspended,
            RiskIndex = Math.Round(riskIndex, 2)
        };
    }

    /// <summary>
    /// Gets organization-level compliance overview with enterprise-wide metrics.
    /// Uses read-optimized projection queries (no entity tracking).
    /// </summary>
    public async Task<ComplianceOverviewDto> GetComplianceOverviewAsync(
        Guid organizationId,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        // Validate organization belongs to tenant
        // (In a real system, you'd check Organization.TenantId, but for now we'll use OrganizationId == TenantId)
        if (organizationId != tenantId)
        {
                throw new UnauthorizedAccessException(
                    $"Access denied: Organization {organizationId} does not belong to tenant {tenantId}");
        }

        // Use projection query with AsNoTracking for read-only performance
        var journeys = await _dbContext.ComplianceJourneys
            .AsNoTracking()
            .Where(j => j.OrganizationId == organizationId && j.TenantId == tenantId)
            .Select(j => new
            {
                j.Status,
                j.RiskScore,
                j.ExpiryDate
            })
            .ToListAsync(cancellationToken);

        if (!journeys.Any())
        {
            return new ComplianceOverviewDto
            {
                PercentageApproved = 0,
                AtRiskCount = 0,
                CriticalCount = 0,
                ExpiryHeatmap = new List<ExpiryHeatmapItemDto>()
            };
        }

        var totalJourneys = journeys.Count;
        var approved = journeys.Count(j => j.Status == ComplianceLifecycleState.Approved);
        var percentageApproved = totalJourneys > 0
            ? Math.Round((decimal)approved / totalJourneys * 100, 2)
            : 0;

        var atRiskCount = journeys.Count(j => j.RiskScore >= 40 && j.RiskScore <= 70);
        var criticalCount = journeys.Count(j =>
            j.RiskScore > 70 ||
            j.Status == ComplianceLifecycleState.Suspended ||
            j.Status == ComplianceLifecycleState.Expired);

        // Build expiry heatmap for next 12 months
        var today = DateTime.UtcNow.Date;
        var heatmap = new Dictionary<string, int>();

        // Initialize next 12 months
        for (int i = 0; i < 12; i++)
        {
            var month = today.AddMonths(i);
            var monthKey = month.ToString("yyyy-MM");
            heatmap[monthKey] = 0;
        }

        // Count approved journeys expiring in each month
        foreach (var journey in journeys.Where(j =>
            j.Status == ComplianceLifecycleState.Approved &&
            j.ExpiryDate.HasValue))
        {
            var expiryMonth = journey.ExpiryDate!.Value.Date;
            var monthKey = expiryMonth.ToString("yyyy-MM");

            // Only include if within next 12 months
            if (expiryMonth >= today && expiryMonth <= today.AddMonths(11))
            {
                if (heatmap.ContainsKey(monthKey))
                {
                    heatmap[monthKey]++;
                }
            }
        }

        var expiryHeatmap = heatmap
            .Where(kvp => kvp.Value > 0) // Only include months with expiring journeys
            .OrderBy(kvp => kvp.Key)
            .Select(kvp => new ExpiryHeatmapItemDto
            {
                Month = kvp.Key,
                Count = kvp.Value
            })
            .ToList();

        return new ComplianceOverviewDto
        {
            PercentageApproved = percentageApproved,
            AtRiskCount = atRiskCount,
            CriticalCount = criticalCount,
            ExpiryHeatmap = expiryHeatmap
        };
    }

    /// <summary>
    /// Gets organization-level regulatory overview for Gateway.
    /// Aggregates active accreditations, upcoming renewals, and risk index using
    /// the same underlying data as compliance overview.
    /// </summary>
    public async Task<RegulatoryOverviewDto> GetRegulatoryOverviewAsync(
        Guid organizationId,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        // Reuse the compliance overview computation to derive risk-related metrics
        var overview = await GetComplianceOverviewAsync(organizationId, tenantId, cancellationToken);

        // Use ComplianceJourneys to derive counts for active accreditations and renewals
        var journeys = await _dbContext.ComplianceJourneys
            .AsNoTracking()
            .Where(j => j.OrganizationId == organizationId && j.TenantId == tenantId)
            .Select(j => new
            {
                j.Status,
                j.RiskScore,
                j.ExpiryDate
            })
            .ToListAsync(cancellationToken);

        // Active accreditations = journeys that are Enrolled or Approved
        var activeAccreditations = journeys.Count(j =>
            j.Status == ComplianceLifecycleState.Enrolled ||
            j.Status == ComplianceLifecycleState.Approved);

        // Upcoming renewals: Approved journeys with expiry in next 90 days
        var today = DateTime.UtcNow.Date;
        var renewalWindowEnd = today.AddDays(90);
        var upcomingRenewals = journeys.Count(j =>
            j.Status == ComplianceLifecycleState.Approved &&
            j.ExpiryDate.HasValue &&
            j.ExpiryDate.Value.Date >= today &&
            j.ExpiryDate.Value.Date <= renewalWindowEnd);

        // Map risk index summary to regulatory risk index DTO
        var riskIndexDto = new RegulatoryRiskIndexDto
        {
            // Approximate overall score using percentage of approved journeys
            Score = (int)Math.Round(overview.PercentageApproved),
            Classification = overview.CriticalCount > 0
                ? "Critical"
                : overview.AtRiskCount > 0
                    ? "AtRisk"
                    : "Safe",
            // Trend and delta are not yet modeled; default to neutral
            Trend = "Stable",
            Delta = 0,
            LastAssessedAt = DateTime.UtcNow
        };

        // Jurisdiction is not currently modeled in ComplianceService.
        // Return empty values; Gateway can enrich from OrganizationService if needed.
        var jurisdictionDto = new RegulatoryJurisdictionDto
        {
            Country = string.Empty,
            Region = string.Empty
        };

        return new RegulatoryOverviewDto
        {
            ActiveAccreditations = activeAccreditations,
            UpcomingRenewals = upcomingRenewals,
            RiskIndex = riskIndexDto,
            PrimaryJurisdiction = jurisdictionDto
        };
    }

    /// <summary>
    /// Gets framework catalog for an organization with basic filtering and pagination.
    /// This is consumed by the Gateway Framework Enrollment module.
    /// </summary>
    public async Task<FrameworkCatalogPageDto> GetFrameworkCatalogAsync(
        Guid organizationId,
        Guid tenantId,
        string? country,
        string? accreditationBody,
        string? type,
        string? version,
        int page,
        int pageSize,
        string? search,
        CancellationToken cancellationToken = default)
    {
        // For now we do not enforce organizationId vs tenantId relationship here;
        // Gateway already enforces OrgAccess policy. This method is read-only.

        // Base query: all active frameworks
        var query = _dbContext.ComplianceFrameworks
            .AsNoTracking()
            .Where(f => f.IsActive);

        // Apply filters (best-effort mapping; country/type fields are not modeled yet)
        if (!string.IsNullOrWhiteSpace(accreditationBody))
        {
            query = query.Where(f => f.Authority == accreditationBody);
        }

        if (!string.IsNullOrWhiteSpace(version))
        {
            query = query.Where(f => f.Version == version);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.ToLowerInvariant();
            query = query.Where(f =>
                f.Name.ToLower().Contains(searchLower) ||
                f.Version.ToLower().Contains(searchLower) ||
                f.Authority.ToLower().Contains(searchLower));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        // Normalize paging parameters
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 1;
        if (pageSize > 100) pageSize = 100;

        var frameworks = await query
            .OrderBy(f => f.Name)
            .ThenBy(f => f.Version)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        // Map to catalog items. Country/Type/Estimated* are not modeled yet, so we default them.
        var items = frameworks.Select(f => new FrameworkCatalogItemDto
        {
            FrameworkId = f.Id,
            Name = f.Name,
            Version = f.Version,
            Country = string.Empty,
            AccreditationBody = f.Authority,
            Type = string.Empty,
            IsDeprecated = !f.IsActive,
            IsAlreadyActive = false, // Not yet tracking org-level activation here
            EstimatedChapters = 0,
            EstimatedControls = 0
        }).ToList();

        return new FrameworkCatalogPageDto
        {
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            Items = items
        };
    }

    /// <summary>
    /// Gets accreditation summaries for an organization.
    /// Used by Gateway regulatory module for /accreditations listing.
    /// </summary>
    public async Task<List<AccreditationSummaryDto>> GetAccreditationsAsync(
        Guid organizationId,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        // Query journeys for this organization+tenant and join with frameworks for names
        var query = from j in _dbContext.ComplianceJourneys.AsNoTracking()
                    join f in _dbContext.ComplianceFrameworks.AsNoTracking()
                        on j.FrameworkId equals f.Id
                    where j.OrganizationId == organizationId && j.TenantId == tenantId
                    select new
                    {
                        JourneyId = j.Id,
                        j.FrameworkId,
                        FrameworkName = f.Name,
                        j.Status,
                        j.ExpiryDate,
                        j.RiskScore,
                        j.RowVersion
                    };

        var rows = await query.ToListAsync(cancellationToken);

        var result = new List<AccreditationSummaryDto>(rows.Count);
        foreach (var row in rows)
        {
            // Map risk score to simple classification
            var classification = row.RiskScore > 70
                ? "Critical"
                : row.RiskScore >= 40
                    ? "AtRisk"
                    : "Safe";

            // CompliancePercentage is not yet modeled; approximate with (100 - RiskScore) clamped to [0,100]
            var compliancePercentage = Math.Clamp(100.0 - (double)row.RiskScore, 0.0, 100.0);

            result.Add(new AccreditationSummaryDto
            {
                Id = row.JourneyId,
                FrameworkId = row.FrameworkId,
                FrameworkName = row.FrameworkName,
                Status = row.Status.ToString(),
                CompliancePercentage = compliancePercentage,
                RiskClassification = classification,
                ExpiresOn = row.ExpiryDate,
                RowVersion = row.RowVersion.Length > 0 ? Convert.ToBase64String(row.RowVersion) : null
            });
        }

        return result;
    }

    /// <summary>
    /// Gets upcoming renewals for an organization within a given window (in days).
    /// Used by Gateway regulatory module for /renewals listing.
    /// </summary>
    public async Task<List<RenewalDto>> GetRenewalsAsync(
        Guid organizationId,
        Guid tenantId,
        int withinDays,
        CancellationToken cancellationToken = default)
    {
        if (withinDays <= 0)
        {
            withinDays = 90;
        }

        var today = DateTime.UtcNow.Date;
        var end = today.AddDays(withinDays);

        var query = from j in _dbContext.ComplianceJourneys.AsNoTracking()
                    join f in _dbContext.ComplianceFrameworks.AsNoTracking()
                        on j.FrameworkId equals f.Id
                    where j.OrganizationId == organizationId
                          && j.TenantId == tenantId
                          && j.Status == ComplianceLifecycleState.Approved
                          && j.ExpiryDate.HasValue
                          && j.ExpiryDate.Value.Date >= today
                          && j.ExpiryDate.Value.Date <= end
                    select new
                    {
                        JourneyId = j.Id,
                        j.FrameworkId,
                        FrameworkName = f.Name,
                        RenewalDueOn = j.ExpiryDate!.Value.Date
                    };

        var rows = await query
            .OrderBy(r => r.RenewalDueOn)
            .ToListAsync(cancellationToken);

        var result = new List<RenewalDto>(rows.Count);
        foreach (var row in rows)
        {
            var daysRemaining = (row.RenewalDueOn - today).Days;

            result.Add(new RenewalDto
            {
                AccreditationId = row.JourneyId,
                FrameworkId = row.FrameworkId,
                FrameworkName = row.FrameworkName,
                RenewalDueOn = row.RenewalDueOn,
                DaysRemaining = daysRemaining
            });
        }

        return result;
    }

    /// <summary>
    /// Updates organization-level jurisdiction metadata.
    /// For now this is a stub implementation that simply echoes back the requested values.
    /// In a future iteration this should be persisted in OrganizationService and/or a shared configuration store.
    /// </summary>
    public Task<UpdateOrganizationJurisdictionResponseDto> UpdateOrganizationJurisdictionAsync(
        Guid organizationId,
        Guid tenantId,
        UpdateOrganizationJurisdictionRequestDto request,
        CancellationToken cancellationToken = default)
    {
        // Validate that the caller's tenant matches the requested organization
        // In a real system we would verify Organization.TenantId in OrganizationService.
        if (organizationId != tenantId)
        {
            throw new UnauthorizedAccessException(
                $"Access denied: Organization {organizationId} does not belong to tenant {tenantId}");
        }

        // Stub: Just echo back the requested jurisdiction.
        // Marketplace cache refresh flag is always false for now.
        var response = new UpdateOrganizationJurisdictionResponseDto
        {
            PrimaryJurisdiction = new OrganizationJurisdictionDto
            {
                Country = request.Country,
                Region = request.Region ?? string.Empty
            },
            MarketplaceCacheRefreshed = false
        };

        return Task.FromResult(response);
    }
}
