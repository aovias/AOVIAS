using Microsoft.Extensions.Configuration;
using OrganizationService.Application.Services;
using Xunit;

namespace OrganizationService.Tests;

public sealed class EntitlementEngineTests
{
    private static EntitlementEngine Create()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Licensing:EntitlementCacheSeconds"] = "5"
            })
            .Build();
        return new EntitlementEngine(config);
    }

    [Fact]
    public void Parses_legacy_string_array_as_boolean()
    {
        var engine = Create();
        var defs = engine.Parse("""["core.compliance","ai.write"]""");
        Assert.Equal(2, defs.Count);
        Assert.All(defs, d => Assert.Equal(EntitlementKind.Boolean, d.Kind));
        Assert.True(engine.HasBoolean("""["ai.write"]""", "ai.write", licenseOperational: true));
        Assert.False(engine.HasBoolean("""["ai.write"]""", "ai.write", licenseOperational: false));
    }

    [Fact]
    public void Evaluates_numeric_as_limited()
    {
        var engine = Create();
        var json = """[{"code":"max.hospitals","kind":"Numeric","value":5}]""";
        var eval = engine.Evaluate(json, "max.hospitals", true);
        Assert.True(eval.Allowed);
        Assert.Equal(FeatureAccessDecision.Limited, eval.Decision);
        Assert.Equal(5, eval.Limit);
    }

    [Fact]
    public void Time_based_entitlement_expires()
    {
        var engine = Create();
        var past = DateTime.UtcNow.AddDays(-1).ToString("O");
        var json = $$"""[{"code":"reports","kind":"TimeBased","expiresAtUtc":"{{past}}"}]""";
        var eval = engine.Evaluate(json, "reports", true);
        Assert.False(eval.Allowed);
        Assert.Equal(FeatureAccessDecision.Disabled, eval.Decision);
    }

    [Fact]
    public void MapOrganizationDisplay_trial_and_readonly()
    {
        var trial = new OrganizationService.Domain.Entities.LicenseKey
        {
            LicenseType = OrganizationService.Domain.Entities.LicenseType.Trial,
            Status = OrganizationService.Domain.Entities.LicenseStatus.Active
        };
        Assert.Equal(OrganizationService.Domain.Entities.OrganizationCommercialDisplayStatus.Trial,
            EntitlementEngine.MapOrganizationDisplay(trial));

        trial.Status = OrganizationService.Domain.Entities.LicenseStatus.Expired;
        Assert.Equal(OrganizationService.Domain.Entities.OrganizationCommercialDisplayStatus.ReadOnly,
            EntitlementEngine.MapOrganizationDisplay(trial));
    }
}
