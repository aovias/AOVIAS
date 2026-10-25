using ComplianceService.Application.Copilot.Contracts;
using ComplianceService.Application.Copilot.DTOs;
using ComplianceService.Domain.Copilot;

namespace ComplianceService.Application.Copilot.Services;

/// <summary>Sprint 13F — aggregates Copilot effectiveness from domain tables and activity events.</summary>
public sealed class CopilotAnalyticsService : ICopilotAnalyticsService
{
    private readonly ICopilotAnalyticsRepository _analytics;
    private readonly ICopilotActivityRepository _activity;

    public CopilotAnalyticsService(
        ICopilotAnalyticsRepository analytics,
        ICopilotActivityRepository activity)
    {
        _analytics = analytics;
        _activity   = activity;
    }

    public async Task<CopilotGovernanceDashboardDto> GetGovernanceDashboardAsync(
        Guid organizationId,
        CancellationToken ct = default)
    {
        var recommendations = await GetRecommendationMetricsAsync(organizationId, ct);
        var drafts            = await GetDraftMetricsAsync(organizationId, ct);
        var proposals         = await GetProposalMetricsAsync(organizationId, ct);
        var personas          = await GetPersonaMetricsAsync(organizationId, ct);
        var activity          = await GetActivityAnalyticsAsync(organizationId, ct);

        var monthStart = DateTime.UtcNow.AddDays(-30);
        var activeUsers = await _activity.CountDistinctActorsAsync(organizationId, monthStart, null, ct);
        var workspaceViews = activity.TopActivities
            .FirstOrDefault(a => a.Label == CopilotActivityEvent.EventTypes.WorkspaceViewed)?.Count ?? 0;

        return new CopilotGovernanceDashboardDto
        {
            Usage = new CopilotUsageSummaryDto
            {
                WorkspaceViews      = workspaceViews,
                ActiveUsers         = activeUsers,
                PersonaChanges      = personas.ManualOverrides,
                TotalActivityEvents = activity.MonthlyEvents,
            },
            Recommendations = recommendations,
            Drafts          = drafts,
            Proposals       = proposals,
            Personas        = personas,
            Activity        = activity,
            Trust = new CopilotTrustMetricsDto
            {
                ProposalApprovalRate        = proposals.ApprovalRate,
                RecommendationAcceptanceRate = recommendations.AcceptanceRate,
                DraftToProposalRate         = drafts.ProposalConversionRate,
                IgnoredRecommendations      = recommendations.Dismissed + recommendations.Expired,
            },
            MetricsAsOfUtc = DateTime.UtcNow,
        };
    }

    public async Task<RecommendationEffectivenessMetricsDto> GetRecommendationMetricsAsync(
        Guid organizationId,
        CancellationToken ct = default)
    {
        var counts = await _analytics.GetRecommendationCountsAsync(organizationId, ct: ct);
        var withProposal = await _analytics.CountRecommendationsWithProposalsAsync(organizationId, ct);
        var withExecuted = await _analytics.CountRecommendationsWithExecutedProposalsAsync(organizationId, ct);
        var viewed = await _activity.AggregateByEventTypeAsync(
            organizationId, null, null, ct);
        var viewedCount = viewed.FirstOrDefault(v =>
            v.Key == CopilotActivityEvent.EventTypes.RecommendationViewed)?.Count ?? 0;

        var byCategory = await _analytics.GetRecommendationCountsByCategoryAsync(organizationId, ct);
        var byPersona  = await _analytics.GetRecommendationCountsByPersonaAsync(organizationId, ct);
        var avgHours   = await _analytics.GetAverageRecommendationTimeToActionHoursAsync(organizationId, ct);

        var actionable = counts.Accepted + counts.Dismissed;
        return new RecommendationEffectivenessMetricsDto
        {
            TotalGenerated       = counts.Total,
            Proposed             = counts.Proposed,
            Accepted             = counts.Accepted,
            Dismissed            = counts.Dismissed,
            Expired              = counts.Expired,
            Viewed               = viewedCount,
            WithProposalCreated  = withProposal,
            WithProposalExecuted = withExecuted,
            AcceptanceRate       = Rate(counts.Accepted, counts.Total),
            ExecutionRate        = Rate(withExecuted, counts.Total),
            DismissalRate        = Rate(counts.Dismissed, counts.Total),
            AverageTimeToActionHours = avgHours,
            ByCategory = MapCategoryMetrics(byCategory, counts.Total),
            ByPersona  = MapCategoryMetrics(byPersona, counts.Total),
        };
    }

