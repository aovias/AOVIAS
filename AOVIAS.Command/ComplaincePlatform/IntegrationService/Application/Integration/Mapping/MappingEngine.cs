using System.Globalization;
using System.Text.Json;
using IntegrationService.Application.Contracts;
using IntegrationService.Application.Integration.Platform;
using IntegrationService.Domain.ReadModels;
using IntegrationService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace IntegrationService.Application.Integration.Mapping;

public sealed class MappingEngine : IMappingEngine
{
    public Task<MappedRecord> MapAsync(ExternalRecord source, MappingProfile profile, IReadOnlyList<FieldMapping> fields, IReadOnlyList<TransformationRule> rules, CancellationToken ct = default)
    {
        var result = new MappedRecord { TargetEntity = profile.TargetEntity };
        var ruleMap = rules.ToDictionary(r => r.Id);
        foreach (var fm in fields.OrderBy(f => f.SortOrder))
        {
            source.Fields.TryGetValue(fm.SourceField, out var raw);
            if (fm.TransformationRuleId.HasValue && ruleMap.TryGetValue(fm.TransformationRuleId.Value, out var rule))
                raw = ApplyRule(raw, rule);
            result.Fields[fm.TargetField] = raw;
        }
        return Task.FromResult(result);
    }

    public Task<ConnectorValidationResult> ValidateProfileAsync(MappingProfile profile, IReadOnlyList<FieldMapping> fields, CancellationToken ct = default)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(profile.Name)) errors.Add("Profile name required");
        if (fields.Count == 0) errors.Add("At least one field mapping required");
        foreach (var f in fields.Where(f => string.IsNullOrWhiteSpace(f.SourceField) || string.IsNullOrWhiteSpace(f.TargetField)))
            errors.Add("Source and target fields required");
        return Task.FromResult(new ConnectorValidationResult { IsValid = errors.Count == 0, Errors = errors });
    }

    private static string? ApplyRule(string? value, TransformationRule rule)
    {
        using var doc = JsonDocument.Parse(rule.RulesJson);
        var root = doc.RootElement;
        return rule.RuleType switch
        {
            "String" => value?.Trim(),
            "Date" => DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d) ? d.ToString("O") : value,
            "Number" => decimal.TryParse(value, out var n) ? n.ToString(CultureInfo.InvariantCulture) : value,
            "Boolean" => bool.TryParse(value, out var b) ? b.ToString() : value,
            "DefaultValue" => string.IsNullOrWhiteSpace(value) && root.TryGetProperty("default", out var def) ? def.GetString() : value,
            "Lookup" => ApplyLookup(value, root),
            "Expression" => value,
            _ => value
        };
    }

    private static string? ApplyLookup(string? value, JsonElement root)
    {
        if (value is null || !root.TryGetProperty("map", out var map)) return value;
        foreach (var p in map.EnumerateObject())
            if (p.Name.Equals(value, StringComparison.OrdinalIgnoreCase)) return p.Value.GetString();
        return value;
    }
}

public sealed class MappingProfileService(IntegrationDbContext db)
{
    public async Task<MappingProfileDto> CreateAsync(CreateMappingProfileRequest request, CancellationToken ct = default)
    {
        var profile = new MappingProfile
        {
            Id = Guid.NewGuid(),
            OrganizationId = request.OrganizationId,
            Name = request.Name,
            SourceEntity = request.SourceEntity,
            TargetEntity = request.TargetEntity
        };
        db.MappingProfiles.Add(profile);
        var order = 0;
        foreach (var f in request.Fields)
        {
            db.FieldMappings.Add(new FieldMapping
            {
                Id = Guid.NewGuid(),
                MappingProfileId = profile.Id,
                SourceField = f.SourceField,
                TargetField = f.TargetField,
                TransformationRuleId = f.TransformationRuleId,
                SortOrder = order++
            });
        }
        await db.SaveChangesAsync(ct);
        return await GetAsync(profile.Id, ct) ?? throw new InvalidOperationException();
    }

    public async Task<MappingProfileDto?> GetAsync(Guid id, CancellationToken ct = default)
    {
        var p = await db.MappingProfiles.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);
        if (p is null) return null;
        var fields = await db.FieldMappings.AsNoTracking().Where(f => f.MappingProfileId == id).OrderBy(f => f.SortOrder).ToListAsync(ct);
        return new MappingProfileDto
        {
            Id = p.Id,
            Name = p.Name,
            SourceEntity = p.SourceEntity,
            TargetEntity = p.TargetEntity,
            Fields = fields.Select(f => new FieldMappingDto { Id = f.Id, SourceField = f.SourceField, TargetField = f.TargetField, TransformationRuleId = f.TransformationRuleId }).ToList()
        };
    }

    public async Task<IReadOnlyList<MappingProfileDto>> ListAsync(Guid organizationId, CancellationToken ct = default)
    {
        var profiles = await db.MappingProfiles.AsNoTracking().Where(p => p.OrganizationId == organizationId && !p.IsDeleted).ToListAsync(ct);
        var result = new List<MappingProfileDto>();
        foreach (var p in profiles) { var dto = await GetAsync(p.Id, ct); if (dto is not null) result.Add(dto); }
        return result;
    }
}
