using Platform.Dashboard.Models;
using Platform.Dashboard.Registry;

namespace Platform.Dashboard.Services;

/// <summary>
/// Coordinates registered dashboard providers and merges their lightweight summary contributions.
/// </summary>
public sealed class DashboardSummaryService : IDashboardSummaryService
{
    private readonly IDashboardProviderRegistry _registry;

    public DashboardSummaryService(IDashboardProviderRegistry registry)
    {
        _registry = registry;
    }

    public async Task<DashboardSummaryResult> GetSummaryAsync(
        DashboardContext context,
        CancellationToken cancellationToken = default)
    {
        var providers = _registry.GetProviders(context);
        var contributions = new List<DashboardSummaryContribution>(providers.Count);

        foreach (var provider in providers)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var contribution = await provider.GetSummaryContributionAsync(context, cancellationToken);
            contributions.Add(contribution);
        }

        return Merge(context, contributions);
    }

    private static DashboardSummaryResult Merge(
        DashboardContext context,
        IReadOnlyList<DashboardSummaryContribution> contributions)
    {
        var applications = contributions
            .Select(c => c.Application)
            .Where(a => a is not null)
            .Cast<DashboardApplicationInfo>()
            .OrderBy(a => a.Order)
            .ThenBy(a => a.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var healthStatuses = contributions
            .Select(c => c.OverallHealth)
            .Where(h => h is not null)
            .Cast<DashboardOverallHealth>()
            .ToList();

        return new DashboardSummaryResult
        {
            Organization = new DashboardOrganizationInfo
            {
                OrganizationId = context.OrganizationId,
                Name = context.OrganizationName,
            },
            CurrentContext = new DashboardCurrentContext
            {
                HospitalId = context.HospitalId,
                HospitalName = context.HospitalName,
                FrameworkId = context.FrameworkId,
            },
            Applications = applications,
            OverallHealth = MergeHealth(healthStatuses),
            CriticalAlerts = SumNullable(contributions.Select(c => c.CriticalAlerts)),
            UnreadNotifications = SumNullable(contributions.Select(c => c.UnreadNotifications)),
            PendingTasks = SumNullable(contributions.Select(c => c.PendingTasks)),
            LastUpdated = contributions
                .Select(c => c.LastUpdated)
                .Where(d => d.HasValue)
                .Select(d => d!.Value)
                .DefaultIfEmpty(DateTimeOffset.UtcNow)
                .Max(),
        };
    }

    private static DashboardOverallHealth MergeHealth(IReadOnlyList<DashboardOverallHealth> statuses)
    {
        if (statuses.Count == 0)
        {
            return new DashboardOverallHealth { Status = "Healthy", Message = "No provider contributions." };
        }

        var rank = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["Critical"] = 4,
            ["Warning"] = 3,
            ["Unknown"] = 2,
            ["Healthy"] = 1,
        };

        var worst = statuses
            .OrderByDescending(s => rank.TryGetValue(s.Status, out var r) ? r : 0)
            .First();

        var averageScore = statuses
            .Where(s => s.Score.HasValue)
            .Select(s => s.Score!.Value)
            .DefaultIfEmpty()
            .Average();

        return new DashboardOverallHealth
        {
            Status = worst.Status,
            Message = worst.Message,
            Score = statuses.Any(s => s.Score.HasValue) ? averageScore : null,
        };
    }

    private static int SumNullable(IEnumerable<int?> values)
    {
        return values.Where(v => v.HasValue).Sum(v => v!.Value);
    }
}
