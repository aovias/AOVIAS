using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shared.Infrastructure.Auth;

namespace Shared.Communication;

public sealed class CommunicationHttpClient(HttpClient http) : ICommunicationClient
{
    public async Task<CommunicationSendResultDto> SendAsync(CommunicationRequestDto request, CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync("/internal/communication/send", request, ct);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
        if (json.TryGetProperty("data", out var data) && data.TryGetProperty("outboundMessageId", out var id))
            return new CommunicationSendResultDto { OutboundMessageId = id.GetGuid(), Status = "Queued" };
        return new CommunicationSendResultDto { OutboundMessageId = Guid.Empty, Status = "Queued" };
    }
}

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPramanCommunication(this IServiceCollection services, IConfiguration configuration)
    {
        var baseUrl = configuration["CommunicationService:BaseUrl"]
            ?? configuration["Communication:BaseUrl"]
            ?? "https://localhost:7215";
        services.TryAddTransient<InternalApiKeyForwardingHandler>();
        services.AddHttpClient<ICommunicationClient, CommunicationHttpClient>(c =>
            c.BaseAddress = new Uri(baseUrl))
            .AddHttpMessageHandler<Shared.Logging.Correlation.CorrelationForwardingHandler>()
            .AddHttpMessageHandler<InternalApiKeyForwardingHandler>();
        return services;
    }
}
