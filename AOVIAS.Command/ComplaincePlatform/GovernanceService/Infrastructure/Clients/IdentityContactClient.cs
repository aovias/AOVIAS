namespace GovernanceService.Infrastructure.Clients;

public interface IIdentityContactClient
{
    Task<(Guid? PersonId, string? Email)> GetUserContactAsync(Guid userId, CancellationToken ct = default);
}

public sealed class IdentityContactClient(HttpClient http) : IIdentityContactClient
{
    public async Task<(Guid? PersonId, string? Email)> GetUserContactAsync(Guid userId, CancellationToken ct = default)
    {
        try
        {
            var response = await http.GetAsync($"/internal/users/{userId}/contact", ct);
            if (!response.IsSuccessStatusCode) return (null, null);
            var json = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>(cancellationToken: ct);
            Guid? personId = json.TryGetProperty("personId", out var p) && p.ValueKind != System.Text.Json.JsonValueKind.Null ? p.GetGuid() : null;
            string? email = json.TryGetProperty("email", out var e) ? e.GetString() : null;
            return (personId, email);
        }
        catch { return (null, null); }
    }
}
