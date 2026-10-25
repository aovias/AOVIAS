using System.Collections.Concurrent;
using System.Text.Json;
using OrganizationService.Domain.Entities;

namespace OrganizationService.Application.Services;

public enum EntitlementKind
{
    Boolean = 0,
    Numeric = 1,
    Usage = 2,
    TimeBased = 3,
    Custom = 4
}

public enum FeatureAccessDecision
{
    Enabled = 0,
    Disabled = 1,
    Limited = 2
}

public sealed class EntitlementDefinition
{
    public string Code { get; init; } = string.Empty;
    public EntitlementKind Kind { get; init; } = EntitlementKind.Boolean;
    public double? Value { get; init; }
    public DateTime? ExpiresAtUtc { get; init; }
    public string? CustomPayload { get; init; }
}

public sealed class EntitlementEvaluation
{
    public string Code { get; init; } = string.Empty;
    public bool Allowed { get; init; }
    public FeatureAccessDecision Decision { get; init; } = FeatureAccessDecision.Disabled;
    public EntitlementKind Kind { get; init; } = EntitlementKind.Boolean;
    public double? Limit { get; init; }
    public string? Message { get; init; }
}

/// <summary>CRP Phase 6 — typed entitlement evaluation over LicenseKey.EntitlementsJson.</summary>
public sealed class EntitlementEngine
{
    private static readonly ConcurrentDictionary<string, (DateTime CachedAt, IReadOnlyList<EntitlementDefinition> Items)> ParseCache = new();
    private static readonly TimeSpan ParseCacheTtl = TimeSpan.FromSeconds(30);

    private readonly TimeSpan _evaluationCacheTtl;
    private readonly ConcurrentDictionary<string, (DateTime CachedAt, EntitlementEvaluation Result)> _evalCache = new();

    public EntitlementEngine(IConfiguration configuration)
    {
        var seconds = configuration.GetValue("Licensing:EntitlementCacheSeconds", 30);
        _evaluationCacheTtl = TimeSpan.FromSeconds(Math.Clamp(seconds, 5, 300));
    }

    public IReadOnlyList<EntitlementDefinition> Parse(string? entitlementsJson)
    {
        var json = string.IsNullOrWhiteSpace(entitlementsJson) ? "[]" : entitlementsJson.Trim();
        if (ParseCache.TryGetValue(json, out var cached)
            && DateTime.UtcNow - cached.CachedAt < ParseCacheTtl)
        {
            return cached.Items;
        }

        var items = ParseCore(json);
        ParseCache[json] = (DateTime.UtcNow, items);
        return items;
    }

    public EntitlementEvaluation Evaluate(string? entitlementsJson, string featureCode, bool licenseOperational)
    {
        var code = (featureCode ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(code))
        {
            return new EntitlementEvaluation
            {
                Code = code,
                Allowed = false,
                Decision = FeatureAccessDecision.Disabled,
                Message = "Feature code required."
            };
        }

        var cacheKey = $"{licenseOperational}|{code}|{entitlementsJson ?? "[]"}";
        if (_evalCache.TryGetValue(cacheKey, out var hit)
            && DateTime.UtcNow - hit.CachedAt < _evaluationCacheTtl)
        {
            return hit.Result;
        }

        var result = EvaluateCore(Parse(entitlementsJson), code, licenseOperational);
        _evalCache[cacheKey] = (DateTime.UtcNow, result);
        return result;
    }

