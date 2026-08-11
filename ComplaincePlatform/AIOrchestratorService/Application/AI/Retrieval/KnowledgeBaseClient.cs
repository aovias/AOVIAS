using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using Microsoft.Extensions.Logging;
using AIOrchestratorService.Application.AI.Models;
using Shared.Infrastructure.DTO;

namespace AIOrchestratorService.Application.AI.Retrieval;

public sealed class KnowledgeBaseSnippet
{
    public string Id { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
}

public interface IKnowledgeBaseClient
{
    Task<IReadOnlyList<KnowledgeBaseSnippet>> AskAsync(AIRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<KnowledgeBaseSnippet>> SearchAsync(string query, Guid? frameworkId, CancellationToken cancellationToken);
}

public sealed class KnowledgeBaseClient : IKnowledgeBaseClient
{
    private const int KnowledgeSearchResultLimit = 5;

    private readonly HttpClient _httpClient;
    private readonly ILogger<KnowledgeBaseClient> _logger;

    public KnowledgeBaseClient(IHttpClientFactory factory, ILogger<KnowledgeBaseClient> logger)
    {
        _httpClient = factory.CreateClient("KnowledgeBaseServiceClient");
        _logger = logger;
    }

    public async Task<IReadOnlyList<KnowledgeBaseSnippet>> AskAsync(AIRequest request, CancellationToken cancellationToken)
    {
        if (!request.FrameworkId.HasValue || request.FrameworkId.Value == Guid.Empty)
        {
            _logger.LogWarning("KnowledgeBase ask skipped because canonical framework id is missing.");
            return Array.Empty<KnowledgeBaseSnippet>();
        }

        var searchResults = await SearchKnowledgeAsync(request.FrameworkId.Value, request.Prompt, cancellationToken);
        if (searchResults.Count > 0)
        {
            return searchResults;
        }

        return await AskAiAsync(request, request.FrameworkId.Value, cancellationToken);
    }

    public async Task<IReadOnlyList<KnowledgeBaseSnippet>> SearchAsync(
        string query,
        Guid? frameworkId,
        CancellationToken cancellationToken)
    {
        if (!frameworkId.HasValue || frameworkId.Value == Guid.Empty)
        {
            _logger.LogDebug("KnowledgeBase search skipped because canonical framework id is missing.");
            return Array.Empty<KnowledgeBaseSnippet>();
        }

        return await SearchKnowledgeAsync(frameworkId.Value, query, cancellationToken);
    }

    private async Task<IReadOnlyList<KnowledgeBaseSnippet>> SearchKnowledgeAsync(
        Guid frameworkId,
        string query,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return Array.Empty<KnowledgeBaseSnippet>();
        }

        var requestUri = BuildSearchUri(query, frameworkId);
        try
        {
            using var response = await _httpClient.GetAsync(requestUri, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning(
                    "KnowledgeBase search failed with status {StatusCode}. Body={Body}",
                    (int)response.StatusCode,
                    body);
                return Array.Empty<KnowledgeBaseSnippet>();
            }

            var envelope = await response.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<KnowledgeItemDto>>>(cancellationToken: cancellationToken);
            var items = envelope?.Data?
                .Where(x => x.IsActive)
                .Take(KnowledgeSearchResultLimit)
                .ToList();
            if (items is null || items.Count == 0)
            {
                return Array.Empty<KnowledgeBaseSnippet>();
            }

            return items.Select(MapKnowledgeItem).ToList();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(
                ex,
                "KnowledgeBase search transport failed. BaseAddress={BaseAddress}",
                _httpClient.BaseAddress);
            return Array.Empty<KnowledgeBaseSnippet>();
        }
    }

    private async Task<IReadOnlyList<KnowledgeBaseSnippet>> AskAiAsync(
        AIRequest request,
        Guid frameworkId,
        CancellationToken cancellationToken)
    {
        var payload = new
        {
            userId = request.HospitalId == Guid.Empty ? Guid.Empty : request.HospitalId,
            frameworkId,
            nodeId = (Guid?)null,
            query = request.Prompt
        };

        try
        {
            using var response = await _httpClient.PostAsJsonAsync("/api/v1.0/ai/ask", payload, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning(
                    "KnowledgeBase ask failed with status {StatusCode}. Body={Body}",
                    (int)response.StatusCode,
                    body);
                return Array.Empty<KnowledgeBaseSnippet>();
            }

            var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<KnowledgeBaseAskDto>>(cancellationToken: cancellationToken);
            var answer = envelope?.Data?.Response;
            if (string.IsNullOrWhiteSpace(answer))
            {
                return Array.Empty<KnowledgeBaseSnippet>();
            }

            return new List<KnowledgeBaseSnippet>
            {
                new()
                {
                    Id = envelope?.Data?.InteractionId.ToString() ?? Guid.NewGuid().ToString("N"),
                    Text = answer
                }
            };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(
                ex,
                "KnowledgeBase ask transport failed. BaseAddress={BaseAddress}",
                _httpClient.BaseAddress);
            return Array.Empty<KnowledgeBaseSnippet>();
        }
    }

    private static KnowledgeBaseSnippet MapKnowledgeItem(KnowledgeItemDto item)
    {
        return new KnowledgeBaseSnippet
        {
            Id = item.Id.ToString("N"),
            Text = BuildKnowledgeSummary(item)
        };
    }

    private static string BuildKnowledgeSummary(KnowledgeItemDto item)
    {
        var parts = new List<string>();
        TryAppendSegment(parts, "Explanation", item.Explanation);
        TryAppendSegment(parts, "Why it matters", item.WhyItMatters);
        TryAppendSegment(parts, "How to comply", item.HowToComply);
        TryAppendSegment(parts, "Audit checklist", item.AuditChecklist);
        if (!string.IsNullOrWhiteSpace(item.Tags))
        {
            parts.Add($"Tags: {item.Tags.Trim()}");
        }

        var summary = string.Join(" | ", parts).Trim();
        return string.IsNullOrWhiteSpace(summary) ? item.Explanation : summary;
    }

    private static string BuildSearchUri(string query, Guid frameworkId)
    {
        var segments = new List<string>
        {
            $"q={Uri.EscapeDataString(query.Trim())}"
        };

        if (frameworkId != Guid.Empty)
        {
            segments.Add($"frameworkId={Uri.EscapeDataString(frameworkId.ToString("N"))}");
        }

        return $"/api/v1.0/knowledge/search?{string.Join("&", segments)}";
    }

    private static void TryAppendSegment(List<string> segments, string label, string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return;
        }

        segments.Add($"{label}: {content.Trim()}");
    }

    private sealed class ApiEnvelope<T>
    {
        public T? Data { get; set; }
    }

    private sealed class KnowledgeBaseAskDto
    {
        public Guid InteractionId { get; set; }
        public string Response { get; set; } = string.Empty;
    }

    private sealed class KnowledgeItemDto
    {
        public Guid Id { get; set; }
        public Guid NodeId { get; set; }
        public string Explanation { get; set; } = string.Empty;
        public string WhyItMatters { get; set; } = string.Empty;
        public string HowToComply { get; set; } = string.Empty;
        public string AuditChecklist { get; set; } = string.Empty;
        public string Tags { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }
}
