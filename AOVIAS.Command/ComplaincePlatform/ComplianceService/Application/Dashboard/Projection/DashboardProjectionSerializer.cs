using System.Text.Json;
using System.Text.Json.Serialization;
using ComplianceService.Application.DTOs;
using ComplianceService.Domain;

namespace ComplianceService.Application.Dashboard.Projection;

public static class DashboardProjectionSerializer
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false,
    };

    public static DashboardProjectionPayload DeserializePayload(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new DashboardProjectionPayload();
        }

        try
        {
            return JsonSerializer.Deserialize<DashboardProjectionPayload>(json, JsonOptions)
                   ?? new DashboardProjectionPayload();
        }
        catch
        {
            return new DashboardProjectionPayload();
        }
    }

    public static string SerializePayload(DashboardProjectionPayload payload) =>
        JsonSerializer.Serialize(payload, JsonOptions);

    public static DashboardSnapshotDto ToSnapshotDto(
        DashboardProjection projection,
        DashboardProjectionPayload payload,
        Guid tenantId,
        string? organizationName = null)
    {
        return new DashboardSnapshotDto
        {
            IsSnapshotAvailable = projection.TotalJourneysCount > 0,
            SnapshotMaterialized = projection.IsComplete,
            FrameworkId = projection.FrameworkId == Guid.Empty ? null : projection.FrameworkId,
            OverallScore = projection.OverallComplianceScore,
            AuditConfidenceScore = projection.AuditConfidenceScore,
            CalculatedAt = projection.ScoreCalculatedAt ?? projection.LastUpdatedAt,
            TenantId = tenantId,
            OrganizationName = organizationName ?? string.Empty,
            GeneratedAt = projection.LastUpdatedAt.ToString("O"),
            Summary = payload.Summary,
            Frameworks = payload.Frameworks,
            Hospitals = payload.Hospitals,
            ScoreTrends = payload.ScoreTrends,
            TasksSummary = payload.TasksSummary,
            Milestones = payload.Milestones,
            Alerts = payload.Alerts,
            Filters = BuildFilters(payload),
            SnapshotRange = BuildFilters(payload).DateRange,
        };
    }

    public static void ApplySnapshotDto(DashboardProjection projection, DashboardSnapshotDto snapshot)
    {
        var payload = new DashboardProjectionPayload
        {
            Summary = snapshot.Summary,
            Frameworks = snapshot.Frameworks,
            Hospitals = snapshot.Hospitals,
            TasksSummary = snapshot.TasksSummary,
            Alerts = snapshot.Alerts,
            ScoreTrends = snapshot.ScoreTrends,
            Milestones = snapshot.Milestones,
            JourneyStatusCounts = snapshot.Frameworks
                .SelectMany(f => f.StatusCounts)
                .GroupBy(kvp => kvp.Key)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Value), StringComparer.OrdinalIgnoreCase),
        };

        projection.OverallComplianceScore = snapshot.OverallScore;
        projection.AuditConfidenceScore = snapshot.AuditConfidenceScore;
        projection.ActiveJourneysCount = snapshot.Summary.ActiveJourneys;
        projection.TotalJourneysCount = snapshot.Summary.TotalJourneys;
        projection.PendingTasksCount = snapshot.TasksSummary.Open;
        projection.OverdueTasksCount = snapshot.Summary.OverdueTasks;
        projection.OpenAlertsCount = snapshot.Alerts.Count(a => !a.Acknowledged);
        projection.ScoreCalculatedAt = snapshot.CalculatedAt;
        projection.PayloadJson = SerializePayload(payload);
        projection.ProjectionSchemaVersion = DashboardProjectionPayload.CurrentSchemaVersion;
        projection.IsComplete = snapshot.IsSnapshotAvailable;
        projection.IsCorrupted = false;
        projection.CorruptionReason = null;
    }

    private static SnapshotFiltersDto BuildFilters(DashboardProjectionPayload payload) =>
        new()
        {
            Frameworks = payload.Frameworks.Select(f => f.FrameworkCode).Distinct().ToList(),
            Hospitals = payload.Hospitals
                .Select(h => new FilterHospitalDto { HospitalId = h.HospitalId, Name = h.Name })
                .ToList(),
            DateRange = payload.ScoreTrends.Count switch
            {
                > 0 => new SnapshotRangeDto
                {
                    From = payload.ScoreTrends.First().Date,
                    To = payload.ScoreTrends.Last().Date,
                },
                _ => new SnapshotRangeDto(),
            },
        };
}
