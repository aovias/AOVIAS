using System.Text.Json;
using Microsoft.Extensions.Logging;
using Shared.Infrastructure.DTO;

namespace ComplianceService.Infrastructure.Services;

/// <summary>
/// Lightweight read-only client for KnowledgeBase Service.
/// Used by ComplianceService to resolve KB ComplianceNode IDs to OE Codes
/// during onboarding assessment application (cross-database ID bridge).
/// </summary>
public interface IKnowledgeBaseServiceClient
{
    /// <summary>
    /// Returns a dictionary of KB ComplianceNode.Id → OE Code
    /// for all ObjectiveElement nodes belonging to the given framework.
    ///
    /// <paramref name="frameworkId"/> is forwarded directly to the KB /nodes endpoint.
    /// ComplianceService and KnowledgeBase share framework GUIDs — both databases were
    /// seeded from the same source, so the FK values are identical across services.
    /// </summary>
    Task<IReadOnlyDictionary<Guid, string>> GetObjectiveElementNodeCodesAsync(
        Guid frameworkId,
        CancellationToken cancellationToken = default);
}

public sealed class KnowledgeBaseServiceClient : IKnowledgeBaseServiceClient
{
    private const string ObjectiveElementNodeType = "ObjectiveElement";

    private readonly HttpClient _httpClient;
    private readonly ILogger<KnowledgeBaseServiceClient> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public KnowledgeBaseServiceClient(HttpClient httpClient, ILogger<KnowledgeBaseServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<Guid, string>> GetObjectiveElementNodeCodesAsync(
        Guid frameworkId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // CS and KB share framework GUIDs — they were seeded from the same master dataset.
            // Query nodes directly using the provided frameworkId.
            var direct = await FetchNodesForFrameworkAsync(frameworkId, cancellationToken);

            if (direct.Count > 0)
            {
                _logger.LogDebug(
                    "KnowledgeBaseService: resolved {Count} OE codes for FrameworkId={FrameworkId} (direct).",
                    direct.Count, frameworkId);
                return direct;
            }

            // Fallback: CS frameworkId might differ from KB in older data — enumerate KB frameworks.
            _logger.LogWarning(
                "KnowledgeBaseService: no nodes found for FrameworkId={FrameworkId} directly; enumerating KB frameworks.",
                frameworkId);

            var kbFrameworkIds = await GetKbFrameworkIdsAsync(cancellationToken);
            if (kbFrameworkIds.Count == 0)
            {
                _logger.LogWarning("KnowledgeBaseService: no frameworks found in KB.");
                return new Dictionary<Guid, string>();
            }

            var result = new Dictionary<Guid, string>();
            foreach (var kbFwId in kbFrameworkIds)
            {
                var batch = await FetchNodesForFrameworkAsync(kbFwId, cancellationToken);
                foreach (var (id, code) in batch)
                    result.TryAdd(id, code);
            }

            _logger.LogDebug(
                "KnowledgeBaseService: resolved {Count} OE codes from {Fw} KB frameworks (fallback).",
                result.Count, kbFrameworkIds.Count);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to resolve OE node codes from KnowledgeBase for FrameworkId={FrameworkId}.",
                frameworkId);
            return new Dictionary<Guid, string>();
        }
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    private async Task<IReadOnlyList<Guid>> GetKbFrameworkIdsAsync(CancellationToken ct)
    {
        var response = await _httpClient.GetAsync("/api/v1.0/frameworks", ct);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "KnowledgeBaseService /frameworks returned {Status}.", response.StatusCode);
            return Array.Empty<Guid>();
        }

        var content = await response.Content.ReadAsStringAsync(ct);
        var envelope = JsonSerializer.Deserialize<ApiResponse<List<KbFrameworkDto>>>(content, JsonOptions);
        return envelope?.Data?.Select(f => f.Id).ToList() ?? new List<Guid>();
    }

    private async Task<IReadOnlyDictionary<Guid, string>> FetchNodesForFrameworkAsync(
        Guid kbFrameworkId,
        CancellationToken ct)
    {
        var url = $"/api/v1.0/nodes?frameworkId={kbFrameworkId:D}";
        var response = await _httpClient.GetAsync(url, ct);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "KnowledgeBaseService /nodes returned {Status} for FrameworkId={KbFwId}.",
                response.StatusCode, kbFrameworkId);
            return new Dictionary<Guid, string>();
        }

        var content = await response.Content.ReadAsStringAsync(ct);
        var envelope = JsonSerializer.Deserialize<ApiResponse<List<KbNodeDto>>>(content, JsonOptions);

        if (envelope?.Data == null)
            return new Dictionary<Guid, string>();

        return envelope.Data
            .Where(n => string.Equals(n.NodeType, ObjectiveElementNodeType, StringComparison.OrdinalIgnoreCase)
                     && !string.IsNullOrWhiteSpace(n.Code))
            .ToDictionary(n => n.Id, n => n.Code!);
    }

    // ── Private DTOs (KB response shapes) ────────────────────────────────────

    private sealed class KbFrameworkDto
    {
        public Guid Id { get; set; }
    }

    private sealed class KbNodeDto
    {
        public Guid Id { get; set; }
        public string? NodeType { get; set; }
        public string? Code { get; set; }
    }
}
