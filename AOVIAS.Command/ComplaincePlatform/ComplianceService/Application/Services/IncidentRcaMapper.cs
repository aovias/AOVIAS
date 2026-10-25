using System.Text.Json;
using ComplianceService.Application.DTOs;
using ComplianceService.Domain;

namespace ComplianceService.Application.Services;

/// <summary>Maps typed RCA DTOs ↔ persisted JSON columns on <see cref="IncidentRca"/>.</summary>
public static class IncidentRcaMapper
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    };

    public static void ApplySaveRequest(IncidentRca rca, SaveRcaRequestDto request)
    {
        if (request.FiveWhys != null)
            rca.FiveWhysJson = JsonSerializer.Serialize(request.FiveWhys, JsonOptions);
        else if (request.FiveWhysJson != null)
            rca.FiveWhysJson = request.FiveWhysJson;

        if (request.Fishbone != null)
            rca.FishboneJson = JsonSerializer.Serialize(request.Fishbone, JsonOptions);
        else if (request.FishboneJson != null)
            rca.FishboneJson = request.FishboneJson;

        if (request.CauseCategories != null)
        {
            rca.HumanFactorsJson = SerializeList(request.CauseCategories.Human);
            rca.ProcessFactorsJson = SerializeList(request.CauseCategories.Process);
            rca.EnvironmentalFactorsJson = SerializeList(request.CauseCategories.Environmental);
            rca.EquipmentFactorsJson = SerializeList(request.CauseCategories.Equipment);
            rca.OrganizationFactorsJson = SerializeList(request.CauseCategories.Organization);
        }
        else
        {
            if (request.HumanFactorsJson != null) rca.HumanFactorsJson = request.HumanFactorsJson;
            if (request.ProcessFactorsJson != null) rca.ProcessFactorsJson = request.ProcessFactorsJson;
            if (request.EnvironmentalFactorsJson != null) rca.EnvironmentalFactorsJson = request.EnvironmentalFactorsJson;
            if (request.EquipmentFactorsJson != null) rca.EquipmentFactorsJson = request.EquipmentFactorsJson;
            if (request.OrganizationFactorsJson != null) rca.OrganizationFactorsJson = request.OrganizationFactorsJson;
        }

        if (request.RootCauseSummary != null) rca.RootCauseSummary = request.RootCauseSummary;
        if (request.CorrectiveActionSummary != null) rca.CorrectiveActionSummary = request.CorrectiveActionSummary;
        if (request.PreventiveActionSummary != null) rca.PreventiveActionSummary = request.PreventiveActionSummary;
        if (request.RootCauseCategory != null) rca.RootCauseCategory = request.RootCauseCategory;
    }

    public static IncidentRcaDto ToDto(IncidentRca rca) => new()
    {
        Id = rca.Id,
        Method = rca.Method.ToString(),
        FiveWhys = ParseFiveWhys(rca.FiveWhysJson),
        CauseCategories = BuildCauseCategories(rca),
        Fishbone = ParseFishbone(rca.FishboneJson),
        RootCauseSummary = rca.RootCauseSummary,
        CorrectiveActionSummary = rca.CorrectiveActionSummary,
        PreventiveActionSummary = rca.PreventiveActionSummary,
        RootCauseCategory = rca.RootCauseCategory,
        FiveWhysJson = rca.FiveWhysJson,
        FishboneJson = rca.FishboneJson,
        HumanFactorsJson = rca.HumanFactorsJson,
        ProcessFactorsJson = rca.ProcessFactorsJson,
        EnvironmentalFactorsJson = rca.EnvironmentalFactorsJson,
        EquipmentFactorsJson = rca.EquipmentFactorsJson,
        OrganizationFactorsJson = rca.OrganizationFactorsJson,
    };

    public static FiveWhysDto? ParseFiveWhys(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind == JsonValueKind.Array)
            {
                var arr = doc.RootElement.EnumerateArray().Select(e => e.GetString() ?? string.Empty).ToList();
                return new FiveWhysDto
                {
                    Why1 = arr.ElementAtOrDefault(0),
                    Why2 = arr.ElementAtOrDefault(1),
                    Why3 = arr.ElementAtOrDefault(2),
                    Why4 = arr.ElementAtOrDefault(3),
                    Why5 = arr.ElementAtOrDefault(4),
                    FinalRootCause = arr.ElementAtOrDefault(4),
                };
            }

            return JsonSerializer.Deserialize<FiveWhysDto>(json, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public static FishboneSchemaDto? ParseFishbone(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            return JsonSerializer.Deserialize<FishboneSchemaDto>(json, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public static CauseCategoriesDto BuildCauseCategories(IncidentRca rca) => new()
    {
        Human = ParseStringList(rca.HumanFactorsJson),
        Process = ParseStringList(rca.ProcessFactorsJson),
        Environmental = ParseStringList(rca.EnvironmentalFactorsJson),
        Equipment = ParseStringList(rca.EquipmentFactorsJson),
        Organization = ParseStringList(rca.OrganizationFactorsJson),
    };

    private static string SerializeList(IList<string>? items) =>
        JsonSerializer.Serialize(items?.Where(x => !string.IsNullOrWhiteSpace(x)).ToList() ?? new List<string>(), JsonOptions);

    private static IList<string> ParseStringList(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new List<string>();
        try
        {
            var list = JsonSerializer.Deserialize<List<string>>(json, JsonOptions);
            return list ?? new List<string>();
        }
        catch (JsonException)
        {
            // Plain text fallback — one factor per non-empty line / comma
            return json.Split(new[] { '\n', ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList();
        }
    }
}
