using Gateway.Security;
using Xunit;

namespace Gateway.UnitTests;

public class TenantIsolationEvaluatorTests
{
    private static readonly Guid OrgA = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid OrgB = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid HospA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid HospB = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    [Fact]
    public void Matching_org_and_hospital_ids_pass()
    {
        var conflict = TenantIsolationEvaluator.FindConflict(
            OrgA,
            HospA,
            [
                new TenantIsolationEvaluator.Candidate("query", "organizationId", OrgA.ToString()),
                new TenantIsolationEvaluator.Candidate("header", "X-Hospital-Id", HospA.ToString()),
                new TenantIsolationEvaluator.Candidate("route", "tenantId", OrgA.ToString())
            ]);

        Assert.Null(conflict);
    }

    [Fact]
    public void Conflicting_organization_id_in_query_is_detected()
    {
        var conflict = TenantIsolationEvaluator.FindConflict(
            OrgA,
            HospA,
            [new TenantIsolationEvaluator.Candidate("query", "organization_id", OrgB.ToString())]);

        Assert.NotNull(conflict);
        Assert.Equal("organization_id", conflict!.ClaimType);
        Assert.Equal(OrgB.ToString(), conflict.RequestedValue);
    }

    [Fact]
    public void Conflicting_hospital_id_in_header_is_detected()
    {
        var conflict = TenantIsolationEvaluator.FindConflict(
            OrgA,
            HospA,
            [new TenantIsolationEvaluator.Candidate("header", "X-Hospital-Id", HospB.ToString())]);

        Assert.NotNull(conflict);
        Assert.Equal("hospital_id", conflict!.ClaimType);
    }

    [Fact]
    public void Conflicting_tenantId_route_is_treated_as_organization()
    {
        var conflict = TenantIsolationEvaluator.FindConflict(
            OrgA,
            null,
            [new TenantIsolationEvaluator.Candidate("route", "tenantId", OrgB.ToString())]);

        Assert.NotNull(conflict);
        Assert.Equal("organization_id", conflict!.ClaimType);
    }

    [Fact]
    public void Missing_jwt_claims_skip_enforcement()
    {
        var conflict = TenantIsolationEvaluator.FindConflict(
            null,
            null,
            [new TenantIsolationEvaluator.Candidate("query", "organizationId", OrgB.ToString())]);

        Assert.Null(conflict);
    }

    [Fact]
    public void Empty_or_invalid_candidate_values_are_ignored()
    {
        var conflict = TenantIsolationEvaluator.FindConflict(
            OrgA,
            HospA,
            [
                new TenantIsolationEvaluator.Candidate("query", "organizationId", ""),
                new TenantIsolationEvaluator.Candidate("query", "hospitalId", "not-a-guid")
            ]);

        Assert.Null(conflict);
    }
}