    public IReadOnlyList<EntitlementEvaluation> EvaluateAll(string? entitlementsJson, bool licenseOperational)
    {
        return Parse(entitlementsJson)
            .Select(d => EvaluateCore(Parse(entitlementsJson), d.Code, licenseOperational))
            .GroupBy(e => e.Code, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .ToList();
    }

    public bool HasBoolean(string? entitlementsJson, string featureCode, bool licenseOperational)
        => Evaluate(entitlementsJson, featureCode, licenseOperational).Allowed;

    public double? GetNumericLimit(string? entitlementsJson, string featureCode, bool licenseOperational)
    {
        var eval = Evaluate(entitlementsJson, featureCode, licenseOperational);
        return eval.Allowed ? eval.Limit : null;
    }

    public static string SerializeLegacyCodes(IEnumerable<string> codes)
        => JsonSerializer.Serialize(codes.Distinct(StringComparer.OrdinalIgnoreCase).ToList());

    public static string SerializeDefinitions(IEnumerable<EntitlementDefinition> definitions)
    {
        var payload = definitions.Select(d => new Dictionary<string, object?>
        {
            ["code"] = d.Code,
            ["kind"] = d.Kind.ToString(),
            ["value"] = d.Value,
            ["expiresAtUtc"] = d.ExpiresAtUtc,
            ["custom"] = d.CustomPayload
        });
        return JsonSerializer.Serialize(payload);
    }

    public void InvalidateCache()
    {
        _evalCache.Clear();
        ParseCache.Clear();
    }

    public static OrganizationCommercialDisplayStatus MapOrganizationDisplay(LicenseKey? key)
    {
        if (key == null)
        {
            return OrganizationCommercialDisplayStatus.Unknown;
        }

        return key.Status switch
        {
            LicenseStatus.Suspended => OrganizationCommercialDisplayStatus.Suspended,
            LicenseStatus.Archived => OrganizationCommercialDisplayStatus.Archived,
            LicenseStatus.Expired or LicenseStatus.Revoked => OrganizationCommercialDisplayStatus.ReadOnly,
            _ when key.LicenseType == LicenseType.Trial => OrganizationCommercialDisplayStatus.Trial,
            LicenseStatus.Active or LicenseStatus.Grace or LicenseStatus.Issued
                => OrganizationCommercialDisplayStatus.Operational,
            _ => OrganizationCommercialDisplayStatus.Unknown
        };
    }

    private static EntitlementEvaluation EvaluateCore(
        IReadOnlyList<EntitlementDefinition> defs,
        string featureCode,
        bool licenseOperational)
    {
        if (!licenseOperational)
        {
            return new EntitlementEvaluation
            {
                Code = featureCode,
                Allowed = false,
                Decision = FeatureAccessDecision.Disabled,
                Message = "License is not operational."
            };
        }

        var match = defs.FirstOrDefault(d =>
            string.Equals(d.Code, featureCode, StringComparison.OrdinalIgnoreCase));
        if (match == null)
        {
            return new EntitlementEvaluation
            {
                Code = featureCode,
                Allowed = false,
                Decision = FeatureAccessDecision.Disabled,
                Message = "Entitlement not present."
            };
        }

        if (match.ExpiresAtUtc.HasValue && match.ExpiresAtUtc.Value < DateTime.UtcNow)
        {
            return new EntitlementEvaluation
            {
                Code = featureCode,
                Allowed = false,
                Kind = match.Kind,
                Decision = FeatureAccessDecision.Disabled,
                Limit = match.Value,
                Message = "Entitlement expired."
            };
        }

        return match.Kind switch
        {
            EntitlementKind.Numeric or EntitlementKind.Usage => new EntitlementEvaluation
            {
                Code = featureCode,
                Allowed = true,
                Kind = match.Kind,
                Decision = FeatureAccessDecision.Limited,
                Limit = match.Value,
                Message = match.Value.HasValue ? $"Limit {match.Value}" : "Limited"
            },
            EntitlementKind.TimeBased => new EntitlementEvaluation
            {
                Code = featureCode,
                Allowed = true,
                Kind = match.Kind,
                Decision = FeatureAccessDecision.Enabled,
                Limit = match.Value,
                Message = match.ExpiresAtUtc.HasValue
                    ? $"Valid until {match.ExpiresAtUtc:O}"
                    : "Time-based entitlement active"
            },
            _ => new EntitlementEvaluation
            {
                Code = featureCode,
                Allowed = true,
                Kind = match.Kind,
                Decision = FeatureAccessDecision.Enabled,
                Limit = match.Value
            }
        };
    }

    private static IReadOnlyList<EntitlementDefinition> ParseCore(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != JsonValueKind.Array)
            {
                return Array.Empty<EntitlementDefinition>();
            }

            var list = new List<EntitlementDefinition>();
            foreach (var el in doc.RootElement.EnumerateArray())
            {
                if (el.ValueKind == JsonValueKind.String)
                {
                    var code = el.GetString();
                    if (!string.IsNullOrWhiteSpace(code))
                    {
                        list.Add(new EntitlementDefinition
                        {
                            Code = code.Trim(),
                            Kind = EntitlementKind.Boolean,
                            Value = 1
                        });
                    }

                    continue;
                }

                if (el.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                var codeProp = GetProp(el, "code") ?? GetProp(el, "Code");
                if (codeProp is null || codeProp.Value.ValueKind != JsonValueKind.String)
                {
                    continue;
                }

                var codeVal = codeProp.Value.GetString()?.Trim();
                if (string.IsNullOrEmpty(codeVal))
                {
                    continue;
                }

                var kind = EntitlementKind.Boolean;
                var kindProp = GetProp(el, "kind") ?? GetProp(el, "Kind");
                if (kindProp is { ValueKind: JsonValueKind.String }
                    && Enum.TryParse<EntitlementKind>(kindProp.Value.GetString(), true, out var parsedKind))
                {
                    kind = parsedKind;
                }

                double? value = null;
                var valueProp = GetProp(el, "value") ?? GetProp(el, "Value");
                if (valueProp is { ValueKind: JsonValueKind.Number })
                {
                    value = valueProp.Value.GetDouble();
                }

                DateTime? expires = null;
                var expProp = GetProp(el, "expiresAtUtc") ?? GetProp(el, "ExpiresAtUtc");
                if (expProp is { ValueKind: JsonValueKind.String }
                    && DateTime.TryParse(expProp.Value.GetString(), out var expDt))
                {
                    expires = DateTime.SpecifyKind(expDt, DateTimeKind.Utc);
                }

                string? custom = null;
                var customProp = GetProp(el, "custom") ?? GetProp(el, "CustomPayload");
                if (customProp is { ValueKind: JsonValueKind.String })
                {
                    custom = customProp.Value.GetString();
                }

                list.Add(new EntitlementDefinition
                {
                    Code = codeVal,
                    Kind = kind,
                    Value = value,
                    ExpiresAtUtc = expires,
                    CustomPayload = custom
                });
            }

            return list;
        }
        catch
        {
            return Array.Empty<EntitlementDefinition>();
        }
    }

    private static JsonElement? GetProp(JsonElement el, string name)
    {
        if (el.TryGetProperty(name, out var p))
        {
            return p;
        }

        return null;
    }
}
