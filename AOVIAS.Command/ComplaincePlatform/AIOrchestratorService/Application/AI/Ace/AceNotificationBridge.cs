using System.Net.Http.Json;

namespace AIOrchestratorService.Application.AI.Ace;

public interface IAceNotificationBridge
{
    Task NotifyInsightCreatedAsync(AceInsightDto insight, CancellationToken cancellationToken);
}

/// <summary>
/// Best-effort HTTP notify to CommunicationService. Failures are logged; insights remain stored.
/// </summary>
public sealed class AceNotificationBridge : IAceNotificationBridge
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AceNotificationBridge> _logger;

    public AceNotificationBridge(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<AceNotificationBridge> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task NotifyInsightCreatedAsync(AceInsightDto insight, CancellationToken cancellationToken)
    {
        var baseUrl = _configuration["Services:CommunicationService:BaseUrl"];
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            _logger.LogInformation(
                "ACE insight {InsightId} stored only (CommunicationService BaseUrl not configured). Title={Title}",
                insight.Id,
                insight.Title);
            return;
        }

        try
        {
            HttpClient client;
            try
            {
                client = _httpClientFactory.CreateClient("CommunicationServiceClient");
            }
            catch (InvalidOperationException)
            {
                _logger.LogInformation(
                    "ACE insight {InsightId} stored only (CommunicationServiceClient not registered).",
                    insight.Id);
                return;
            }

            var payload = new
            {
                eventType = "AceInsightCreated",
                moduleCode = "AOVIAS",
                personId = Guid.Empty,
                critical = insight.Priority <= 2,
                // Tenant isolation: hospitalId scopes notification delivery / templates per hospital.
                variables = new Dictionary<string, string>
                {
                    ["insightId"] = insight.Id.ToString(),
                    ["hospitalId"] = insight.HospitalId.ToString(),
                    ["title"] = insight.Title,
                    ["summary"] = insight.Summary,
                    ["priority"] = insight.Priority.ToString()
                }
            };

            using var response = await client.PostAsJsonAsync("internal/communication/events", payload, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "ACE Communication notify failed for insight {InsightId}: {Status}",
                    insight.Id,
                    (int)response.StatusCode);
                return;
            }

            _logger.LogInformation("ACE insight {InsightId} notified to CommunicationService", insight.Id);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "ACE Communication notify transport failed for insight {InsightId}", insight.Id);
        }
    }
}