    public async Task<DraftEffectivenessMetricsDto> GetDraftMetricsAsync(
        Guid organizationId,
        CancellationToken ct = default)
    {
        var counts = await _analytics.GetDraftCountsAsync(organizationId, ct: ct);
        var withProposal = await _analytics.CountDraftsWithProposalsAsync(organizationId, ct);
        var byType = await _analytics.GetDraftCountsByTypeAsync(organizationId, ct);

        return new DraftEffectivenessMetricsDto
        {
            TotalDrafts            = counts.Total,
            ActiveDrafts           = counts.Active,
            ArchivedDrafts         = counts.Archived,
            RegeneratedGroups      = counts.RegeneratedGroups,
            WithProposalCreated    = withProposal,
            UtilizationRate        = Rate(withProposal, counts.Total),
            RegenerationRate       = Rate(counts.RegeneratedGroups, counts.Total),
            ProposalConversionRate = Rate(withProposal, counts.Total),
            ByDraftType = MapCategoryMetrics(byType, counts.Total),
        };
    }

    public async Task<ProposalConversionMetricsDto> GetProposalMetricsAsync(
        Guid organizationId,
        CancellationToken ct = default)
    {
        var counts = await _analytics.GetProposalCountsAsync(organizationId, ct: ct);
        var viewed = await _analytics.CountProposalAuditViewedAsync(organizationId, ct);
        var byType = await _analytics.GetProposalCountsByTypeAsync(organizationId, ct);
        var approvalHours = await _analytics.GetAverageProposalApprovalTimeHoursAsync(organizationId, ct);
        var executionHours = await _analytics.GetAverageProposalExecutionTimeHoursAsync(organizationId, ct);

        var decided = counts.Approved + counts.Rejected + counts.Executed;
        return new ProposalConversionMetricsDto
        {
            Proposed   = counts.Proposed,
            Viewed     = viewed,
            Approved   = counts.Approved + counts.Executed,
            Rejected   = counts.Rejected,
            Executed   = counts.Executed,
            Expired    = counts.Expired,
            ApprovalRate = Rate(counts.Approved + counts.Executed, decided),
            ExecutionRate = Rate(counts.Executed, counts.Approved + counts.Executed),
            RejectionRate = Rate(counts.Rejected, decided),
            AverageApprovalTimeHours  = approvalHours,
            AverageExecutionTimeHours = executionHours,
            ByProposalType = MapCategoryMetrics(byType, counts.Total),
        };
    }

    public async Task<PersonaAdoptionMetricsDto> GetPersonaMetricsAsync(
        Guid organizationId,
        CancellationToken ct = default)
    {
        var monthStart = DateTime.UtcNow.AddDays(-30);
        var byPersona = await _activity.AggregateByPersonaAsync(organizationId, monthStart, null, ct);
        var byEvent = await _activity.AggregateByEventTypeAsync(organizationId, monthStart, null, ct);

        var overrides = byEvent.FirstOrDefault(e =>
            e.Key == CopilotActivityEvent.EventTypes.PersonaChanged)?.Count ?? 0;

        var rows = new List<PersonaAdoptionRowDto>();
        foreach (var persona in byPersona)
        {
            var personaEvents = await _activity.GetByDateRangeAsync(
                organizationId, monthStart, null, persona: persona.Key, ct: ct);

            var recViewed = personaEvents.Count(e =>
                e.EventType == CopilotActivityEvent.EventTypes.RecommendationViewed);
            var drafts = personaEvents.Count(e =>
                e.EventType == CopilotActivityEvent.EventTypes.DraftCreated);
            var approved = personaEvents.Count(e =>
                e.EventType == CopilotActivityEvent.EventTypes.ProposalApproved);

            rows.Add(new PersonaAdoptionRowDto
            {
                Persona               = persona.Key,
                SessionEvents         = persona.Count,
                RecommendationsViewed = recViewed,
                DraftsGenerated       = drafts,
                ProposalsApproved     = approved,
                ConversionRate        = Rate(approved, persona.Count),
            });
        }

        var ordered = rows.OrderByDescending(r => r.SessionEvents).ToList();
        var highestConversion = rows.OrderByDescending(r => r.ConversionRate).FirstOrDefault();

        return new PersonaAdoptionMetricsDto
        {
            Personas = ordered,
            MostUsedPersona = ordered.FirstOrDefault()?.Persona,
            LeastUsedPersona = ordered.LastOrDefault()?.Persona,
            HighestConversionPersona = highestConversion?.Persona,
            ManualOverrides = overrides,
        };
    }

