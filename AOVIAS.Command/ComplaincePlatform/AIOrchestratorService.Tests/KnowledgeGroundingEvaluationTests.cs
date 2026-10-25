using AIOrchestratorService.Application.AI.Classification;
using AIOrchestratorService.Application.AI.Knowledge;
using AIOrchestratorService.Application.AI.Models;
using AIOrchestratorService.Application.AI.Retrieval;
using Xunit;

namespace AIOrchestratorService.Tests;

public class KnowledgeGroundingEvaluationTests
{
    [Fact]
    public async Task HashingEmbedding_Cosine_Ranks_Golden_QA_Higher_Than_Unrelated()
    {
        var embeddings = new HashingEmbeddingService();
        var query = await embeddings.EmbedAsync("infection control hand hygiene PPE");
        var relevant = await embeddings.EmbedAsync(
            "Infection control SOP covering hand hygiene, PPE, and biomedical waste.");
        var unrelated = await embeddings.EmbedAsync(
            "Parking policy for visitor vehicles near the main lobby.");

        var relevantScore = HashingEmbeddingService.CosineSimilarity(query, relevant);
        var unrelatedScore = HashingEmbeddingService.CosineSimilarity(query, unrelated);

        Assert.True(relevantScore > unrelatedScore);
        Assert.True(relevantScore > 0.15f);
    }

    [Fact]
    public async Task KnowledgeSearch_Semantic_Returns_VersionAware_Citations_From_VectorIndex()
    {
        var hospitalId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var vector = new InMemoryVectorIndex();
        await vector.UpsertAsync(new VectorUpsertRequest
        {
            HospitalId = hospitalId,
            SourceType = "Policy",
            SourceId = "infection-control-sop",
            DocumentId = "infection-control-sop",
            Version = "1.0",
            Title = "Infection Control SOP",
            Content = "Infection control standard operating procedure covering hand hygiene and PPE."
        });

        var search = new KnowledgeSearchService(
            vector,
            new EmptyKnowledgeBaseClient(),
            new EmptyComplianceContextClient());

        var result = await search.SearchAsync(new KnowledgeQuery
        {
            HospitalId = hospitalId,
            Query = "hand hygiene infection control",
            Mode = "semantic",
            TopK = 5
        });

        Assert.Equal("semantic", result.Mode);
        Assert.True(result.Total >= 1);
        var hit = Assert.Single(result.Citations);
        Assert.Equal("infection-control-sop", hit.DocumentId);
        Assert.Equal("1.0", hit.Version);
        Assert.False(string.IsNullOrWhiteSpace(hit.Excerpt));
        Assert.True(hit.RelevanceScore > 0);
    }

    [Fact]
    public void GroundedAnswer_Populated_Shape_Has_Required_Fields()
    {
        var grounded = new GroundedAnswer
        {
            Answer = "Hand hygiene evidence is incomplete for AAC.4.",
            Citations =
            {
                new AISourceReference
                {
                    SourceType = "OE",
                    SourceId = "oe-aac-4",
                    DocumentId = "oe-aac-4",
                    Version = "2.1",
                    Excerpt = "AAC.4 requires documented evidence of initial assessment.",
                    RelevanceScore = 0.91m
                }
            },
            Reasoning =
            {
                new AIReasoningStep { Code = "RETRIEVAL", Message = "Matched OE AAC.4 citation." }
            },
            ConfidenceScore = 0.84m,
            ModelVersion = "ExplainCompliance:1.0",
            TraceId = "trace-eval-001",
            AnswerMode = AssistantAnswerMode.OrganizationSpecific,
            HumanApprovalRequired = false,
            Retrieval = new AIRetrievalContext
            {
                ContextVersionId = "ctx-1",
                RelevanceScore = 0.9m,
                Facts = { ["OE"] = "AAC.4" }
            }
        };

        Assert.False(string.IsNullOrWhiteSpace(grounded.Answer));
        Assert.NotEmpty(grounded.Citations);
        Assert.NotEmpty(grounded.Reasoning);
        Assert.InRange(grounded.ConfidenceScore, 0m, 1m);
        Assert.False(string.IsNullOrWhiteSpace(grounded.ModelVersion));
        Assert.False(string.IsNullOrWhiteSpace(grounded.TraceId));
        Assert.NotNull(grounded.Retrieval);
        Assert.Equal("oe-aac-4", grounded.Citations[0].DocumentId);
        Assert.Equal("2.1", grounded.Citations[0].Version);
    }

