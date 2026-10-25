using System.Net.Http.Json;
using System.Text.Json;

namespace Shared.Governance;

public sealed class GovernanceHttpClient : IGovernanceClient, IApprovalWorkflowEvaluator, IEscalationResolver
{
    private readonly HttpClient _http;

    public GovernanceHttpClient(HttpClient http) => _http = http;

    public async Task<IReadOnlyList<GovernanceTimelineEntryDto>> GetTimelineAsync(string resourceType, Guid resourceId, CancellationToken ct = default)
    {
        var response = await _http.GetAsync($"/internal/governance/timeline?resourceType={Uri.EscapeDataString(resourceType)}&resourceId={resourceId}", ct);
        response.EnsureSuccessStatusCode();
        return await ReadDataListAsync<GovernanceTimelineEntryDto>(response, ct);
    }

    public async Task<OwnerLookupResult> GetOwnersAsync(OwnerLookupRequest request, CancellationToken ct = default)
    {
        var q = $"/internal/governance/owners?resourceType={Uri.EscapeDataString(request.ResourceType)}&resourceId={request.ResourceId}";
        if (!string.IsNullOrEmpty(request.ResponsibilityTypeCode))
            q += $"&responsibilityTypeCode={Uri.EscapeDataString(request.ResponsibilityTypeCode)}";
        var response = await _http.GetAsync(q, ct);
        response.EnsureSuccessStatusCode();
        return (await ReadDataAsync<OwnerLookupResult>(response, ct)) ?? new OwnerLookupResult();
    }

    public async Task<ApprovalDecision> EvaluateAsync(ApprovalEvaluateRequest request, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync("/internal/governance/approval/evaluate", request, ct);
        response.EnsureSuccessStatusCode();
        return (await ReadDataAsync<ApprovalDecision>(response, ct)) ?? new ApprovalDecision { Reason = "Empty response" };
    }

    public async Task<EscalationResolution> ResolveAsync(EscalationResolveRequest request, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync("/internal/governance/escalation/resolve", request, ct);
        response.EnsureSuccessStatusCode();
        return (await ReadDataAsync<EscalationResolution>(response, ct)) ?? new EscalationResolution { Reason = "Empty response" };
    }

    private static async Task<T?> ReadDataAsync<T>(HttpResponseMessage response, CancellationToken ct)
    {
        using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
        if (doc.RootElement.TryGetProperty("data", out var data))
            return data.Deserialize<T>();
        return doc.RootElement.Deserialize<T>();
    }

    private static async Task<IReadOnlyList<T>> ReadDataListAsync<T>(HttpResponseMessage response, CancellationToken ct)
    {
        using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
        var el = doc.RootElement.TryGetProperty("data", out var data) ? data : doc.RootElement;
        return el.Deserialize<List<T>>() ?? new List<T>();
    }
}
