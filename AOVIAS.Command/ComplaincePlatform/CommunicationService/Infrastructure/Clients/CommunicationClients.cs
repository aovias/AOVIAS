using CommunicationService.Application.Contracts;

namespace CommunicationService.Infrastructure.Clients;

public interface IPersonContactClient
{
    Task<PersonContactInfo?> GetPrimaryContactAsync(Guid personId, CancellationToken ct = default);
}

public sealed class PersonContactInfo
{
    public string? Email { get; init; }
    public string? Phone { get; init; }
}

public sealed class PersonContactClient(HttpClient http) : IPersonContactClient
{
    public async Task<PersonContactInfo?> GetPrimaryContactAsync(Guid personId, CancellationToken ct = default)
    {
        try
        {
            var response = await http.GetAsync($"/api/v1/persons/{personId}", ct);
            if (!response.IsSuccessStatusCode) return null;
            var json = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>(cancellationToken: ct);
            string? email = null, phone = null;
            if (json.TryGetProperty("data", out var data))
            {
                if (data.TryGetProperty("contacts", out var contacts) && contacts.ValueKind == System.Text.Json.JsonValueKind.Array)
                {
                    foreach (var c in contacts.EnumerateArray())
                    {
                        var kind = c.TryGetProperty("contactKind", out var k) ? k.GetString() : null;
                        var value = c.TryGetProperty("value", out var v) ? v.GetString() : null;
                        if (string.Equals(kind, "EMAIL", StringComparison.OrdinalIgnoreCase) && email == null) email = value;
                        if (string.Equals(kind, "PHONE", StringComparison.OrdinalIgnoreCase) && phone == null) phone = value;
                    }
                }
            }
            return new PersonContactInfo { Email = email, Phone = phone };
        }
        catch { return null; }
    }
}

public interface IIdentityPersonClient
{
    Task<Guid?> GetPersonIdForUserAsync(Guid userId, CancellationToken ct = default);
}

public sealed class IdentityPersonClient(HttpClient http) : IIdentityPersonClient
{
    public async Task<Guid?> GetPersonIdForUserAsync(Guid userId, CancellationToken ct = default)
    {
        try
        {
            var response = await http.GetAsync($"/internal/users/{userId}/contact", ct);
            if (!response.IsSuccessStatusCode) return null;
            var json = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>(cancellationToken: ct);
            if (json.TryGetProperty("personId", out var pid) && pid.ValueKind != System.Text.Json.JsonValueKind.Null)
                return pid.GetGuid();
            return null;
        }
        catch { return null; }
    }
}
