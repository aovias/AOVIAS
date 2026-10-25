using ComplianceService.Infrastructure.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Xunit;

namespace ComplianceService.Tests.Authorization;

public sealed class HospitalIdLocatorTests
{
    [Fact]
    public void Reads_route_hospitalId()
    {
        var http = new DefaultHttpContext();
        var route = new RouteData();
        route.Values["hospitalId"] = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var action = new ActionContext(http, route, new ActionDescriptor());
        var ctx = new ActionExecutingContext(action, new List<IFilterMetadata>(), new Dictionary<string, object?>(), controller: new object());

        Assert.True(HospitalIdLocator.TryGet(ctx, out var id));
        Assert.Equal(Guid.Parse("11111111-1111-1111-1111-111111111111"), id);
    }

    [Fact]
    public void Reads_query_hospitalId()
    {
        var http = new DefaultHttpContext();
        http.Request.QueryString = new QueryString("?hospitalId=22222222-2222-2222-2222-222222222222");
        var action = new ActionContext(http, new RouteData(), new ActionDescriptor());
        var ctx = new ActionExecutingContext(action, new List<IFilterMetadata>(), new Dictionary<string, object?>(), controller: new object());

        Assert.True(HospitalIdLocator.TryGet(ctx, out var id));
        Assert.Equal(Guid.Parse("22222222-2222-2222-2222-222222222222"), id);
    }

    [Fact]
    public void Skips_when_no_hospital_parameter()
    {
        var http = new DefaultHttpContext();
        var action = new ActionContext(http, new RouteData(), new ActionDescriptor());
        var ctx = new ActionExecutingContext(action, new List<IFilterMetadata>(), new Dictionary<string, object?>(), controller: new object());

        Assert.False(HospitalIdLocator.TryGet(ctx, out _));
    }
}
