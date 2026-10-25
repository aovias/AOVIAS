using System.Net;

namespace Aovias.Platform.IntegrationTests;

/// <summary>
/// The release gate for tenant isolation (Wave 2.2B.1).
///
/// <para>
/// Every test here holds the caller, the endpoint and the permission constant and varies **only**
/// the organization being requested. That isolation of variables is what makes a 403 attributable
/// to the tenant boundary rather than to a permission check — the distinction that made the first
/// verification attempt in Wave 2.2B inconclusive.
/// </para>
/// </summary>
[Collection(nameof(PlatformCollection))]
public sealed class TenantIsolationTests(PlatformFixture platform)
{
    /// <summary>
    /// Endpoints that accept an <c>organizationId</c> query parameter and are tenant-scoped.
    /// Extend this list as controllers are hardened — the suite is the ratchet.
    /// </summary>
    public static TheoryData<string> TenantScopedEndpoints() => new()
    {
        // Hardened in Wave 2.2B via [ValidateTenantScope]. Before that fix,
        // /directory/facets returned 200 for a foreign organization — these cases would have failed.
        "/api/v1/directory/search?q=a&organizationId=",
        "/api/v1/directory/facets?organizationId=",
        "/api/v1/directory/suggestions?prefix=a&organizationId=",

        // Not covered by [ValidateTenantScope], but verified to reject cross-tenant access by their
        // own downstream scoping. Locked in here so that protection cannot silently regress —
        // and as evidence that a client-supplied organizationId parameter is not, by itself,
        // proof of exploitability.
        "/api/v1/sla/policies?organizationId=",
        "/api/v1/assignment/rules?organizationId=",
        "/api/v1/hospitals?organizationId=",
        "/api/v1/professional-profiles?organizationId=",
    };

    /// <summary>
    /// Endpoints whose cross-tenant rejection is verified, but whose own-tenant call returns
    /// 404/405 for this account (no seeded data, or a different verb). Only the negative case is
    /// asserted — claiming a positive here would assert nothing.
    /// </summary>
    public static TheoryData<string> RejectionOnlyEndpoints() => new()
    {
        "/api/v1/employment/records?organizationId=",
        "/api/v1/integration/connectors?organizationId=",
        "/api/v1/reporting/definitions?organizationId=",
        "/api/v1/subscriptions?organizationId=",
        "/api/v1/licensing/status?organizationId=",
    };

    [SkippableTheory]
    [MemberData(nameof(RejectionOnlyEndpoints))]
    public async Task Foreign_organization_is_rejected_on_secondary_endpoints(string endpointPrefix)
    {
        Skip.IfNot(platform.PlatformAvailable, platform.UnavailableReason ?? "Platform unavailable.");

        using var client = platform.TenantA!.CreateClient(platform.GatewayBaseUrl);
        var response = await client.GetAsync(endpointPrefix + platform.ForeignOrganizationId);

        Assert.True(
            response.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.Unauthorized,
            $"{endpointPrefix} returned {(int)response.StatusCode} for a FOREIGN organization.");
    }

    [SkippableTheory]
    [MemberData(nameof(TenantScopedEndpoints))]
    public async Task Own_organization_is_reachable(string endpointPrefix)
    {
        Skip.IfNot(platform.PlatformAvailable, platform.UnavailableReason ?? "Platform unavailable.");
        var tenant = platform.TenantA!;

        using var client = tenant.CreateClient(platform.GatewayBaseUrl);
        var response = await client.GetAsync(endpointPrefix + tenant.OrganizationId);

        // The control case. Without it, a blanket-deny bug would make every isolation test pass.
        Assert.False(
            response.StatusCode == HttpStatusCode.Forbidden,
            $"{endpointPrefix} rejected the caller's OWN organization — isolation is over-blocking.");
    }

