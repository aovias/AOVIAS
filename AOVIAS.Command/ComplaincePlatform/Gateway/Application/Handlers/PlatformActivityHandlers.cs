using System.Text.Json;
using System.Text.RegularExpressions;
using Gateway.Application.Queries;
using Gateway.DTOs;
using Gateway.Services;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Shared.Infrastructure.Api;
using Shared.Infrastructure.DTO;

namespace Gateway.Application.Handlers;

public sealed class PlatformActivityHandlers
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public sealed class GetPlatformActivityHandler(IProxyService proxy, IHttpContextAccessor httpContextAccessor, ILogger<GetPlatformActivityHandler> logger)
        : IRequestHandler<GetPlatformActivityQuery, IActionResult>
    {
        public async Task<IActionResult> Handle(GetPlatformActivityQuery request, CancellationToken ct)
        {
            var traceId = httpContextAccessor.HttpContext?.TraceIdentifier ?? Guid.NewGuid().ToString();
            var entries = new List<PlatformActivityEntryDto>();

            await CollectIdentityAsync(request, entries, ct);
            await CollectOrganizationAsync(request, entries, ct);
            await CollectIntegrationAsync(request, entries, ct);
            await CollectComplianceAsync(request, entries, ct);
            await CollectNotificationsAsync(request, entries, ct);
            await CollectAiAsync(request, entries, ct);

            entries = ApplyFilters(entries, request).ToList();
            var summary = BuildSummary(entries);

            var sortDesc = !string.Equals(request.Sort, "asc", StringComparison.OrdinalIgnoreCase);
            entries = (sortDesc
                    ? entries.OrderByDescending(e => e.TimestampUtc)
                    : entries.OrderBy(e => e.TimestampUtc))
                .ToList();

            var total = entries.Count;
            var page = Math.Max(1, request.Page);
            var pageSize = Math.Clamp(request.PageSize, 1, 200);
            var paged = entries.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            var response = new PlatformActivityResponseDto
            {
                Summary = summary,
                Items = paged,
                TotalCount = total,
                Page = page,
                PageSize = pageSize,
            };

            var envelope = ApiResponseFactory.Success(ApiResponseCode.SUCCESS, response, traceId);
            return new ObjectResult(envelope) { StatusCode = ApiResponseCodes.Registry[ApiResponseCode.SUCCESS].HttpStatus };
        }

        private async Task CollectIdentityAsync(GetPlatformActivityQuery request, List<PlatformActivityEntryDto> entries, CancellationToken ct)
        {
            try
            {
                var endpoint = AppendQuery($"/api/v{request.ApiVersion}/security/audit-logs", BuildDateQuery(request.From, request.To, "from", "to"), "take=200");
                var result = await proxy.ProxyToIdentityServiceAsync(endpoint, HttpMethod.Get, cancellationToken: ct);
                var logs = await ReadListAsync<IdentityAuditLogEnvelope>(result);
                foreach (var log in logs)
                {
                    var category = ClassifyIdentity(log.EventCode, log.Action);
                    entries.Add(new PlatformActivityEntryDto
                    {
                        Id = $"identity:{log.Id}",
                        TimestampUtc = log.OccurredAt,
                        Category = category,
                        Action = log.Action,
                        Module = "Identity",
                        Service = "IdentityService",
                        UserId = log.ActorUserId?.ToString(),
                        Target = log.TargetUserId?.ToString(),
                        Status = log.Outcome,
                        IpAddress = log.IpAddress,
                        CorrelationId = log.CorrelationId,
                        PreviousValue = MaskSensitive(log.OldValue),
                        NewValue = MaskSensitive(log.NewValue),
                        DetailsJson = log.Reason,
                    });
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to collect identity audit activity");
            }
        }

        private async Task CollectOrganizationAsync(GetPlatformActivityQuery request, List<PlatformActivityEntryDto> entries, CancellationToken ct)
        {
            try
            {
                var endpoint = AppendQuery($"/api/v{request.ApiVersion}/organizations/{request.OrganizationId}/settings/audit-logs", BuildDateQuery(request.From, request.To, "from", "to"), "take=200");
                var result = await proxy.ProxyToOrganizationServiceAsync(endpoint, HttpMethod.Get, cancellationToken: ct);
                var logs = await ReadListAsync<OrganizationAuditLogEnvelope>(result);
                foreach (var log in logs)
                {
                    entries.Add(new PlatformActivityEntryDto
                    {
                        Id = $"organization:{log.Id}",
                        TimestampUtc = log.OccurredAt,
                        Category = "Configuration",
                        Action = log.Action,
                        Module = "Organization",
                        Service = "OrganizationService",
                        UserId = log.ActorUserId.ToString(),
                        Target = $"{log.EntityType}:{log.EntityId}",
                        Status = "Success",
                    });
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to collect organization audit activity");
            }
        }

        private async Task CollectIntegrationAsync(GetPlatformActivityQuery request, List<PlatformActivityEntryDto> entries, CancellationToken ct)
        {
            try
            {
                var result = await proxy.ProxyToIntegrationServiceAsync(
                    $"/api/v{request.ApiVersion}/integration-audit?organizationId={request.OrganizationId}",
                    HttpMethod.Get,
                    cancellationToken: ct);
                var logs = await ReadListAsync<IntegrationAuditEnvelope>(result);
                foreach (var log in logs)
                {
                    entries.Add(new PlatformActivityEntryDto
                    {
                        Id = $"integration:{log.Id}",
                        TimestampUtc = log.OccurredAtUtc,
                        Category = "Integration",
                        Action = log.Operation,
                        Module = "Enterprise Integrations",
                        Service = "IntegrationService",
                        Target = log.TraceId,
                        Status = log.Result,
                        CorrelationId = log.TraceId,
                        DetailsJson = $"{{\"recordsProcessed\":{log.RecordsProcessed},\"durationMs\":{log.DurationMs}}}",
                    });
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to collect integration audit activity");
            }
        }

        private async Task CollectComplianceAsync(GetPlatformActivityQuery request, List<PlatformActivityEntryDto> entries, CancellationToken ct)
        {
            try
            {
                var endpoint = AppendQuery($"/api/v{request.ApiVersion}/audit-logs", BuildDateQuery(request.From, request.To, "from", "to"));
                var result = await proxy.ProxyToComplianceServiceAsync(endpoint, HttpMethod.Get, cancellationToken: ct);
                var logs = await ReadListAsync<ComplianceAuditLogEnvelope>(result);
                foreach (var log in logs)
                {
                    entries.Add(new PlatformActivityEntryDto
                    {
                        Id = $"compliance:{log.Id}",
                        TimestampUtc = log.Timestamp,
                        Category = "Compliance",
                        Action = log.Action,
                        Module = "Compliance",
                        Service = "ComplianceService",
                        UserId = log.PerformedBy.ToString(),
                        Target = $"{log.EntityType}:{log.EntityId}",
                        Status = "Success",
                        DetailsJson = MaskSensitive(log.MetadataJson),
                    });
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to collect compliance audit activity");
            }
        }

        private async Task CollectNotificationsAsync(GetPlatformActivityQuery request, List<PlatformActivityEntryDto> entries, CancellationToken ct)
        {
            try
            {
                var result = await proxy.ProxyToCommunicationServiceAsync(
                    $"/api/v{request.ApiVersion}/communication/delivery-history?take=200",
                    HttpMethod.Get,
                    cancellationToken: ct);
                var logs = await ReadListAsync<DeliveryHistoryEnvelope>(result);
                foreach (var log in logs)
                {
                    if (request.From.HasValue && log.SentAt.HasValue && log.SentAt < request.From) continue;
                    if (request.To.HasValue && log.SentAt.HasValue && log.SentAt > request.To) continue;
                    entries.Add(new PlatformActivityEntryDto
                    {
                        Id = $"notification:{log.Id}",
                        TimestampUtc = log.SentAt ?? DateTime.UtcNow,
                        Category = "Notification",
                        Action = log.EventType ?? "Delivery",
                        Module = "Notifications",
                        Service = "CommunicationService",
                        Target = log.RecipientPersonId?.ToString(),
                        Status = log.Status,
                        CorrelationId = log.CorrelationId,
                        DetailsJson = MaskSensitive(log.FailureReason),
                    });
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to collect notification delivery activity");
            }
        }

        private async Task CollectAiAsync(GetPlatformActivityQuery request, List<PlatformActivityEntryDto> entries, CancellationToken ct)
        {
            try
            {
                var result = await proxy.ProxyToAIServiceAsync(
                    $"/api/v{request.ApiVersion}/ai/executions?page=1&pageSize=100",
                    HttpMethod.Get,
                    cancellationToken: ct);
                var page = await ReadObjectAsync<PagedAiExecutionsEnvelope>(result);
                foreach (var log in page?.Items ?? [])
                {
                    if (request.From.HasValue && log.OccurredAtUtc < request.From) continue;
                    if (request.To.HasValue && log.OccurredAtUtc > request.To) continue;
                    entries.Add(new PlatformActivityEntryDto
                    {
                        Id = $"ai:{log.Id}",
                        TimestampUtc = log.OccurredAtUtc,
                        Category = "AI",
                        Action = log.UseCase,
                        Module = "AI",
                        Service = "AIOrchestratorService",
                        Target = log.ProfileId.ToString(),
                        Status = log.Outcome,
                        CorrelationId = log.TraceId,
                        DetailsJson = $"{{\"model\":\"{log.ModelName}\",\"provider\":\"{log.ModelProvider}\",\"latencyMs\":{log.LatencyMs}}}",
                    });
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to collect AI execution activity");
            }
        }

        private static IEnumerable<PlatformActivityEntryDto> ApplyFilters(IEnumerable<PlatformActivityEntryDto> entries, GetPlatformActivityQuery request)
        {
            var q = entries;
            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                var term = request.Search.Trim();
                q = q.Where(e =>
                    e.Action.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    e.Module.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    (e.Target?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (e.CorrelationId?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false));
            }
            if (!string.IsNullOrWhiteSpace(request.Module))
                q = q.Where(e => e.Module.Equals(request.Module, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(request.Category))
                q = q.Where(e => e.Category.Equals(request.Category, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(request.Action))
                q = q.Where(e => e.Action.Contains(request.Action, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(request.UserId))
                q = q.Where(e => e.UserId == request.UserId);
            if (!string.IsNullOrWhiteSpace(request.Status))
                q = q.Where(e => e.Status.Contains(request.Status, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(request.Service))
                q = q.Where(e => e.Service.Contains(request.Service, StringComparison.OrdinalIgnoreCase));
            return q;
        }

        private static PlatformActivitySummaryDto BuildSummary(IReadOnlyList<PlatformActivityEntryDto> entries)
        {
            return new PlatformActivitySummaryDto
            {
                AdministrativeActions = entries.Count(e => e.Category is "Configuration" or "Compliance"),
                SecurityEvents = entries.Count(e => e.Category == "Security"),
                ConfigurationChanges = entries.Count(e => e.Category == "Configuration"),
                UserManagementEvents = entries.Count(e => e.Category == "User Management"),
                IntegrationEvents = entries.Count(e => e.Category == "Integration"),
                NotificationEvents = entries.Count(e => e.Category == "Notification"),
                RecentErrors = entries.Count(e => Regex.IsMatch(e.Status, "fail|error", RegexOptions.IgnoreCase)),
            };
        }

        private static string ClassifyIdentity(string eventCode, string action)
        {
            var text = eventCode + action;
            if (Regex.IsMatch(text, "password|login|mfa|session|security|lock", RegexOptions.IgnoreCase))
                return "Security";
            if (Regex.IsMatch(text, "user|role|membership|register", RegexOptions.IgnoreCase))
                return "User Management";
            return "Security";
        }

        private static string BuildDateQuery(DateTime? from, DateTime? to, string fromKey, string toKey)
        {
            var parts = new List<string>();
            if (from.HasValue) parts.Add($"{fromKey}={Uri.EscapeDataString(from.Value.ToString("O"))}");
            if (to.HasValue) parts.Add($"{toKey}={Uri.EscapeDataString(to.Value.ToString("O"))}");
            return parts.Count == 0 ? string.Empty : string.Join("&", parts);
        }

        private static string AppendQuery(string basePath, params string[] parts)
        {
            var query = string.Join("&", parts.Where(p => !string.IsNullOrWhiteSpace(p)));
            return string.IsNullOrWhiteSpace(query) ? basePath : $"{basePath}?{query}";
        }

        private static async Task<List<T>> ReadListAsync<T>(IActionResult result)
        {
            var data = await ReadEnvelopeAsync<List<T>>(result);
            return data ?? [];
        }

        private static async Task<T?> ReadObjectAsync<T>(IActionResult result) where T : class
            => await ReadEnvelopeAsync<T>(result);

        private static async Task<T?> ReadEnvelopeAsync<T>(IActionResult result)
        {
            if (result is ContentResult content && !string.IsNullOrEmpty(content.Content))
            {
                if ((content.StatusCode ?? 0) >= 400) return default;
                var envelope = JsonSerializer.Deserialize<ApiResponse<T>>(content.Content, JsonOptions);
                return envelope?.Success == true ? envelope.Data : default;
            }
            return default;
        }

        private static string? MaskSensitive(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return value;
            if (SensitivePattern.IsMatch(value)) return "••••••••";
            return value.Length > 120 ? value[..120] + "…" : value;
        }

        private static readonly Regex SensitivePattern = new(
            "(password|secret|apikey|api_key|token|credential)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);
    }

    private sealed class IdentityAuditLogEnvelope
    {
        public Guid Id { get; set; }
        public DateTime OccurredAt { get; set; }
        public Guid? ActorUserId { get; set; }
        public Guid? TargetUserId { get; set; }
        public string EventCode { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string? OldValue { get; set; }
        public string? NewValue { get; set; }
        public string? IpAddress { get; set; }
        public string? CorrelationId { get; set; }
        public string Outcome { get; set; } = string.Empty;
        public string? Reason { get; set; }
    }

    private sealed class OrganizationAuditLogEnvelope
    {
        public Guid Id { get; set; }
        public string EntityType { get; set; } = string.Empty;
        public Guid EntityId { get; set; }
        public string Action { get; set; } = string.Empty;
        public Guid ActorUserId { get; set; }
        public DateTime OccurredAt { get; set; }
    }

    private sealed class IntegrationAuditEnvelope
    {
        public Guid Id { get; set; }
        public string Operation { get; set; } = string.Empty;
        public string Result { get; set; } = string.Empty;
        public int RecordsProcessed { get; set; }
        public long DurationMs { get; set; }
        public string? TraceId { get; set; }
        public DateTime OccurredAtUtc { get; set; }
    }

    private sealed class ComplianceAuditLogEnvelope
    {
        public Guid Id { get; set; }
        public string EntityType { get; set; } = string.Empty;
        public Guid EntityId { get; set; }
        public string Action { get; set; } = string.Empty;
        public Guid PerformedBy { get; set; }
        public DateTime Timestamp { get; set; }
        public string MetadataJson { get; set; } = "{}";
    }

    private sealed class DeliveryHistoryEnvelope
    {
        public Guid Id { get; set; }
        public Guid? RecipientPersonId { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime? SentAt { get; set; }
        public string? FailureReason { get; set; }
        public string? CorrelationId { get; set; }
        public string? EventType { get; set; }
    }

    private sealed class PagedAiExecutionsEnvelope
    {
        public List<AiExecutionEnvelope> Items { get; set; } = [];
    }

    private sealed class AiExecutionEnvelope
    {
        public Guid Id { get; set; }
        public Guid ProfileId { get; set; }
        public string UseCase { get; set; } = string.Empty;
        public string ModelProvider { get; set; } = string.Empty;
        public string ModelName { get; set; } = string.Empty;
        public long LatencyMs { get; set; }
        public string Outcome { get; set; } = string.Empty;
        public string? TraceId { get; set; }
        public DateTime OccurredAtUtc { get; set; }
    }
}
