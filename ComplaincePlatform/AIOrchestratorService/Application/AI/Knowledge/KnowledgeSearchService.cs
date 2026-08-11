using AIOrchestratorService.Application.AI.Models;
using AIOrchestratorService.Application.AI.Retrieval;

namespace AIOrchestratorService.Application.AI.Knowledge;

public interface IKnowledgeSearchService
{
    Task<KnowledgeSearchResult> SearchAsync(KnowledgeQuery query, CancellationToken cancellationToken = default);
    Task SeedDemoIndexAsync(Guid hospitalId, Guid? frameworkId, CancellationToken cancellationToken = default);
}

public sealed class KnowledgeSearchService : IKnowledgeSearchService
{
    private readonly IVectorIndex _vectorIndex;
    private readonly IKnowledgeBaseClient _knowledgeBaseClient;
    private readonly IComplianceContextClient _complianceContextClient;

    public KnowledgeSearchService(
        IVectorIndex vectorIndex,
        IKnowledgeBaseClient knowledgeBaseClient,
        IComplianceContextClient complianceContextClient)
    {
        _vectorIndex = vectorIndex;
        _knowledgeBaseClient = knowledgeBaseClient;
        _complianceContextClient = complianceContextClient;
    }

    public async Task<KnowledgeSearchResult> SearchAsync(KnowledgeQuery query, CancellationToken cancellationToken = default)
    {
        var mode = string.IsNullOrWhiteSpace(query.Mode) ? "hybrid" : query.Mode.Trim().ToLowerInvariant();
        var topK = query.TopK <= 0 ? 10 : Math.Min(query.TopK, 40);
        var citations = new List<AISourceReference>();

        if (mode is "semantic" or "hybrid")
        {
            var vectorHits = await _vectorIndex.SearchAsync(
                query.HospitalId,
                query.Query,
                query.FrameworkId,
                topK,
                cancellationToken);

            foreach (var hit in vectorHits)
            {
                citations.Add(new AISourceReference
                {
                    SourceType = hit.SourceType,
                    SourceId = hit.SourceId,
                    DocumentId = hit.DocumentId,
                    Version = hit.Version,
                    Excerpt = hit.Excerpt,
                    Summary = string.IsNullOrWhiteSpace(hit.Title) ? hit.Excerpt : $"{hit.Title}: {hit.Excerpt}",
                    RelevanceScore = hit.Score
                });
            }
        }

        if (mode is "keyword" or "hybrid")
        {
            var aiRequest = new AIRequest
            {
                Prompt = query.Query,
                HospitalId = query.HospitalId,
                FrameworkId = query.FrameworkId,
                UseCase = AIUseCase.QueryCompliance
            };
            var kb = await _knowledgeBaseClient.AskAsync(aiRequest, cancellationToken);
            foreach (var snippet in kb.Take(topK))
            {
                citations.Add(new AISourceReference
                {
                    SourceType = "KnowledgeBase",
                    SourceId = snippet.Id,
                    Summary = snippet.Text,
                    Excerpt = snippet.Text.Length > 280 ? snippet.Text[..280] : snippet.Text,
                    RelevanceScore = 0.7m
                });
            }

            var bundle = await _complianceContextClient.GetContextBundleAsync(
                new ComplianceContextBundleRequest
                {
                    HospitalId = query.HospitalId,
                    FrameworkId = query.FrameworkId
                },
                cancellationToken);

            if (bundle != null)
            {
                foreach (var source in bundle.Sources.Take(Math.Min(5, topK)))
                {
                    if (!string.IsNullOrWhiteSpace(query.Query) &&
                        !(source.Summary?.Contains(query.Query, StringComparison.OrdinalIgnoreCase) ?? false) &&
                        mode == "keyword")
                    {
                        continue;
                    }

                    citations.Add(new AISourceReference
                    {
                        SourceType = source.SourceType,
                        SourceId = source.SourceId,
                        Summary = source.Summary,
                        Excerpt = source.Summary,
                        RelevanceScore = source.RelevanceScore
                    });
                }
            }
        }

        citations = citations
            .Where(x => !string.IsNullOrWhiteSpace(x.SourceId))
            .GroupBy(x => $"{x.SourceType}:{x.SourceId}", StringComparer.OrdinalIgnoreCase)
            .Select(g => g.OrderByDescending(x => x.RelevanceScore).First())
            .OrderByDescending(x => x.RelevanceScore)
            .Take(topK)
            .ToList();

        return new KnowledgeSearchResult
        {
            Query = query.Query,
            Mode = mode,
            Citations = citations,
            Total = citations.Count
        };
    }

    public async Task SeedDemoIndexAsync(Guid hospitalId, Guid? frameworkId, CancellationToken cancellationToken = default)
    {
        var seeds = new[]
        {
            ("Policy", "infection-control-sop", "Infection Control SOP", "Infection control standard operating procedure covering hand hygiene, PPE, and biomedical waste."),
            ("Policy", "aac-4-policy", "AAC.4 Access Policy", "AAC.4 defines patient access assessment criteria and documentation requirements for registration."),
            ("OE", "oe-aac-4", "Objective Element AAC.4", "Objective Element AAC.4 requires documented evidence of initial assessment within defined timelines."),
            ("CAPA", "capa-overdue-sample", "Overdue CAPA sample", "Sample CAPA describing overdue corrective actions linked to medication safety incidents."),
            ("Risk", "risk-med-safety", "Medication Safety Risk", "Enterprise risk: recurring medication incidents indicate probable training gap for nursing staff."),
            ("Training", "training-ic", "Infection Control Training", "Mandatory infection control training program with annual renewal and attendance tracking."),
            ("Incident", "incident-near-miss", "Near-miss medication incident", "Near-miss incident in Ward B involving look-alike medication labeling."),
            ("Document", "doc-quality-manual", "Hospital Quality Manual", "Quality manual describing NABH governance, document control, and management review cadence.")
        };

        foreach (var (sourceType, sourceId, title, content) in seeds)
        {
            await _vectorIndex.UpsertAsync(new VectorUpsertRequest
            {
                HospitalId = hospitalId,
                FrameworkId = frameworkId,
                SourceType = sourceType,
                SourceId = sourceId,
                DocumentId = sourceId,
                Version = "1.0",
                Title = title,
                Content = content,
                SourceUpdatedAtUtc = DateTime.UtcNow
            }, cancellationToken);
        }
    }
}
