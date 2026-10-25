using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Shared.Infrastructure.Execution;

namespace Aovias.Platform.IntegrationTests;

/// <summary>
/// Contract tests for <see cref="PlatformExecutionContext"/>, <see cref="ExecutionContextAccessor"/>
/// and <see cref="TenantAuthority"/>.
///
/// <para>
/// These run in-process against real <see cref="DefaultHttpContext"/> instances rather than mocks —
/// the resolution order and the fail-closed behaviour are the things under test, so substituting a
/// fake accessor would assert nothing. They need no running platform, so they are deterministic and
/// safe for CI from day one.
/// </para>
/// </summary>
public sealed class ExecutionContextTests
{
    private static readonly Guid OrgA = Guid.Parse("2759fad4-c895-4a5f-9e49-6ded5d378098");
    private static readonly Guid OrgB = Guid.Parse("b7632d36-4a68-4fa2-865f-21c4025ea3d3");

    // ── The context record ────────────────────────────────────────────────

    [Fact]
    public void A_context_without_an_organization_is_invalid()
    {
        var ctx = new PlatformExecutionContext
        {
            OrganizationId = Guid.Empty,
            Mode = ExecutionMode.User,
            CorrelationId = "c",
            RequestId = "r",
        };

        Assert.False(ctx.IsValid);
    }

    [Fact]
    public void Background_contexts_are_explicit_about_their_tenant_and_carry_no_user()
    {
        var ctx = PlatformExecutionContext.ForBackgroundJob(OrgA, "NightlyRollup");

        Assert.True(ctx.IsValid);
        Assert.Equal(OrgA, ctx.OrganizationId);
        Assert.Equal(ExecutionMode.BackgroundJob, ctx.Mode);
        Assert.Null(ctx.UserId);
        Assert.Equal("NightlyRollup", ctx.Caller);
        Assert.False(string.IsNullOrWhiteSpace(ctx.CorrelationId));
    }

    [Fact]
    public void System_contexts_are_cross_tenant_but_never_count_as_valid_tenant_scope()
    {
        var ctx = PlatformExecutionContext.ForSystem("migration");

        // The distinction matters: a call site must not treat "cross-tenant maintenance" as
        // "a tenant is established".
        Assert.True(ctx.IsCrossTenant);
        Assert.False(ctx.IsValid);
    }

    [Fact]
    public void The_context_is_immutable_for_the_request()
    {
        var ctx = PlatformExecutionContext.ForBackgroundJob(OrgA, "job");
        var mutated = ctx with { OrganizationId = OrgB };

        // `with` produces a new instance; the original is untouched. Services read, they do not reshape.
        Assert.Equal(OrgA, ctx.OrganizationId);
        Assert.Equal(OrgB, mutated.OrganizationId);
        Assert.NotSame(ctx, mutated);
    }

    // ── Resolution ────────────────────────────────────────────────────────

    [Fact]
    public void Context_resolves_from_propagated_headers_on_an_internal_hop()
    {
        var http = new DefaultHttpContext();
        http.Request.Headers[ExecutionContextHeaders.OrganizationId] = OrgA.ToString();
        http.Request.Headers[ExecutionContextHeaders.ExecutionMode] = "BackgroundJob";
        http.Request.Headers[ExecutionContextHeaders.CorrelationId] = "corr-123";

        var ctx = Accessor(http).Current;

        Assert.NotNull(ctx);
        Assert.Equal(OrgA, ctx!.OrganizationId);
        Assert.Equal(ExecutionMode.BackgroundJob, ctx.Mode);
        Assert.Equal("corr-123", ctx.CorrelationId);
    }

    [Fact]
    public void Context_resolves_from_token_claims_for_a_direct_user_request()
    {
        var http = AuthenticatedContext(OrgA);

        var ctx = Accessor(http).Current;

        Assert.NotNull(ctx);
        Assert.Equal(OrgA, ctx!.OrganizationId);
        Assert.Equal(ExecutionMode.User, ctx.Mode);
    }