    [Fact]
    public void SensitiveDataRedactor_Masks_Email_And_Phone()
    {
        var input = "Contact nurse.lead@hospital.org or call 9876543210 about CAPA.";
        var redacted = AIOrchestratorService.Application.AI.Governance.SensitiveDataRedactor.Redact(input);

        Assert.Contains("[REDACTED_EMAIL]", redacted);
        Assert.Contains("[REDACTED_PHONE]", redacted);
        Assert.DoesNotContain("nurse.lead@hospital.org", redacted);
        Assert.DoesNotContain("9876543210", redacted);
    }

    private sealed class InMemoryVectorIndex : IVectorIndex
    {
        private readonly List<(VectorUpsertRequest Request, float[] Embedding)> _items = new();
        private readonly HashingEmbeddingService _embeddings = new();

        public async Task UpsertAsync(VectorUpsertRequest request, CancellationToken cancellationToken = default)
        {
            var embedding = await _embeddings.EmbedAsync(request.Content, cancellationToken);
            _items.RemoveAll(x =>
                x.Request.HospitalId == request.HospitalId &&
                x.Request.SourceType == request.SourceType &&
                x.Request.SourceId == request.SourceId);
            _items.Add((request, embedding));
        }

        public async Task<IReadOnlyList<VectorSearchHit>> SearchAsync(
            Guid hospitalId,
            string query,
            Guid? frameworkId,
            int topK,
            CancellationToken cancellationToken = default)
        {
            var q = await _embeddings.EmbedAsync(query, cancellationToken);
            return _items
                .Where(x => x.Request.HospitalId == hospitalId)
                .Select(x => new VectorSearchHit
                {
                    SourceType = x.Request.SourceType,
                    SourceId = x.Request.SourceId,
                    DocumentId = x.Request.DocumentId,
                    Version = x.Request.Version,
                    Title = x.Request.Title,
                    Excerpt = x.Request.Content.Length > 280 ? x.Request.Content[..280] : x.Request.Content,
                    Score = (decimal)HashingEmbeddingService.CosineSimilarity(q, x.Embedding)
                })
                .OrderByDescending(x => x.Score)
                .Take(Math.Max(1, topK))
                .ToList();
        }

        public Task<int> KeywordSearchCountAsync(Guid hospitalId, string query, CancellationToken cancellationToken = default)
            => Task.FromResult(_items.Count(x =>
                x.Request.HospitalId == hospitalId &&
                x.Request.Content.Contains(query, StringComparison.OrdinalIgnoreCase)));
    }

    private sealed class EmptyKnowledgeBaseClient : IKnowledgeBaseClient
    {
        public Task<IReadOnlyList<KnowledgeBaseSnippet>> AskAsync(AIRequest request, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<KnowledgeBaseSnippet>>(Array.Empty<KnowledgeBaseSnippet>());

        public Task<IReadOnlyList<KnowledgeBaseSnippet>> SearchAsync(string query, Guid? frameworkId, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<KnowledgeBaseSnippet>>(Array.Empty<KnowledgeBaseSnippet>());
    }

    private sealed class EmptyComplianceContextClient : IComplianceContextClient
    {
        public Task<ComplianceContextBundleDto?> GetContextBundleAsync(
            ComplianceContextBundleRequest request,
            CancellationToken cancellationToken)
            => Task.FromResult<ComplianceContextBundleDto?>(null);

        public Task<ComplianceCommandPreconditionResponse?> ValidateCommandPreconditionsAsync(
            ComplianceCommandPreconditionRequest request,
            CancellationToken cancellationToken)
            => Task.FromResult<ComplianceCommandPreconditionResponse?>(null);
    }
}
