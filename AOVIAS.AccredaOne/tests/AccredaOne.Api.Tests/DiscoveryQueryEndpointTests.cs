using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AccredaOne.Api.Discovery.Contracts;
using Microsoft.AspNetCore.Mvc.Testing;

namespace AccredaOne.Api.Tests;

public class DiscoveryQueryEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _client;

    public DiscoveryQueryEndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Explicit_eligible_framework_returns_single_group_and_bounded_results()
    {
        var response = await PostQueryAsync(new DiscoveryQueryRequest { Query = "synthetic topic", FrameworkCode = "NOAF" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await DeserializeAsync(response);
        Assert.Equal("bounded_results", body.Outcome);
        Assert.Equal("mvp-development-synthetic-discovery", body.DatasetPackId);
        Assert.Single(body.FrameworkGroups);
        Assert.Equal("NOAF", body.FrameworkGroups[0].FrameworkCode);
        Assert.Equal(4, body.FrameworkGroups[0].Requirements.Count);
        Assert.Equal(4, body.RequirementIds.Count);
        Assert.All(body.FrameworkGroups[0].Requirements, r => Assert.False(string.IsNullOrWhiteSpace(r.Provenance)));
        Assert.NotNull(body.FrameworkGroups[0].AccredaOneExplanation);
        Assert.Equal("source_instrument", body.FrameworkGroups[0].SourceInstrument.ContentKind);
    }

    [Fact]
    public async Task No_framework_selection_returns_cross_framework_groups()
    {
        var response = await PostQueryAsync(new DiscoveryQueryRequest { Query = "cross framework" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await DeserializeAsync(response);
        Assert.Equal("bounded_cross_framework_results", body.Outcome);
        Assert.Equal(2, body.FrameworkGroups.Count);
        Assert.Equal(["MCQF", "NOAF"], body.FrameworkGroups.Select(g => g.FrameworkCode).ToList());
        Assert.Equal(8, body.RequirementIds.Count);
        Assert.Equal(
            body.FrameworkGroups.SelectMany(g => g.Requirements.Select(r => r.Id)).ToList(),
            body.RequirementIds);
    }

    [Fact]
    public async Task Disabled_framework_returns_no_results()
    {
        var response = await PostQueryAsync(new DiscoveryQueryRequest { Query = "disabled", FrameworkCode = "LCF" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await DeserializeAsync(response);
        Assert.Equal("no_results", body.Outcome);
        Assert.Empty(body.FrameworkGroups);
        Assert.Empty(body.RequirementIds);
    }

    [Fact]
    public async Task Unknown_framework_returns_no_results()
    {
        var response = await PostQueryAsync(new DiscoveryQueryRequest { Query = "unknown", FrameworkCode = "NOT-REAL" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await DeserializeAsync(response);
        Assert.Equal("no_results", body.Outcome);
        Assert.Empty(body.FrameworkGroups);
    }

    [Fact]
    public async Task Invalid_query_returns_validation_problem()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/discovery/query", new { query = "   ", frameworkCode = (string?)null });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Unknown_api_version_returns_not_found()
    {
        var response = await _client.PostAsJsonAsync("/api/v9/discovery/query", new DiscoveryQueryRequest { Query = "test" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Response_preserves_hierarchy_context_and_provenance()
    {
        var response = await PostQueryAsync(new DiscoveryQueryRequest { Query = "hierarchy", FrameworkCode = "NOAF" });
        var body = await DeserializeAsync(response);
        var requirement = body.FrameworkGroups[0].Requirements[0];
        Assert.Equal("NOAF/section-a", requirement.HierarchyContext);
        Assert.Contains("Fictional", requirement.Provenance, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Response_includes_requirement_text_and_applicability_for_noaf()
    {
        var response = await PostQueryAsync(new DiscoveryQueryRequest { Query = "synthetic topic", FrameworkCode = "NOAF" });
        var body = await DeserializeAsync(response);
        var requirement = body.FrameworkGroups[0].Requirements[0];
        Assert.False(string.IsNullOrWhiteSpace(requirement.RequirementText));
        Assert.Contains("Synthetic source placeholder", requirement.RequirementText, StringComparison.Ordinal);
        Assert.NotNull(requirement.Applicability);
        Assert.Contains("synthetic", requirement.Applicability!.Summary, StringComparison.OrdinalIgnoreCase);
        Assert.NotEmpty(requirement.Applicability.ContextTags);
        Assert.Contains("not an official", requirement.Applicability.Disclaimer, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Response_includes_requirement_text_and_applicability_for_mcqf()
    {
        var response = await PostQueryAsync(new DiscoveryQueryRequest { Query = "synthetic topic", FrameworkCode = "MCQF" });
        var body = await DeserializeAsync(response);
        var requirement = body.FrameworkGroups[0].Requirements[0];
        Assert.False(string.IsNullOrWhiteSpace(requirement.RequirementText));
        Assert.NotNull(requirement.Applicability);
        Assert.Contains("MCQF", requirement.Applicability!.Summary, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Response_allows_missing_applicability_on_individual_requirements()
    {
        var response = await PostQueryAsync(new DiscoveryQueryRequest { Query = "synthetic topic", FrameworkCode = "NOAF" });
        var body = await DeserializeAsync(response);
        var withoutApplicability = body.FrameworkGroups[0].Requirements.Single(r => r.Id == "SYN-REQ-NOAF-004");
        Assert.Null(withoutApplicability.Applicability);
        Assert.False(string.IsNullOrWhiteSpace(withoutApplicability.RequirementText));
    }

    private Task<HttpResponseMessage> PostQueryAsync(DiscoveryQueryRequest request) =>
        _client.PostAsJsonAsync("/api/v1/discovery/query", request);

    private static async Task<DiscoveryQueryResponse> DeserializeAsync(HttpResponseMessage response)
    {
        var body = await response.Content.ReadFromJsonAsync<DiscoveryQueryResponse>(JsonOptions);
        Assert.NotNull(body);
        return body;
    }
}