    public async Task<CopilotActivityAnalyticsDto> GetActivityAnalyticsAsync(
        Guid organizationId,
        CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var dayStart = now.AddDays(-1);
        var weekStart = now.AddDays(-7);
        var monthStart = now.AddDays(-30);

        var daily = await _activity.GetByDateRangeAsync(organizationId, dayStart, null, ct: ct);
        var weekly = await _activity.GetByDateRangeAsync(organizationId, weekStart, null, ct: ct);
        var monthly = await _activity.GetByDateRangeAsync(organizationId, monthStart, null, ct: ct);
        var top = await _activity.AggregateByEventTypeAsync(organizationId, monthStart, null, ct);
        var trend = await _activity.AggregateDailyAsync(organizationId, 30, ct);

        return new CopilotActivityAnalyticsDto
        {
            DailyEvents   = daily.Count,
            WeeklyEvents  = weekly.Count,
            MonthlyEvents = monthly.Count,
            TopActivities = top.Select(t => new CategoryMetricDto
            {
                Label = t.Key,
                Count = t.Count,
                Rate  = Rate(t.Count, monthly.Count),
            }).ToList(),
            DailyTrend = trend.Select(t => new DailyActivityPointDto
            {
                Date  = t.Date,
                Count = t.Count,
            }).ToList(),
        };
    }

    public async Task<CopilotTrendsDto> GetTrendsAsync(
        Guid organizationId,
        int days = 30,
        CancellationToken ct = default)
    {
        await CaptureDailySnapshotAsync(organizationId, ct);

        var snapshots = await _analytics.GetEffectivenessSnapshotsAsync(organizationId, days, ct);
        var activityTrend = await _activity.AggregateDailyAsync(organizationId, days, ct);

        return new CopilotTrendsDto
        {
            Snapshots = snapshots.Select(s => new CopilotEffectivenessSnapshotDto
            {
                SnapshotDate            = s.SnapshotDate,
                RecommendationsGenerated = s.RecommendationsGenerated,
                RecommendationsAccepted  = s.RecommendationsAccepted,
                DraftsGenerated          = s.DraftsGenerated,
                ProposalsExecuted        = s.ProposalsExecuted,
                PersonaSwitches          = s.PersonaSwitches,
                ActiveUsers              = s.ActiveUsers,
            }).ToList(),
            ActivityTrend = activityTrend.Select(t => new DailyActivityPointDto
            {
                Date  = t.Date,
                Count = t.Count,
            }).ToList(),
        };
    }

    public async Task<CopilotEffectivenessSnapshotDto> CaptureDailySnapshotAsync(
        Guid organizationId,
        CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var existing = await _analytics.GetEffectivenessSnapshotForDateAsync(organizationId, today, ct);
        if (existing != null)
        {
            return MapSnapshot(existing);
        }

        var dayStart = DateTime.UtcNow.Date;
        var recCounts = await _analytics.GetRecommendationCountsAsync(organizationId, dayStart, null, ct);
        var draftCounts = await _analytics.GetDraftCountsAsync(organizationId, dayStart, null, ct);
        var propCounts = await _analytics.GetProposalCountsAsync(organizationId, dayStart, null, ct);

        var dayEvents = await _activity.GetByDateRangeAsync(organizationId, dayStart, null, ct: ct);
        var viewed = dayEvents.Count(e =>
            e.EventType == CopilotActivityEvent.EventTypes.RecommendationViewed);
        var personaSwitches = dayEvents.Count(e =>
            e.EventType == CopilotActivityEvent.EventTypes.PersonaChanged);
        var activeUsers = await _activity.CountDistinctActorsAsync(organizationId, dayStart, null, ct);

        var snapshot = CopilotEffectivenessSnapshot.Create(
            organizationId,
            today,
            recCounts.Total,
            viewed,
            recCounts.Accepted,
            recCounts.Dismissed,
            draftCounts.Total,
            draftCounts.RegeneratedGroups,
            propCounts.Total,
            propCounts.Approved,
            propCounts.Rejected,
            propCounts.Executed,
            personaSwitches,
            activeUsers);

        await _analytics.SaveEffectivenessSnapshotAsync(snapshot, ct);
        return MapSnapshot(snapshot);
    }

    private static CopilotEffectivenessSnapshotDto MapSnapshot(CopilotEffectivenessSnapshot s) =>
        new()
        {
            SnapshotDate             = s.SnapshotDate,
            RecommendationsGenerated = s.RecommendationsGenerated,
            RecommendationsAccepted  = s.RecommendationsAccepted,
            DraftsGenerated          = s.DraftsGenerated,
            ProposalsExecuted        = s.ProposalsExecuted,
            PersonaSwitches          = s.PersonaSwitches,
            ActiveUsers              = s.ActiveUsers,
        };

    private static double Rate(int numerator, int denominator) =>
        denominator == 0 ? 0 : Math.Round((double)numerator / denominator * 100, 1);

    private static IReadOnlyList<CategoryMetricDto> MapCategoryMetrics(
        IReadOnlyList<LabelCount> items,
        int total) =>
        items.Select(i => new CategoryMetricDto
        {
            Label = i.Label,
            Count = i.Count,
            Rate  = Rate(i.Count, total),
        }).ToList();
}
