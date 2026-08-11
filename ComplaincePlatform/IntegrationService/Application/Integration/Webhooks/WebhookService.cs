using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using IntegrationService.Application.Contracts;
using IntegrationService.Application.Integration.Platform;
using IntegrationService.Domain.ReadModels;
using IntegrationService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace IntegrationService.Application.Integration.Webhooks;

public sealed class WebhookService(
    IntegrationDbContext db,
    IHttpClientFactory httpClientFactory,
    IIntegrationAuditService audit,
    ISynchronizationEngine syncEngine,
    ILogger<WebhookService> logger) : IWebhookService
{
    private static readonly TimeSpan TimestampTolerance = TimeSpan.FromMinutes(5);

    public async Task<WebhookSubscriptionDto> CreateAsync(CreateWebhookRequest request, CancellationToken ct = default)
    {
        var secret = request.Secret ?? Guid.NewGuid().ToString("N");
        var filter = new WebhookRuleConfig
        {
            Events = request.EventFilters?.ToList() ?? [],
            TriggerSyncJobId = request.TriggerSyncJobId
        };
        var sub = new WebhookSubscription
        {
            Id = Guid.NewGuid(),
            OrganizationId = request.OrganizationId,
            Name = request.Name,
            Direction = request.Direction,
            TargetUrl = request.TargetUrl,
            SecretHash = HashSecret(secret),
            EventFilterJson = JsonSerializer.Serialize(filter),
            IsEnabled = true
        };
        db.WebhookSubscriptions.Add(sub);
        await db.SaveChangesAsync(ct);
        return new WebhookSubscriptionDto { Id = sub.Id, Name = sub.Name, Direction = sub.Direction, TargetUrl = sub.TargetUrl, IsEnabled = sub.IsEnabled };
    }

    public async Task<bool> ProcessInboundAsync(
        Guid subscriptionId,
        string payload,
        string signature,
        string? timestampHeader,
        string? idempotencyKey,
        CancellationToken ct = default)
    {
        var sub = await db.WebhookSubscriptions.FirstOrDefaultAsync(s => s.Id == subscriptionId && s.Direction == "Inbound" && s.IsEnabled, ct);
        if (sub is null) return false;
        if (!VerifySignature(payload, sub.SecretHash, signature)) return false;
        if (!IsTimestampValid(timestampHeader)) return false;

        var payloadHash = HashPayload(payload);
        if (!string.IsNullOrWhiteSpace(idempotencyKey))
        {
            var duplicate = await db.WebhookDeliveries.AnyAsync(d =>
                d.WebhookSubscriptionId == subscriptionId &&
                d.IdempotencyKey == idempotencyKey, ct);
            if (duplicate) return true;
        }
        else
        {
            var duplicate = await db.WebhookDeliveries.AnyAsync(d =>
                d.WebhookSubscriptionId == subscriptionId &&
                d.PayloadHash == payloadHash &&
                d.Status == "Delivered", ct);
            if (duplicate) return true;
        }

        db.WebhookDeliveries.Add(new WebhookDelivery
        {
            Id = Guid.NewGuid(),
            WebhookSubscriptionId = subscriptionId,
            EventType = "Inbound",
            PayloadHash = payloadHash,
            IdempotencyKey = idempotencyKey,
            Status = "Delivered",
            AttemptCount = 1,
            DeliveredAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync(ct);
        await audit.LogAsync(sub.OrganizationId, "WebhookInbound", "Success", null, null, "external", "integration", 1, 0, null, ct: ct);

        var rules = ParseRules(sub.EventFilterJson);
        if (rules.TriggerSyncJobId is Guid jobId)
        {
            try
            {
                await syncEngine.RunAsync(jobId, Guid.NewGuid().ToString(), null, ct);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Inbound webhook failed to trigger sync job {JobId}", jobId);
            }
        }

        return true;
    }

    public async Task DeliverOutboundAsync(string eventType, object payload, Guid organizationId, CancellationToken ct = default)
    {
        var subs = await db.WebhookSubscriptions.AsNoTracking()
            .Where(s => s.OrganizationId == organizationId && s.Direction == "Outbound" && s.IsEnabled && !s.IsDeleted)
            .ToListAsync(ct);
        var json = JsonSerializer.Serialize(payload);
        var client = httpClientFactory.CreateClient();
        foreach (var sub in subs)
        {
            if (!MatchesFilter(sub.EventFilterJson, eventType)) continue;
            var delivery = new WebhookDelivery
            {
                Id = Guid.NewGuid(),
                WebhookSubscriptionId = sub.Id,
                EventType = eventType,
                PayloadHash = HashPayload(json),
                Status = "Pending"
            };
            db.WebhookDeliveries.Add(delivery);
            for (var attempt = 1; attempt <= sub.MaxRetries; attempt++)
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(sub.TargetUrl)) break;
                    var sig = ComputeSignature(json, sub.SecretHash);
                    var req = new HttpRequestMessage(HttpMethod.Post, sub.TargetUrl) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
                    req.Headers.Add("X-Praman-Signature", sig);
                    req.Headers.Add("X-Praman-Timestamp", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString());
                    var resp = await client.SendAsync(req, ct);
                    if (resp.IsSuccessStatusCode)
                    {
                        delivery.Status = "Delivered";
                        delivery.DeliveredAtUtc = DateTime.UtcNow;
                        delivery.AttemptCount = attempt;
                        break;
                    }
                    delivery.LastError = $"HTTP {(int)resp.StatusCode}";
                }
                catch (Exception ex)
                {
                    delivery.LastError = ex.Message;
                    logger.LogWarning(ex, "Webhook delivery failed for {SubId}", sub.Id);
                }
                delivery.AttemptCount = attempt;
                if (attempt == sub.MaxRetries) { delivery.Status = "Failed"; delivery.IsDeadLetter = true; }
            }
            await db.SaveChangesAsync(ct);
        }
    }

    public async Task<IReadOnlyList<WebhookSubscriptionDto>> ListAsync(Guid organizationId, CancellationToken ct = default)
    {
        return await db.WebhookSubscriptions.AsNoTracking()
            .Where(s => s.OrganizationId == organizationId && !s.IsDeleted)
            .Select(s => new WebhookSubscriptionDto { Id = s.Id, Name = s.Name, Direction = s.Direction, TargetUrl = s.TargetUrl, IsEnabled = s.IsEnabled })
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<WebhookDeliveryDto>> ListDeliveriesAsync(Guid subscriptionId, CancellationToken ct = default)
    {
        return await db.WebhookDeliveries.AsNoTracking()
            .Where(d => d.WebhookSubscriptionId == subscriptionId)
            .OrderByDescending(d => d.DeliveredAtUtc)
            .Take(100)
            .Select(d => new WebhookDeliveryDto
            {
                Id = d.Id,
                EventType = d.EventType,
                Status = d.Status,
                AttemptCount = d.AttemptCount,
                DeliveredAtUtc = d.DeliveredAtUtc,
                LastError = d.LastError
            }).ToListAsync(ct);
    }

    private static bool IsTimestampValid(string? timestampHeader)
    {
        if (string.IsNullOrWhiteSpace(timestampHeader)) return true;
        if (long.TryParse(timestampHeader, out var unix))
        {
            var ts = DateTimeOffset.FromUnixTimeSeconds(unix);
            return Math.Abs((DateTimeOffset.UtcNow - ts).TotalMinutes) <= TimestampTolerance.TotalMinutes;
        }
        if (DateTimeOffset.TryParse(timestampHeader, out var parsed))
            return Math.Abs((DateTimeOffset.UtcNow - parsed).TotalMinutes) <= TimestampTolerance.TotalMinutes;
        return false;
    }

    private static bool MatchesFilter(string filterJson, string eventType)
    {
        var rules = ParseRules(filterJson);
        return rules.Events.Count == 0 || rules.Events.Contains(eventType, StringComparer.OrdinalIgnoreCase);
    }

    private static WebhookRuleConfig ParseRules(string filterJson)
    {
        try
        {
            var rules = JsonSerializer.Deserialize<WebhookRuleConfig>(filterJson);
            if (rules is not null) return rules;
            var legacy = JsonSerializer.Deserialize<List<string>>(filterJson) ?? [];
            return new WebhookRuleConfig { Events = legacy };
        }
        catch
        {
            return new WebhookRuleConfig();
        }
    }

    private static string HashSecret(string secret) => Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(secret)));
    private static string HashPayload(string payload) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload)));
    private static string ComputeSignature(string payload, string secretHash) => Convert.ToBase64String(HMACSHA256.HashData(Encoding.UTF8.GetBytes(secretHash), Encoding.UTF8.GetBytes(payload)));
    private static bool VerifySignature(string payload, string secretHash, string signature)
    {
        if (string.IsNullOrWhiteSpace(signature)) return false;
        var expected = ComputeSignature(payload, secretHash);
        return CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(expected), Encoding.UTF8.GetBytes(signature));
    }

    private sealed class WebhookRuleConfig
    {
        public List<string> Events { get; set; } = [];
        public Guid? TriggerSyncJobId { get; set; }
    }
}
