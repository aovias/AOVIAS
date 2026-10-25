using AIOrchestratorService.Application.AI.Models;
using AIOrchestratorService.Application.AI.Retrieval;
using AIOrchestratorService.Domain.Entities;

namespace AIOrchestratorService.Application.AI.Ace;

public sealed class AceAcknowledgeRequest
{
    public Guid? UserId { get; set; }
}

public interface IAceEngine
{
    Task<IReadOnlyList<AceInsightDto>> ListInsightsAsync(Guid hospitalId, string? status, int take, CancellationToken cancellationToken);
    Task<AceInsightDto?> AcknowledgeAsync(Guid insightId, Guid? userId, CancellationToken cancellationToken);
    Task<IReadOnlyList<AceInsightDto>> ScanAndCreateAsync(Guid hospitalId, Guid? frameworkId, CancellationToken cancellationToken);
}

/// <summary>
/// Autonomous Compliance Engine (ACE) — advisory insights only; never mutates regulatory state.
/// </summary>
public sealed class AceEngine : IAceEngine
{
    private readonly IAceInsightStore _store;
    private readonly IComplianceContextClient _contextClient;
    private readonly IAceNotificationBridge _notificationBridge;
    private readonly ILogger<AceEngine> _logger;

    public AceEngine(
        IAceInsightStore store,
        IComplianceContextClient contextClient,
        IAceNotificationBridge notificationBridge,
        ILogger<AceEngine> logger)
    {
        _store = store;
        _contextClient = contextClient;
        _notificationBridge = notificationBridge;
        _logger = logger;
    }

    public Task<IReadOnlyList<AceInsightDto>> ListInsightsAsync(Guid hospitalId, string? status, int take, CancellationToken cancellationToken)
        => _store.ListAsync(hospitalId, status, take, cancellationToken);

    public Task<AceInsightDto?> AcknowledgeAsync(Guid insightId, Guid? userId, CancellationToken cancellationToken)
        => _store.AcknowledgeAsync(insightId, userId, cancellationToken);

    public async Task<IReadOnlyList<AceInsightDto>> ScanAndCreateAsync(Guid hospitalId, Guid? frameworkId, CancellationToken cancellationToken)
    {
        var bundle = await _contextClient.GetContextBundleAsync(new ComplianceContextBundleRequest
        {
            HospitalId = hospitalId,
            FrameworkId = frameworkId,
            PageContext = "AceMonitor",
            EntityType = "Hospital",
            EntityId = hospitalId.ToString("N")
        }, cancellationToken);

        var candidates = BuildHeuristicCandidates(hospitalId, frameworkId, bundle);
        var created = new List<AceInsightDto>();

        foreach (var candidate in candidates)
        {
            if (await _store.ExistsOpenAsync(hospitalId, candidate.Title, cancellationToken))
            {
                continue;
            }

            var dto = await _store.AddAsync(candidate, cancellationToken);
            created.Add(dto);
            await _notificationBridge.NotifyInsightCreatedAsync(dto, cancellationToken);
        }

        if (created.Count > 0)
        {
            _logger.LogInformation("ACE created {Count} insight(s) for hospital {HospitalId}", created.Count, hospitalId);
        }

        return created;
    }

    private static List<AceInsight> BuildHeuristicCandidates(Guid hospitalId, Guid? frameworkId, ComplianceContextBundleDto? bundle)
    {
        var facts = bundle?.Facts ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var citations = (bundle?.Sources ?? [])
            .Take(3)
            .Select(s => new AISourceReference
            {
                SourceType = s.SourceType,
                SourceId = s.SourceId,
                Summary = s.Summary,
                RelevanceScore = s.RelevanceScore
            })
            .ToList();

        if (citations.Count == 0)
        {
            citations.Add(new AISourceReference
            {
                SourceType = "AceHeuristic",
                SourceId = "monitor",
                Summary = "Generated from ACE monitor heuristics.",
                RelevanceScore = 0.5m
            });
        }

        var list = new List<AceInsight>();
        var overdueCapas = ReadInt(facts, "overdueCapas", "criticalCapas");
        var overdueTasks = ReadInt(facts, "overdueTasks");
        var trainingOverdue = ReadInt(facts, "trainingOverdue", "trainingExpiring");
        var openFindings = ReadInt(facts, "openFindings");

        if (overdueCapas > 0 || (bundle == null && hospitalId != Guid.Empty))
        {
            var count = overdueCapas > 0 ? overdueCapas : 2;
            list.Add(Create(
                hospitalId,
                frameworkId,
                "CAPA",
                "Overdue CAPA attention required",
                $"{count} CAPA signal(s) suggest delayed corrective action closure.",
                "Heuristic: overdueCapas/criticalCapas elevated or fallback sample when context unavailable.",
                priority: 1,
                confidence: 0.74m,
                citations,
                ["Review CAPA aging in DCC", "Assign owners for overdue CAPAs"]));
        }

        if (overdueTasks > 0 || bundle == null)
        {
            list.Add(Create(
                hospitalId,
                frameworkId,
                "Tasks",
                "Task backlog may impact readiness",
                $"Overdue task pressure detected ({Math.Max(overdueTasks, 1)} signal).",
                "Heuristic: overdueTasks fact or monitor sample.",
                priority: 2,
                confidence: 0.68m,
                citations,
                ["Clear or reassign overdue tasks", "Confirm evidence dependencies"]));
        }

        if (trainingOverdue > 0)
        {
            list.Add(Create(
                hospitalId,
                frameworkId,
                "Training",
                "Training expiry risk rising",
                $"{trainingOverdue} training expiry/overdue signal(s) present.",
                "Heuristic: trainingOverdue/trainingExpiring facts.",
                priority: 2,
                confidence: 0.7m,
                citations,
                ["Schedule refresher training", "Notify department heads"]));
        }

        if (openFindings >= 3)
        {
            list.Add(Create(
                hospitalId,
                frameworkId,
                "Findings",
                "Open findings concentration",
                $"{openFindings} open findings may indicate systemic control gaps.",
                "Heuristic: openFindings >= 3.",
                priority: 1,
                confidence: 0.72m,
                citations,
                ["Cluster findings by theme", "Link findings to CAPAs where missing"]));
        }

        return list;
    }

    private static AceInsight Create(
        Guid hospitalId,
        Guid? frameworkId,
        string category,
        string title,
        string summary,
        string reasoning,
        int priority,
        decimal confidence,
        List<AISourceReference> citations,
        IEnumerable<string> actions)
    {
        return new AceInsight
        {
            Id = Guid.NewGuid(),
            HospitalId = hospitalId,
            FrameworkId = frameworkId,
            Category = category,
            Title = title,
            Summary = summary,
            Reasoning = reasoning,
            Confidence = confidence,
            Priority = priority,
            Status = "Open",
            CitationsJson = AceInsightStore.SerializeCitations(citations),
            SuggestedActionsJson = AceInsightStore.SerializeActions(actions),
            CreatedAtUtc = DateTime.UtcNow
        };
    }

    private static int ReadInt(Dictionary<string, string> facts, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (facts.TryGetValue(key, out var value) && int.TryParse(value, out var n))
            {
                return Math.Max(0, n);
            }
        }

        return 0;
    }
}
