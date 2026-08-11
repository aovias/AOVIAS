using ComplianceService.Application.Analytics;
using ComplianceService.Application.Copilot.Contracts;
using ComplianceService.Domain.Copilot.Enums;

namespace ComplianceService.Application.RoleExperience;

/// <summary>
/// Persona Intelligence home — pack resolution, top priorities, and best-effort attention counts.
/// </summary>
public sealed class RoleExperienceService : IRoleExperienceService
{
    private const int MaxPriorities = 5;

    private readonly ICopilotRecommendationService _recommendations;
    private readonly IAnalyticsQueryService _analyticsQuery;

    public RoleExperienceService(
        ICopilotRecommendationService recommendations,
        IAnalyticsQueryService analyticsQuery)
    {
        _recommendations = recommendations;
        _analyticsQuery = analyticsQuery;
    }

    public async Task<RoleExperienceHomeDto> GetHomeAsync(
        IReadOnlyList<string> roles,
        Guid organizationId,
        Guid hospitalId,
        Guid? frameworkId = null,
        CancellationToken cancellationToken = default)
    {
        var pack = RoleExperiencePackRegistry.Resolve(roles);

        var priorities = await LoadPrioritiesAsync(organizationId, pack.Persona, cancellationToken);
        var attention = await LoadAttentionCountsAsync(organizationId, hospitalId, frameworkId, cancellationToken);

        return new RoleExperienceHomeDto
        {
            PackId = pack.PackId,
            Persona = pack.Persona,
            DefaultHomeRoute = pack.DefaultHomeRoute,
            Priorities = priorities,
            AttentionCounts = attention,
            QuickActions = pack.DefaultQuickActions
                .Select(a => new RoleExperienceQuickActionDto { Label = a.Label, Href = a.Href })
                .ToList(),
            WidgetAllowList = pack.WidgetAllowList,
            NavSectionIds = pack.NavSectionIds,
        };
    }

    private async Task<IReadOnlyList<RoleExperiencePriorityDto>> LoadPrioritiesAsync(
        Guid organizationId,
        string persona,
        CancellationToken ct)
    {
        if (organizationId == Guid.Empty)
            return Array.Empty<RoleExperiencePriorityDto>();

        try
        {
            var recs = await _recommendations.GetByPersonaAsync(organizationId, persona, MaxPriorities, ct);
            return recs
                .Take(MaxPriorities)
                .Select(r => new RoleExperiencePriorityDto
                {
                    Title = r.Title,
                    Rationale = r.Rationale ?? r.Description,
                    Href = r.DeepLink,
                    Severity = string.IsNullOrWhiteSpace(r.Severity) ? nameof(RecommendationSeverity.Medium) : r.Severity,
                })
                .ToList();
        }
        catch
        {
            return Array.Empty<RoleExperiencePriorityDto>();
        }
    }

    public async Task<RoleEngagementDto> GetEngagementAsync(
        IReadOnlyList<string> roles,
        Guid organizationId,
        Guid hospitalId,
        Guid? frameworkId = null,
        CancellationToken cancellationToken = default)
    {
        var pack = RoleExperiencePackRegistry.Resolve(roles);
        var counters = await LoadEngagementCountersAsync(organizationId, hospitalId, frameworkId, cancellationToken);

        return new RoleEngagementDto
        {
            PackId = pack.PackId,
            Persona = pack.Persona,
            Counters = counters,
            GeneratedAtUtc = DateTimeOffset.UtcNow,
        };
    }

    private async Task<RoleExperienceAttentionCountsDto> LoadAttentionCountsAsync(
        Guid organizationId,
        Guid hospitalId,
        Guid? frameworkId,
        CancellationToken ct)
    {
        if (organizationId == Guid.Empty || hospitalId == Guid.Empty)
            return new RoleExperienceAttentionCountsDto();

        try
        {
            var scope = await _analyticsQuery.ResolveScopeAsync(
                organizationId, hospitalId, frameworkId, ct);

            var operational = await _analyticsQuery.GetOperationalAsync(scope, ct);
            var overview = await _analyticsQuery.GetOverviewAsync(scope, ct);

            return new RoleExperienceAttentionCountsDto
            {
                OverdueTasks = operational.OverdueWork?.OverdueTasks ?? 0,
                PendingReviews = overview.PendingReviews,
                CriticalCapas = operational.OverdueWork?.OverdueCapas ?? 0,
                // Best-effort: no dedicated upcoming-audits field on analytics overview —
                // use audit session total as a soft signal when available.
                UpcomingAudits = Math.Max(0, operational.AuditSessionsTotal - operational.AuditSessionsClosedOrSubmitted),
            };
        }
        catch
        {
            return new RoleExperienceAttentionCountsDto();
        }
    }

    private async Task<RoleEngagementCountersDto> LoadEngagementCountersAsync(
        Guid organizationId,
        Guid hospitalId,
        Guid? frameworkId,
        CancellationToken ct)
    {
        if (organizationId == Guid.Empty || hospitalId == Guid.Empty)
            return new RoleEngagementCountersDto();

        try
        {
            var scope = await _analyticsQuery.ResolveScopeAsync(
                organizationId, hospitalId, frameworkId, ct);

            var operational = await _analyticsQuery.GetOperationalAsync(scope, ct);
            var capa = await _analyticsQuery.GetCapaAsync(scope, ct);

            return new RoleEngagementCountersDto
            {
                TasksCompleted = operational.TaskThroughput?.Completed ?? 0,
                ReviewsCompleted = operational.EvidencePipeline?.Approved ?? 0,
                ReviewsPending = Math.Max(
                    operational.VerificationBacklog,
                    operational.EvidencePipeline?.PendingReview ?? 0),
                CapaResolutions = operational.DeficiencyFunnel?.Closed ?? 0,
                CapasEffective = capa.Metrics?.EffectiveCapas ?? 0,
                AuditsCompleted = operational.AuditSessionsClosedOrSubmitted,
                OverdueTasks = operational.OverdueWork?.OverdueTasks ?? 0,
                OverdueCapas = operational.OverdueWork?.OverdueCapas ?? 0,
            };
        }
        catch
        {
            return new RoleEngagementCountersDto();
        }
    }
}