    [Fact]
    public void An_anonymous_request_with_no_headers_yields_no_context()
    {
        var ctx = Accessor(new DefaultHttpContext()).Current;

        Assert.Null(ctx);
    }

    [Fact]
    public void An_unresolved_context_fails_closed_rather_than_throwing()
    {
        // Guid.Empty is what query filters compare against — matching nothing. A throw here would
        // be a worse outcome: it converts a silent over-fetch into a crash instead of into no data.
        Assert.Equal(Guid.Empty, Accessor(new DefaultHttpContext()).OrganizationId);
    }

    [Fact]
    public void Resolution_is_cached_so_it_cannot_drift_within_one_request()
    {
        var http = AuthenticatedContext(OrgA);
        var accessor = Accessor(http);

        var first = accessor.Current;
        // Simulate a later mutation attempt mid-request.
        http.Request.Headers[ExecutionContextHeaders.OrganizationId] = OrgB.ToString();
        var second = accessor.Current;

        Assert.Same(first, second);
        Assert.Equal(OrgA, second!.OrganizationId);
    }

    // ── Tenant authority ──────────────────────────────────────────────────

    [Fact]
    public void A_caller_may_act_on_its_own_organization()
    {
        var result = TenantAuthority.ResolveForTenantScopedRequest(AuthenticatedContext(OrgA), OrgA);

        Assert.True(result.IsAuthorized);
        Assert.Equal(OrgA, result.OrganizationId);
    }

    [Fact]
    public void An_omitted_organization_defaults_to_the_token_not_to_unscoped()
    {
        var result = TenantAuthority.ResolveForTenantScopedRequest(AuthenticatedContext(OrgA), Guid.Empty);

        Assert.True(result.IsAuthorized);
        Assert.Equal(OrgA, result.OrganizationId);
    }

    [Fact]
    public void A_caller_may_not_act_on_another_organization()
    {
        var result = TenantAuthority.ResolveForTenantScopedRequest(AuthenticatedContext(OrgA), OrgB);

        Assert.False(result.IsAuthorized);
        Assert.Equal(Guid.Empty, result.OrganizationId);
    }

    [Fact]
    public void A_request_with_no_organization_claim_is_denied()
    {
        var http = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(Array.Empty<Claim>(), "TestAuth")),
        };

        var result = TenantAuthority.ResolveForTenantScopedRequest(http, OrgA);

        Assert.False(result.IsAuthorized);
    }

    [Fact]
    public void A_platform_role_may_act_across_tenants()
    {
        var http = AuthenticatedContext(OrgA, "SUPER_ADMIN");

        var result = TenantAuthority.ResolveForTenantScopedRequest(http, OrgB);

        Assert.True(result.IsAuthorized);
        Assert.Equal(OrgB, result.OrganizationId);
    }

    [Fact]
    public void A_cross_tenant_endpoint_still_requires_a_cross_tenant_role()
    {
        // The endpoint being privileged does not make the caller privileged.
        var result = TenantAuthority.ResolveForCrossTenantRequest(AuthenticatedContext(OrgA), OrgB);

        Assert.False(result.IsAuthorized);
    }

    [Fact]
    public void A_customer_admin_role_is_not_a_cross_tenant_role()
    {
        // ORG_ADMIN is the top customer role and must NOT confer cross-tenant reach.
        var result = TenantAuthority.ResolveForTenantScopedRequest(
            AuthenticatedContext(OrgA, "ORG_ADMIN"), OrgB);

        Assert.False(result.IsAuthorized);
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private static ExecutionContextAccessor Accessor(HttpContext http) =>
        new(new HttpContextAccessor { HttpContext = http });

    private static DefaultHttpContext AuthenticatedContext(Guid organizationId, params string[] roles)
    {
        var claims = new List<Claim>
        {
            new("organization_id", organizationId.ToString()),
            new(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
        };
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

        return new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth")),
        };
    }
}
