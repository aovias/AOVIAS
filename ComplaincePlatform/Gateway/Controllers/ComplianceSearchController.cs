using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Gateway.DTOs;
using Gateway.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Infrastructure.Api;
using Shared.Infrastructure.Auth;
using Shared.Infrastructure.DTO;

namespace Gateway.Controllers;

/// <summary>
/// Phase 1 Sprint 5 — unified compliance search facade.
/// Preferentially thin-wraps ComplianceService <c>GET /api/v1/graph/search</c>
/// into a flat hit list. Optional evidence-library enrichment is best-effort and ignored on failure.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/compliance/search")]
[Authorize(Policy = AuthConstants.Policies.ComplianceScope)]
public sealed class ComplianceSearchController : ControllerBase
{
    private readonly IProxyService _proxy;
    private readonly ILogger<ComplianceSearchController> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: true) },
    };

    public ComplianceSearchController(IProxyService proxy, ILogger<ComplianceSearchController> logger)
    {
        _proxy = proxy;
        _logger = logger;
    }

    /// <summary>
    /// Aggregated search. Primary source: enterprise graph search.
    /// Filters <paramref name="hospitalId"/>, <paramref name="status"/>, <paramref name="owner"/>,
    /// and <paramref name="department"/> are applied post-aggregation when present on hits.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<ComplianceSearchResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<ComplianceSearchResponseDto>>> Search(
        [FromQuery] string? q = null,
        [FromQuery] Guid? hospitalId = null,
        [FromQuery] string? status = null,
        [FromQuery] string? owner = null,
        [FromQuery] string? department = null,
        CancellationToken cancellationToken = default)
    {
        var traceId = HttpContext.TraceIdentifier;
        var query = (q ?? string.Empty).Trim();

        if (query.Length < 2)
        {
            var empty = new ComplianceSearchResponseDto
            {
                Query = query,
                TotalResults = 0,
                Source = "graph",
                Results = Array.Empty<ComplianceSearchHitDto>(),
            };
            var ok = ApiResponseFactory.Success(ApiResponseCode.SUCCESS, empty, traceId);
            return StatusCode(ApiResponseCodes.Registry[ApiResponseCode.SUCCESS].HttpStatus, ok);
        }

        var apiVersion = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";
        var graphQs = new StringBuilder($"?q={Uri.EscapeDataString(query)}");
        if (hospitalId.HasValue && hospitalId.Value != Guid.Empty)
        {
            graphQs.Append("&hospitalId=").Append(hospitalId.Value);
        }

        var graphEndpoint = $"/api/v{apiVersion}/graph/search{graphQs}";
        var graphResult = await _proxy.ProxyToComplianceServiceAsync(
            graphEndpoint, HttpMethod.Get, null, null, cancellationToken);

        var hits = new List<ComplianceSearchHitDto>();
        var source = "graph";

        if (TryReadContent(graphResult, out var statusCode, out var content)
            && statusCode == (int)HttpStatusCode.OK
            && !string.IsNullOrWhiteSpace(content))
        {
            var envelope = JsonSerializer.Deserialize<ApiEnvelope<GraphSearchPayload>>(content, JsonOptions);
            var graph = envelope?.Data;
            if (graph?.Groups != null)
            {
                foreach (var group in graph.Groups)
                {
                    foreach (var item in group.Items ?? new List<GraphSearchItem>())
                    {
                        hits.Add(new ComplianceSearchHitDto
                        {
                            Type = string.IsNullOrWhiteSpace(item.EntityType) ? group.EntityType : item.EntityType,
                            Id = item.EntityId.ToString(),
                            Title = item.Title ?? string.Empty,
                            Status = item.Status ?? string.Empty,
                            HospitalId = hospitalId,
                            Department = null,
                            Owner = null,
                            Href = item.Route,
                            Subtitle = item.Subtitle,
                        });
                    }
                }
            }
        }
        else
        {
            _logger.LogWarning(
                "Graph search unavailable for q={Query}; returning empty facade result. Status={Status}",
                query, statusCode);
        }

        // Optional cheap evidence enrichment (best-effort; never fails the facade).
        try
        {
            var evidenceQs = new StringBuilder($"?search={Uri.EscapeDataString(query)}&page=1&pageSize=10");
            if (hospitalId.HasValue && hospitalId.Value != Guid.Empty)
            {
                evidenceQs.Append("&hospitalId=").Append(hospitalId.Value);
            }

            var evidenceEndpoint = $"/api/v{apiVersion}/evidence/list{evidenceQs}";
            var evidenceResult = await _proxy.ProxyToComplianceServiceAsync(
                evidenceEndpoint, HttpMethod.Get, null, null, cancellationToken);

            if (TryReadContent(evidenceResult, out var evStatus, out var evContent)
                && evStatus == (int)HttpStatusCode.OK
                && !string.IsNullOrWhiteSpace(evContent))
            {
                var existingIds = new HashSet<string>(hits.Select(h => h.Id), StringComparer.OrdinalIgnoreCase);
                var evEnvelope = JsonSerializer.Deserialize<ApiEnvelope<EvidenceListPayload>>(evContent, JsonOptions);
                var items = evEnvelope?.Data?.Items ?? evEnvelope?.Data?.Results;
                if (items != null)
                {
                    foreach (var ev in items.Take(10))
                    {
                        var id = ev.Id ?? ev.EvidenceId;
                        if (string.IsNullOrWhiteSpace(id) || !existingIds.Add(id))
                        {
                            continue;
                        }

                        hits.Add(new ComplianceSearchHitDto
                        {
                            Type = "evidence",
                            Id = id,
                            Title = ev.Title ?? ev.FileName ?? "Evidence",
                            Status = ev.Status ?? ev.VerificationStatus ?? string.Empty,
                            HospitalId = hospitalId ?? ParseGuid(ev.HospitalId),
                            Department = ev.Department,
                            Owner = ev.Owner ?? ev.UploadedBy,
                            Href = $"/app/evidence/{id}",
                        });
                    }

                    source = "graph+evidence";
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Optional evidence search enrichment skipped");
        }

        IEnumerable<ComplianceSearchHitDto> filtered = hits;
        if (!string.IsNullOrWhiteSpace(status))
        {
            filtered = filtered.Where(h =>
                h.Status.Contains(status, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(owner))
        {
            filtered = filtered.Where(h =>
                !string.IsNullOrWhiteSpace(h.Owner)
                && h.Owner.Contains(owner, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(department))
        {
            filtered = filtered.Where(h =>
                !string.IsNullOrWhiteSpace(h.Department)
                && h.Department.Contains(department, StringComparison.OrdinalIgnoreCase));
        }

        var results = filtered.ToList();
        var response = new ComplianceSearchResponseDto
        {
            Query = query,
            TotalResults = results.Count,
            Source = source,
            Results = results,
        };

        var success = ApiResponseFactory.Success(ApiResponseCode.SUCCESS, response, traceId);
        return StatusCode(ApiResponseCodes.Registry[ApiResponseCode.SUCCESS].HttpStatus, success);
    }

    private static bool TryReadContent(IActionResult result, out int? statusCode, out string? content)
    {
        statusCode = null;
        content = null;
        if (result is ContentResult cr)
        {
            statusCode = cr.StatusCode;
            content = cr.Content;
            return true;
        }

        if (result is ObjectResult or)
        {
            statusCode = or.StatusCode;
            content = or.Value is string s
                ? s
                : or.Value != null
                    ? JsonSerializer.Serialize(or.Value, JsonOptions)
                    : null;
            return true;
        }

        return false;
    }

    private static Guid? ParseGuid(string? value) =>
        Guid.TryParse(value, out var g) ? g : null;

    private sealed class ApiEnvelope<T>
    {
        public T? Data { get; set; }
    }

    private sealed class GraphSearchPayload
    {
        public string? Query { get; set; }
        public int TotalResults { get; set; }
        public List<GraphSearchGroup>? Groups { get; set; }
    }

    private sealed class GraphSearchGroup
    {
        public string EntityType { get; set; } = string.Empty;
        public string? Label { get; set; }
        public List<GraphSearchItem>? Items { get; set; }
    }

    private sealed class GraphSearchItem
    {
        public Guid EntityId { get; set; }
        public string EntityType { get; set; } = string.Empty;
        public string? Title { get; set; }
        public string? Subtitle { get; set; }
        public string? Status { get; set; }
        public string? Route { get; set; }
    }

    private sealed class EvidenceListPayload
    {
        public List<EvidenceListItem>? Items { get; set; }
        public List<EvidenceListItem>? Results { get; set; }
    }

    private sealed class EvidenceListItem
    {
        public string? Id { get; set; }
        public string? EvidenceId { get; set; }
        public string? Title { get; set; }
        public string? FileName { get; set; }
        public string? Status { get; set; }
        public string? VerificationStatus { get; set; }
        public string? HospitalId { get; set; }
        public string? Department { get; set; }
        public string? Owner { get; set; }
        public string? UploadedBy { get; set; }
    }
}
