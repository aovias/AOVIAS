using IdentityService.Application.Contracts;
using System.Text.Json;

namespace IdentityService.Infrastructure.Clients;

public class WorkforceContextClient(HttpClient http, ILogger<WorkforceContextClient> logger) : IWorkforceContextClient
{
    public async Task<Guid?> GetPrimaryEmploymentIdAsync(Guid personId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await http.GetAsync($"/internal/employment/by-person/{personId}", cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogDebug("No employment context for person {PersonId}", personId);
                return null;
            }
            using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
            if (!doc.RootElement.TryGetProperty("data", out var data) || !data.TryGetProperty("items", out var items) || items.GetArrayLength() == 0)
                return null;
            var first = items[0];
            return first.TryGetProperty("id", out var id) ? id.GetGuid() : null;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            logger.LogWarning("WorkforceService unavailable while resolving employment for person {PersonId}", personId);
            return null;
        }
    }
}

public class ProfessionalContextClient(HttpClient http, ILogger<ProfessionalContextClient> logger) : IProfessionalContextClient
{
    public async Task<Guid?> GetProfessionalProfileIdAsync(Guid personId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await http.GetAsync($"/internal/professional/by-person/{personId}", cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogDebug("No professional profile for person {PersonId}", personId);
                return null;
            }
            using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
            if (!doc.RootElement.TryGetProperty("data", out var data) || !data.TryGetProperty("id", out var id))
                return null;
            return id.GetGuid();
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            logger.LogWarning("ProfessionalService unavailable while resolving profile for person {PersonId}", personId);
            return null;
        }
    }
}
