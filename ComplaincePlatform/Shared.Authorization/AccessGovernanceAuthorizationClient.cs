using System.Net.Http.Json;
using System.Text.Json;

namespace Shared.Authorization;

public sealed class AccessGovernanceAuthorizationClient : IAuthorizationEvaluator
{
    private readonly HttpClient _httpClient;

    public AccessGovernanceAuthorizationClient(HttpClient httpClient) => _httpClient = httpClient;

    public async Task<AuthorizationDecision> EvaluateAsync(AuthorizationRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("/internal/authorization/evaluate", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        var data = await ReadDataElementAsync(response, cancellationToken);
        return MapDecision(data);
    }

    public async Task<IReadOnlyList<EffectivePermission>> GetEffectivePermissionsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"/internal/authorization/effective-permissions/{userId}", cancellationToken);
        response.EnsureSuccessStatusCode();
        var data = await ReadDataElementAsync(response, cancellationToken);
        if (data.ValueKind != JsonValueKind.Array)
            return Array.Empty<EffectivePermission>();
        return data.EnumerateArray().Select(item => new EffectivePermission
        {
            Code = item.GetProperty("code").GetString() ?? string.Empty,
            Sources = ReadStringArray(item, "sources")
        }).ToList();
    }

    private static AuthorizationDecision MapDecision(JsonElement data) => new()
    {
        Allowed = data.GetProperty("allowed").GetBoolean(),
        Reason = data.TryGetProperty("reason", out var reason) ? reason.GetString() ?? string.Empty : string.Empty,
        MatchedRoles = ReadStringArray(data, "matchedRoles"),
        MatchedPermissions = ReadStringArray(data, "matchedPermissions"),
        MatchedPolicies = ReadStringArray(data, "matchedPolicies"),
        MatchedDelegations = ReadStringArray(data, "matchedDelegations"),
        MatchedResponsibilities = ReadStringArray(data, "matchedResponsibilities"),
        EvaluationTrace = ReadTrace(data),
        RowLevelFilter = ReadRowLevelFilter(data)
    };

    private static RowLevelFilter? ReadRowLevelFilter(JsonElement data)
    {
        if (!data.TryGetProperty("rowLevelFilter", out var filter) || filter.ValueKind == JsonValueKind.Null)
            return null;
        return new RowLevelFilter
        {
            OrganizationId = TryReadGuid(filter, "organizationId"),
            HospitalId = TryReadGuid(filter, "hospitalId"),
            DepartmentId = TryReadGuid(filter, "departmentId")
        };
    }

    private static IReadOnlyList<EvaluationTraceStep> ReadTrace(JsonElement data)
    {
        if (!data.TryGetProperty("evaluationTrace", out var arr) || arr.ValueKind != JsonValueKind.Array)
            return Array.Empty<EvaluationTraceStep>();
        return arr.EnumerateArray().Select(x => new EvaluationTraceStep
        {
            Step = x.TryGetProperty("step", out var s) ? s.GetString() ?? string.Empty : string.Empty,
            Outcome = x.TryGetProperty("outcome", out var o) ? o.GetString() ?? string.Empty : string.Empty,
            Detail = x.TryGetProperty("detail", out var d) ? d.GetString() : null
        }).ToList();
    }

    private static Guid? TryReadGuid(JsonElement parent, string name)
    {
        if (!parent.TryGetProperty(name, out var el) || el.ValueKind == JsonValueKind.Null)
            return null;
        return Guid.TryParse(el.GetString(), out var id) ? id : null;
    }

    private static async Task<JsonElement> ReadDataElementAsync(HttpResponseMessage response, CancellationToken ct)
    {
        using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
        return doc.RootElement.TryGetProperty("data", out var dataEl) ? dataEl.Clone() : doc.RootElement.Clone();
    }

    private static IReadOnlyList<string> ReadStringArray(JsonElement parent, string name)
    {
        if (!parent.TryGetProperty(name, out var arr) || arr.ValueKind != JsonValueKind.Array)
            return Array.Empty<string>();
        return arr.EnumerateArray().Select(x => x.GetString() ?? string.Empty).ToList();
    }
}
