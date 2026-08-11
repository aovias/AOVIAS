using ComplianceService.Application.Contracts;
using ComplianceService.Application.Scheduling;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace ComplianceService.Infrastructure.Services;

public sealed class OrganizationBusinessCalendarProvider : IBusinessCalendarProvider
{
    private readonly IOrganizationServiceClient _orgClient;
    private readonly IMemoryCache _cache;
    private readonly ILogger<OrganizationBusinessCalendarProvider> _logger;

    public OrganizationBusinessCalendarProvider(
        IOrganizationServiceClient orgClient,
        IMemoryCache cache,
        ILogger<OrganizationBusinessCalendarProvider> logger)
    {
        _orgClient = orgClient;
        _cache = cache;
        _logger = logger;
    }

    public async Task<BusinessCalendarSnapshot> GetAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        if (organizationId == Guid.Empty)
        {
            return new BusinessCalendarSnapshot { OrganizationId = organizationId };
        }

        var cacheKey = $"bizcal:{organizationId}:{DateTime.UtcNow:yyyy}";
        if (_cache.TryGetValue(cacheKey, out BusinessCalendarSnapshot? cached) && cached != null)
        {
            return cached;
        }

        var dto = await _orgClient.GetBusinessCalendarAsync(organizationId, DateTime.UtcNow.Year, cancellationToken);
        if (dto == null)
        {
            _logger.LogDebug("Business calendar unavailable for org {OrganizationId}; using defaults", organizationId);
            return new BusinessCalendarSnapshot { OrganizationId = organizationId };
        }

        var start = TimeSpan.TryParse(dto.WorkingHours?.Start, out var s)
            ? s
            : BusinessCalendarSnapshotDefaults.WorkingHoursStart;
        var end = TimeSpan.TryParse(dto.WorkingHours?.End, out var e)
            ? e
            : BusinessCalendarSnapshotDefaults.WorkingHoursEnd;
        if (start >= end)
        {
            start = BusinessCalendarSnapshotDefaults.WorkingHoursStart;
            end = BusinessCalendarSnapshotDefaults.WorkingHoursEnd;
        }

        var snapshot = new BusinessCalendarSnapshot
        {
            OrganizationId = organizationId,
            WorkingDays = BusinessCalendarCalculator.ParseWorkingDays(dto.WorkingDays),
            Holidays = dto.Holidays?.Select(h => h.Date).ToHashSet() ?? [],
            WorkingHoursStart = start,
            WorkingHoursEnd = end
        };

        _cache.Set(cacheKey, snapshot, TimeSpan.FromMinutes(15));
        return snapshot;
    }
}
