using ComplianceService.Application.Authorization;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ComplianceService.Tests.Authorization;

public sealed class HospitalAccessAuthorizerTests
{
    private static readonly Guid OrgA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid OrgB = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid HospitalA = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid HospitalB = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid HospitalC = Guid.Parse("33333333-3333-3333-3333-333333333333");

    [Fact]
    public async Task Own_hospital_is_allowed()
    {
        var sut = CreateSut(Found(HospitalA, OrgA));
        var result = await sut.AuthorizeAsync(HospitalA, User(OrgA, HospitalA));
        Assert.True(result.IsAllowed);
        Assert.Equal(OrgA, result.OrganizationId);
        Assert.NotEqual(HospitalA, result.OrganizationId);
    }

    [Fact]
    public async Task Other_hospital_same_organization_is_denied()
    {
        var sut = CreateSut(Found(HospitalB, OrgA));
        var result = await sut.AuthorizeAsync(HospitalB, User(OrgA, HospitalA));
        Assert.False(result.IsAllowed);
        Assert.Equal(HospitalAccessStatus.HospitalMismatch, result.Status);
    }

    [Fact]
    public async Task Other_organization_hospital_is_denied()
    {
        var sut = CreateSut(Found(HospitalC, OrgB));
        var result = await sut.AuthorizeAsync(HospitalC, User(OrgA, HospitalA));
        Assert.Equal(HospitalAccessStatus.OrganizationMismatch, result.Status);
    }

    [Fact]
    public async Task Random_guid_is_not_found_not_unavailable()
    {
        var sut = CreateSut(HospitalLookupResult.NotFound());
        var result = await sut.AuthorizeAsync(Guid.NewGuid(), User(OrgA, HospitalA));
        Assert.Equal(Shared.Infrastructure.Api.ApiResponseCode.RESOURCE_NOT_FOUND, result.ToApiResponseCode());
    }

    [Fact]
    public async Task Empty_hospital_id_is_required()
    {
        var sut = CreateSut(HospitalLookupResult.NotFound());
        var result = await sut.AuthorizeAsync(Guid.Empty, User(OrgA, HospitalA));
        Assert.Equal(HospitalAccessStatus.HospitalIdRequired, result.Status);
    }

    [Fact]
    public async Task Organization_unavailable_is_not_treated_as_missing_hospital()
    {
        var sut = CreateSut(HospitalLookupResult.Unavailable());
        var result = await sut.AuthorizeAsync(HospitalA, User(OrgA, HospitalA));
        Assert.Equal(HospitalAccessStatus.OrganizationServiceUnavailable, result.Status);
        Assert.Equal(
            Shared.Infrastructure.Api.ApiResponseCode.SERVICE_UNAVAILABLE,
            result.ToApiResponseCode());
    }

    [Fact]
    public async Task Deleted_hospital_is_rejected()
    {
        var sut = CreateSut(HospitalLookupResult.Found(new OrganizationHospitalRecord(
            HospitalA, OrgA, "Active", IsDeleted: true, IsOnboarded: true)));
        var result = await sut.AuthorizeAsync(HospitalA, User(OrgA, HospitalA));
        Assert.Equal(HospitalAccessStatus.HospitalInactive, result.Status);
    }

    [Fact]
    public async Task Super_admin_may_use_another_hospital_when_it_exists()
    {
        var sut = CreateSut(Found(HospitalC, OrgB));
        var result = await sut.AuthorizeAsync(
            HospitalC,
            new HospitalAccessCaller(HospitalAccessCallerKind.Privileged, OrgA, HospitalA, ["SUPER_ADMIN"]));
        Assert.True(result.IsAllowed);
        Assert.Equal(OrgB, result.OrganizationId);
    }

    [Fact]
    public async Task Internal_service_may_use_any_hospital_in_execution_organization()
    {
        var sut = CreateSut(Found(HospitalB, OrgA));
        var result = await sut.AuthorizeAsync(
            HospitalB,
            new HospitalAccessCaller(HospitalAccessCallerKind.InternalService, OrgA, null, []));
        Assert.True(result.IsAllowed);
    }

    [Fact]
    public async Task Internal_service_without_org_header_is_allowed_when_hospital_exists()
    {
        var sut = CreateSut(Found(HospitalA, OrgA));
        var result = await sut.AuthorizeAsync(
            HospitalA,
            new HospitalAccessCaller(HospitalAccessCallerKind.InternalService, null, null, []));
        Assert.True(result.IsAllowed);
        Assert.Equal(OrgA, result.OrganizationId);
    }

    [Fact]
    public async Task Internal_service_cannot_use_hospital_from_another_organization()
    {
        var sut = CreateSut(Found(HospitalC, OrgB));
        var result = await sut.AuthorizeAsync(
            HospitalC,
            new HospitalAccessCaller(HospitalAccessCallerKind.InternalService, OrgA, null, []));
        Assert.Equal(HospitalAccessStatus.OrganizationMismatch, result.Status);
    }

    private static HospitalLookupResult Found(Guid hospitalId, Guid organizationId)
        => HospitalLookupResult.Found(new OrganizationHospitalRecord(
            hospitalId, organizationId, "Active", IsDeleted: false, IsOnboarded: true));

    private static HospitalAccessCaller User(Guid org, Guid hospital)
        => new(HospitalAccessCallerKind.User, org, hospital, ["ORG_ADMIN"]);

    private static HospitalAccessAuthorizer CreateSut(HospitalLookupResult lookup)
        => new(new StubDirectory(lookup), NullLogger<HospitalAccessAuthorizer>.Instance);

    private sealed class StubDirectory : IHospitalDirectory
    {
        private readonly HospitalLookupResult _result;
        public StubDirectory(HospitalLookupResult result) => _result = result;
        public Task<HospitalLookupResult> GetHospitalAsync(Guid hospitalId, CancellationToken cancellationToken = default)
            => Task.FromResult(_result);
    }
}
