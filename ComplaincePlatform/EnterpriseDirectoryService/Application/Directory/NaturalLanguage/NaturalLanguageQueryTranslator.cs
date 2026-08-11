using System.Net.Http.Json;
using System.Text.Json;
using EnterpriseDirectoryService.Application.Contracts;
using EnterpriseDirectoryService.Application.Directory.Platform;

namespace EnterpriseDirectoryService.Application.Directory.NaturalLanguage;

public interface INaturalLanguageQueryTranslator
{
    Task<DirectoryQuery> TranslateAsync(string naturalLanguage, Guid organizationId, CancellationToken ct = default);
}

public sealed class NaturalLanguageQueryTranslator(IAIOrchestratorClient aiClient) : INaturalLanguageQueryTranslator
{
    public async Task<DirectoryQuery> TranslateAsync(string naturalLanguage, Guid organizationId, CancellationToken ct = default)
    {
        var structured = await aiClient.TranslateDirectoryQueryAsync(naturalLanguage, organizationId, ct);
        if (structured is not null)
        {
            structured.Filters.OrganizationId = organizationId;
            return structured;
        }
        return HeuristicTranslate(naturalLanguage, organizationId);
    }

    private static DirectoryQuery HeuristicTranslate(string text, Guid organizationId)
    {
        var q = new DirectoryQuery { Text = text, Filters = new DirectoryQueryFilters { OrganizationId = organizationId }, Fuzzy = true };
        var lower = text.ToLowerInvariant();
        if (lower.Contains("icu") || lower.Contains("intensive care")) q.Filters.DepartmentId = null;
        if (lower.Contains("nurse") || lower.Contains("rn")) q.Text = "nurse";
        if (lower.Contains("acls")) { q.Filters.Certification = "ACLS"; q.Text = string.Join(' ', q.Text.Split(' ').Where(t => !t.Equals("acls", StringComparison.OrdinalIgnoreCase))); }
        if (lower.Contains("expir") && int.TryParse(new string(lower.Where(char.IsDigit).ToArray()), out var days))
            q.Filters.ExpiryWithinDays = days > 0 ? days : 60;
        return q;
    }
}

public interface IAIOrchestratorClient
{
    Task<DirectoryQuery?> TranslateDirectoryQueryAsync(string query, Guid organizationId, CancellationToken ct = default);
}

public sealed class AIOrchestratorClient(HttpClient http) : IAIOrchestratorClient
{
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public async Task<DirectoryQuery?> TranslateDirectoryQueryAsync(string query, Guid organizationId, CancellationToken ct = default)
    {
        try
        {
            var body = new { query, organizationId, schema = "DirectoryQuery" };
            var response = await http.PostAsJsonAsync("/api/v1/ai/directory/translate-query", body, ct);
            if (!response.IsSuccessStatusCode) return null;
            var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<DirectoryQuery>>(JsonOpts, ct);
            return envelope?.Data;
        }
        catch
        {
            return null;
        }
    }
}
