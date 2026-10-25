using System.Text.Json;
using Gateway.Application.Queries;
using Gateway.Services;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Shared.Infrastructure.Api;
using Shared.Infrastructure.DTO;

namespace Gateway.Application.Handlers;

/// <summary>
/// Phase 4 Sprint 6 — merges Governance committee KPIs with Compliance domain enrichment.
/// </summary>
public sealed class GetAnalyticsGovernanceQueryHandler(IProxyService proxy)
    : IRequestHandler<GetAnalyticsGovernanceQuery, IActionResult>
{
    public async Task<IActionResult> Handle(GetAnalyticsGovernanceQuery request, CancellationToken cancellationToken)
    {
        var govResult = await proxy.ProxyToGovernanceServiceAsync(
            $"/api/v{request.ApiVersion}/governance/analytics/kpis",
            HttpMethod.Get,
            cancellationToken: cancellationToken);

        var complianceResult = await proxy.ProxyToComplianceServiceAsync(
            $"/api/v{request.ApiVersion}/analytics/governance",
            HttpMethod.Get,
            null,
            null,
            cancellationToken);

        var govJson = ExtractDataJson(govResult);
        var complianceJson = ExtractDataJson(complianceResult);

        using var govDoc = govJson != null ? JsonDocument.Parse(govJson) : null;
        using var complianceDoc = complianceJson != null ? JsonDocument.Parse(complianceJson) : null;

        var merged = new Dictionary<string, object?>
        {
            ["activeCommittees"] = ReadInt(govDoc, "activeCommittees"),
            ["openActionItems"] = ReadInt(govDoc, "openActionItems"),
            ["meetingsThisMonth"] = ReadInt(govDoc, "meetingsThisMonth"),
            ["pendingMinutesApprovals"] = ReadInt(govDoc, "pendingMinutesApprovals"),
            ["documentsEffective"] = ReadInt(complianceDoc, "documentsEffective"),
            ["trainingOverdue"] = ReadInt(complianceDoc, "trainingOverdue"),
            ["openIncidents"] = ReadInt(complianceDoc, "openIncidents"),
            ["openRisks"] = ReadInt(complianceDoc, "openRisks"),
            ["sourceNotes"] = string.Join(" ", new[]
            {
                ReadString(govDoc, "sourceNotes"),
                ReadString(complianceDoc, "notes")
            }.Where(s => !string.IsNullOrWhiteSpace(s)))
        };

        var envelope = ApiResponseFactory.Success(ApiResponseCode.REPORT_GENERATED, merged, "gateway-governance-analytics");
        var meta = ApiResponseCodes.Registry[ApiResponseCode.REPORT_GENERATED];
        return new ObjectResult(envelope) { StatusCode = meta.HttpStatus };
    }

    private static string? ExtractDataJson(IActionResult result)
    {
        if (result is ContentResult { Content: { Length: > 0 } content })
        {
            try
            {
                using var doc = JsonDocument.Parse(content);
                if (doc.RootElement.TryGetProperty("data", out var data))
                    return data.GetRawText();
                return content;
            }
            catch { return null; }
        }

        if (result is ObjectResult { Value: not null } obj)
        {
            try
            {
                var json = JsonSerializer.Serialize(obj.Value);
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("data", out var data)
                    || doc.RootElement.TryGetProperty("Data", out data))
                    return data.GetRawText();
            }
            catch { /* ignore */ }
        }

        return null;
    }

    private static int ReadInt(JsonDocument? doc, string name)
    {
        if (doc == null) return 0;
        foreach (var prop in doc.RootElement.EnumerateObject())
        {
            if (string.Equals(prop.Name, name, StringComparison.OrdinalIgnoreCase)
                && prop.Value.ValueKind == JsonValueKind.Number
                && prop.Value.TryGetInt32(out var n))
                return n;
        }
        return 0;
    }

    private static string ReadString(JsonDocument? doc, string name)
    {
        if (doc == null) return string.Empty;
        foreach (var prop in doc.RootElement.EnumerateObject())
        {
            if (string.Equals(prop.Name, name, StringComparison.OrdinalIgnoreCase)
                && prop.Value.ValueKind == JsonValueKind.String)
                return prop.Value.GetString() ?? string.Empty;
        }
        return string.Empty;
    }
}
