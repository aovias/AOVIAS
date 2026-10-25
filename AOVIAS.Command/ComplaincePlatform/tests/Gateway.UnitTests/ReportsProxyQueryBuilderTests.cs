using Gateway.Application.Handlers;
using Xunit;

namespace Gateway.UnitTests;

public class ReportsProxyQueryBuilderTests
{
    [Fact]
    public void BuildEndpoint_IncludesHospitalAndFramework()
    {
        var hospitalId = Guid.NewGuid();
        var frameworkId = Guid.NewGuid();

        var endpoint = ReportsProxyQueryBuilder.BuildEndpoint(
            "1.0",
            "compliance",
            hospitalId,
            frameworkId,
            null,
            null,
            null,
            null,
            null,
            null);

        Assert.Contains($"/api/v1.0/reports/compliance", endpoint);
        Assert.Contains($"hospitalId={hospitalId}", endpoint);
        Assert.Contains($"frameworkId={frameworkId}", endpoint);
    }
}
