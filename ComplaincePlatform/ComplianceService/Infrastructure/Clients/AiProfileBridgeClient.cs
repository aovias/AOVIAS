using System.Net.Http.Json;
using System.Text.Json;

namespace ComplianceService.Infrastructure.Clients;

public interface IAiProfileBridgeClient
{
    Task<Guid?> GetProfileIdForPersonaAsync(string personaKey, CancellationToken ct = default);
}

public sealed class AiProfileBridgeClient(HttpClient http) : IAiProfileBridgeClient
{
    public async Task<Guid?> GetProfileIdForPersonaAsync(string personaKey, CancellationToken ct = default)
    {
        try
        {
            var response = await http.GetAsync($"/internal/ai/profiles/by-persona/{personaKey}", ct);
            if (!response.IsSuccessStatusCode) return null;
            var json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
            if (json.TryGetProperty("data", out var data) && data.TryGetProperty("id", out var id))
                return id.GetGuid();
        }
        catch { /* bridge optional at startup */ }
        return null;
    }
}
