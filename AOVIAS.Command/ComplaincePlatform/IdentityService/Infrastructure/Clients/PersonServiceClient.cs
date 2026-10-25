using IdentityService.Application.Contracts;
using Shared.Infrastructure.DTO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace IdentityService.Infrastructure.Clients;

public class PersonServiceClient : IPersonServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<PersonServiceClient> _logger;

    public PersonServiceClient(HttpClient httpClient, ILogger<PersonServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<PersonLinkResult?> CreatePersonForRegistrationAsync(string email, string? displayName, CancellationToken cancellationToken = default)
    {
        var request = new { email, displayName };
        var json = JsonSerializer.Serialize(request);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync("/internal/persons", content, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("PersonService create returned {StatusCode}: {Body}", response.StatusCode, body);
            return null;
        }

        var apiResponse = JsonSerializer.Deserialize<ApiResponse<InternalPersonCreatePayload>>(body, JsonOptions);
        if (apiResponse?.Success != true || apiResponse.Data == null)
        {
            _logger.LogWarning("PersonService create unsuccessful envelope: {Body}", body);
            return null;
        }

        return new PersonLinkResult
        {
            PersonId = apiResponse.Data.PersonId,
            PlatformId = apiResponse.Data.PlatformId
        };
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private sealed class InternalPersonCreatePayload
    {
        [JsonPropertyName("personId")]
        public Guid PersonId { get; set; }

        [JsonPropertyName("platformId")]
        public string PlatformId { get; set; } = string.Empty;
    }
}