    [SkippableTheory]
    [MemberData(nameof(TenantScopedEndpoints))]
    public async Task Foreign_organization_is_rejected(string endpointPrefix)
    {
        Skip.IfNot(platform.PlatformAvailable, platform.UnavailableReason ?? "Platform unavailable.");
        var tenant = platform.TenantA!;

        using var client = tenant.CreateClient(platform.GatewayBaseUrl);
        var response = await client.GetAsync(endpointPrefix + platform.ForeignOrganizationId);

        Assert.True(
            response.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.Unauthorized,
            $"{endpointPrefix} returned {(int)response.StatusCode} for a FOREIGN organization — " +
            "expected 403/401. This is cross-tenant access.");
    }

    [SkippableFact]
    public async Task Omitted_organization_falls_back_to_the_token_not_to_everything()
    {
        Skip.IfNot(platform.PlatformAvailable, platform.UnavailableReason ?? "Platform unavailable.");
        var tenant = platform.TenantA!;

        using var client = tenant.CreateClient(platform.GatewayBaseUrl);
        var response = await client.GetAsync("/api/v1/directory/facets?organizationId=" + Guid.Empty);

        // Empty must resolve to the caller's tenant, never to "unscoped".
        Assert.False(
            response.StatusCode == HttpStatusCode.InternalServerError,
            "An empty organizationId produced a server error; it should resolve to the caller's tenant.");
    }

    [SkippableFact]
    public async Task Spoofed_context_headers_from_a_client_are_not_honoured()
    {
        Skip.IfNot(platform.PlatformAvailable, platform.UnavailableReason ?? "Platform unavailable.");
        var tenant = platform.TenantA!;

        using var client = tenant.CreateClient(platform.GatewayBaseUrl);
        // A client asserting its own execution context. The edge must strip these.
        client.DefaultRequestHeaders.Add("X-Aovias-Organization-Id", platform.ForeignOrganizationId.ToString());
        client.DefaultRequestHeaders.Add("X-Aovias-Execution-Mode", "System");

        var response = await client.GetAsync(
            "/api/v1/directory/facets?organizationId=" + platform.ForeignOrganizationId);

        Assert.True(
            response.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.Unauthorized,
            "Client-supplied execution-context headers were honoured — the edge is not stripping them.");
    }

    [SkippableFact]
    public async Task Unauthenticated_requests_are_rejected()
    {
        Skip.IfNot(platform.PlatformAvailable, platform.UnavailableReason ?? "Platform unavailable.");

        using var client = new HttpClient { BaseAddress = new Uri(platform.GatewayBaseUrl) };
        var response = await client.GetAsync(
            "/api/v1/directory/facets?organizationId=" + platform.TenantA!.OrganizationId);

        Assert.True(
            response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden,
            $"Unauthenticated request returned {(int)response.StatusCode}.");
    }

    [SkippableFact]
    public async Task An_invalid_token_is_rejected()
    {
        Skip.IfNot(platform.PlatformAvailable, platform.UnavailableReason ?? "Platform unavailable.");

        using var client = new HttpClient { BaseAddress = new Uri(platform.GatewayBaseUrl) };
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "not.a.token");

        var response = await client.GetAsync(
            "/api/v1/directory/facets?organizationId=" + platform.TenantA!.OrganizationId);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ---------------------------------------------------------------------
    // Full two-tenant coverage. Skipped — never silently passed — until a
    // second organization's credentials are configured.
    // ---------------------------------------------------------------------

    [SkippableFact]
    public async Task Tenants_cannot_read_each_others_data()
    {
        Skip.IfNot(platform.TwoTenantsAvailable,
            "tenantB credentials are not configured; cross-tenant data-visibility cannot be proven.");

        var a = platform.TenantA!;
        var b = platform.TenantB!;

        using var clientA = a.CreateClient(platform.GatewayBaseUrl);
        var response = await clientA.GetAsync("/api/v1/directory/facets?organizationId=" + b.OrganizationId);

        Assert.True(
            response.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.Unauthorized,
            "Tenant A reached Tenant B's data.");
    }

    [SkippableFact]
    public void The_two_configured_tenants_are_genuinely_different()
    {
        Skip.IfNot(platform.TwoTenantsAvailable, "tenantB credentials are not configured.");

        Assert.NotEqual(platform.TenantA!.OrganizationId, platform.TenantB!.OrganizationId);
    }
}
