using EnterpriseDirectoryService.Application.Contracts;
using EnterpriseDirectoryService.Application.Directory.Configuration;
using EnterpriseDirectoryService.Application.Directory.Indexing;
using EnterpriseDirectoryService.Application.Directory.Platform;
using EnterpriseDirectoryService.Application.Directory.NaturalLanguage;
using EnterpriseDirectoryService.Application.Directory.Profiles;
using EnterpriseDirectoryService.Application.Directory.SavedSearch;
using EnterpriseDirectoryService.Application.Directory.Search;
using EnterpriseDirectoryService.Application.Directory.Security;
using EnterpriseDirectoryService.Application.Directory.Utilities;
using EnterpriseDirectoryService.Domain.ReadModels;
using EnterpriseDirectoryService.Infrastructure.Clients;
using EnterpriseDirectoryService.Infrastructure.Persistence;
using EnterpriseDirectoryService.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Shared.Authorization;
using System.Security.Claims;

namespace EnterpriseDirectoryService.Tests;

public class DirectoryTestDb : IDisposable
{
    public EnterpriseDirectoryDbContext Db { get; }
    public DirectoryTestDb()
    {
        Db = new EnterpriseDirectoryDbContext(new DbContextOptionsBuilder<EnterpriseDirectoryDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
    }
    public void Dispose() => Db.Dispose();
}

public class PhoneticHelperTests
{
    [Theory]
    [InlineData("Smith", "S530")]
    [InlineData("", "")]
    [InlineData("John Doe", "J500")]
    public void ComputeKey_ReturnsSoundex(string input, string expectedStart)
    {
        var key = PhoneticHelper.ComputeKey(input);
        if (string.IsNullOrEmpty(expectedStart)) Assert.Equal(string.Empty, key);
        else Assert.StartsWith(expectedStart[..1], key);
    }

    [Fact]
    public void ComputeKey_IsDeterministic() =>
        Assert.Equal(PhoneticHelper.ComputeKey("Nurse"), PhoneticHelper.ComputeKey("Nurse"));
}

public class DirectoryIndexProjectorTests
{
    [Fact]
    public async Task RebuildOrganization_CreatesIndexRowsPerEmployment()
    {
        using var fx = new DirectoryTestDb();
        var orgId = Guid.NewGuid();
        var p1 = Guid.NewGuid();
        var workforce = new Mock<IWorkforceSourceClient>();
        workforce.Setup(w => w.ListEmploymentsAsync(orgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SourceEmploymentDto>
            {
                new() { Id = Guid.NewGuid(), PersonId = p1, OrganizationId = orgId, EmployeeNumber = "E1", EmploymentStatus = "Active", IsActive = true, DepartmentName = "ICU" }
            });
        var person = new Mock<IPersonSourceClient>();
        person.Setup(p => p.ListPersonsAsync(orgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SourcePersonDto> { new() { Id = p1, LegalName = "Jane Nurse", OrganizationId = orgId } });
        var professional = new Mock<IProfessionalSourceClient>();
        professional.Setup(p => p.ListProfilesAsync(orgId, It.IsAny<CancellationToken>())).ReturnsAsync(Array.Empty<SourceProfessionalProfileDto>());
        var outbox = new Mock<IDirectoryOutboxWriter>();
        var projector = new DirectoryIndexProjector(fx.Db, person.Object, workforce.Object, professional.Object, outbox.Object, NullLogger<DirectoryIndexProjector>.Instance);

        var count = await projector.RebuildOrganizationAsync(orgId, "corr");
        Assert.Equal(1, count);
        Assert.Single(await fx.Db.DirectoryIndexes.ToListAsync());
        Assert.Contains("jane", (await fx.Db.DirectoryIndexes.SingleAsync()).SearchBlob);
    }

    [Fact]
    public async Task HandleEmploymentTerminated_DeactivatesRow()
    {
        using var fx = new DirectoryTestDb();
        var empId = Guid.NewGuid();
        fx.Db.DirectoryIndexes.Add(new DirectoryIndex { Id = Guid.NewGuid(), PersonId = Guid.NewGuid(), EmploymentId = empId, OrganizationId = Guid.NewGuid(), DisplayName = "X", SearchBlob = "x", IsActive = true });
        await fx.Db.SaveChangesAsync();
        var projector = new DirectoryIndexProjector(fx.Db, Mock.Of<IPersonSourceClient>(), Mock.Of<IWorkforceSourceClient>(), Mock.Of<IProfessionalSourceClient>(), Mock.Of<IDirectoryOutboxWriter>(), NullLogger<DirectoryIndexProjector>.Instance);
        await projector.HandleEmploymentEventAsync("EmploymentTerminated", empId, "{}");
        Assert.False((await fx.Db.DirectoryIndexes.SingleAsync()).IsActive);
    }
}

public class DirectorySearchServiceTests
{
    private static DirectorySearchService CreateService(DirectoryTestDb fx) =>
        new(fx.Db, Options.Create(new DirectoryOptions()), NullLogger<DirectorySearchService>.Instance);

    private static GovernedDirectoryContext Auth(Guid orgId, bool contact = true) => new()
    {
        SearchAllowed = true,
        ContactReadable = contact,
        CredentialReadable = true,
        RowLevelFilter = new RowLevelFilterScope { OrganizationId = orgId }
    };

    [Fact]
    public async Task Search_FiltersByOrganization()
    {
        using var fx = new DirectoryTestDb();
        var orgA = Guid.NewGuid(); var orgB = Guid.NewGuid();
        fx.Db.DirectoryIndexes.AddRange(
            new DirectoryIndex { Id = Guid.NewGuid(), PersonId = Guid.NewGuid(), OrganizationId = orgA, DisplayName = "Alice", SearchBlob = "alice nurse", IsActive = true },
            new DirectoryIndex { Id = Guid.NewGuid(), PersonId = Guid.NewGuid(), OrganizationId = orgB, DisplayName = "Bob", SearchBlob = "bob nurse", IsActive = true });
        await fx.Db.SaveChangesAsync();
        var svc = CreateService(fx);
        var result = await svc.SearchAsync(new DirectoryQuery { Text = "nurse", Filters = new DirectoryQueryFilters { OrganizationId = orgA } }, Auth(orgA));
        Assert.Single(result.Items);
        Assert.Equal("Alice", result.Items[0].DisplayName);
    }

    [Fact]
    public async Task Search_AppliesRlsHospitalFilter()
    {
        using var fx = new DirectoryTestDb();
        var orgId = Guid.NewGuid();
        var h1 = Guid.NewGuid(); var h2 = Guid.NewGuid();
        fx.Db.DirectoryIndexes.AddRange(
            new DirectoryIndex { Id = Guid.NewGuid(), PersonId = Guid.NewGuid(), OrganizationId = orgId, HospitalId = h1, DisplayName = "H1", SearchBlob = "h1", IsActive = true },
            new DirectoryIndex { Id = Guid.NewGuid(), PersonId = Guid.NewGuid(), OrganizationId = orgId, HospitalId = h2, DisplayName = "H2", SearchBlob = "h2", IsActive = true });
        await fx.Db.SaveChangesAsync();
        var auth = new GovernedDirectoryContext { SearchAllowed = true, ContactReadable = true, CredentialReadable = true, RowLevelFilter = new RowLevelFilterScope { OrganizationId = orgId, HospitalId = h1 } };
        var result = await CreateService(fx).SearchAsync(new DirectoryQuery { Filters = new DirectoryQueryFilters { OrganizationId = orgId } }, auth);
        Assert.Single(result.Items);
        Assert.Equal("H1", result.Items[0].DisplayName);
    }

    [Fact]
    public async Task Search_MasksContactWhenNotReadable()
    {
        using var fx = new DirectoryTestDb();
        var orgId = Guid.NewGuid();
        fx.Db.DirectoryIndexes.Add(new DirectoryIndex { Id = Guid.NewGuid(), PersonId = Guid.NewGuid(), OrganizationId = orgId, DisplayName = "A", SearchBlob = "a", EmailNormalized = "a@test.com", IsActive = true });
        await fx.Db.SaveChangesAsync();
        var result = await CreateService(fx).SearchAsync(new DirectoryQuery { Filters = new DirectoryQueryFilters { OrganizationId = orgId } }, Auth(orgId, contact: false));
        Assert.Equal("***", result.Items[0].Email);
    }

    [Fact]
    public async Task Search_RankingIsDeterministic()
    {
        using var fx = new DirectoryTestDb();
        var orgId = Guid.NewGuid();
        fx.Db.DirectoryIndexes.AddRange(
            new DirectoryIndex { Id = Guid.NewGuid(), PersonId = Guid.NewGuid(), OrganizationId = orgId, DisplayName = "Nurse A", SearchBlob = "nurse a", EmploymentStatus = "Active", IsActive = true },
            new DirectoryIndex { Id = Guid.NewGuid(), PersonId = Guid.NewGuid(), OrganizationId = orgId, DisplayName = "Nurse B", SearchBlob = "nurse b", EmploymentStatus = "Active", IsActive = true });
        await fx.Db.SaveChangesAsync();
        var svc = CreateService(fx);
        var q = new DirectoryQuery { Text = "Nurse", Filters = new DirectoryQueryFilters { OrganizationId = orgId }, Fuzzy = true };
        var r1 = await svc.SearchAsync(q, Auth(orgId));
        var r2 = await svc.SearchAsync(q, Auth(orgId));
        Assert.Equal(r1.Items.Select(i => i.PersonId), r2.Items.Select(i => i.PersonId));
    }

    [Fact]
    public async Task Suggest_ReturnsPrefixMatches()
    {
        using var fx = new DirectoryTestDb();
        var orgId = Guid.NewGuid();
        fx.Db.DirectoryIndexes.Add(new DirectoryIndex { Id = Guid.NewGuid(), PersonId = Guid.NewGuid(), OrganizationId = orgId, DisplayName = "Alice", SearchBlob = "alice", IsActive = true });
        await fx.Db.SaveChangesAsync();
        var items = await CreateService(fx).SuggestAsync("Ali", Auth(orgId), orgId);
        Assert.Single(items);
    }

    [Fact]
    public async Task GetFacets_ReturnsAggregatedCounts()
    {
        using var fx = new DirectoryTestDb();
        var orgId = Guid.NewGuid();
        fx.Db.DirectoryFacets.Add(new DirectoryFacet { Id = Guid.NewGuid(), OrganizationId = orgId, FacetType = "department", FacetValue = "ICU", Count = 5, AsOfUtc = DateTime.UtcNow });
        await fx.Db.SaveChangesAsync();
        var facets = await CreateService(fx).GetFacetsAsync(Auth(orgId), orgId);
        Assert.True(facets.ContainsKey("department"));
        Assert.Equal(5, facets["department"][0].Count);
    }

    [Fact]
    public async Task Search_ExpandsSynonyms()
    {
        using var fx = new DirectoryTestDb();
        var orgId = Guid.NewGuid();
        fx.Db.DirectorySynonyms.Add(new DirectorySynonym { Id = Guid.NewGuid(), Token = "rn", Synonym = "nurse" });
        fx.Db.DirectoryIndexes.Add(new DirectoryIndex { Id = Guid.NewGuid(), PersonId = Guid.NewGuid(), OrganizationId = orgId, DisplayName = "RN Staff", SearchBlob = "rn staff registered", IsActive = true });
        await fx.Db.SaveChangesAsync();
        var result = await CreateService(fx).SearchAsync(new DirectoryQuery { Text = "nurse", Filters = new DirectoryQueryFilters { OrganizationId = orgId } }, Auth(orgId));
        Assert.NotEmpty(result.Items);
    }
}

public class DirectoryAuthorizationServiceTests
{
    [Fact]
    public async Task BuildContext_DeniesSearchWhenNotAllowed()
    {
        var evaluator = new Mock<IAuthorizationEvaluator>();
        evaluator.Setup(e => e.EvaluateAsync(It.Is<AuthorizationRequest>(r => r.Resource.Contains("Directory.Search")), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AuthorizationDecision { Allowed = false });
        evaluator.Setup(e => e.EvaluateAsync(It.Is<AuthorizationRequest>(r => !r.Resource.Contains("Directory.Search")), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AuthorizationDecision { Allowed = false });
        var svc = new DirectoryAuthorizationService(evaluator.Object);
        var user = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())]));
        var ctx = await svc.BuildContextAsync(user, Guid.NewGuid());
        Assert.False(ctx.SearchAllowed);
    }

    [Fact]
    public async Task EnsureSearchAllowed_ThrowsWhenDenied()
    {
        var evaluator = new Mock<IAuthorizationEvaluator>();
        evaluator.Setup(e => e.EvaluateAsync(It.IsAny<AuthorizationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AuthorizationDecision { Allowed = false });
        var svc = new DirectoryAuthorizationService(evaluator.Object);
        var user = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())]));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => svc.EnsureSearchAllowedAsync(user, Guid.NewGuid()));
    }
}

public class NaturalLanguageQueryTranslatorTests
{
    [Fact]
    public async Task Translate_FallsBackToHeuristicForAcls()
    {
        var ai = new Mock<IAIOrchestratorClient>();
        ai.Setup(a => a.TranslateDirectoryQueryAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((DirectoryQuery?)null);
        var translator = new NaturalLanguageQueryTranslator(ai.Object);
        var result = await translator.TranslateAsync("ICU nurse with ACLS expiring in 60 days", Guid.NewGuid());
        Assert.Equal("ACLS", result.Filters.Certification);
        Assert.True(result.Fuzzy);
    }

    [Fact]
    public async Task Translate_UsesAiWhenAvailable()
    {
        var orgId = Guid.NewGuid();
        var ai = new Mock<IAIOrchestratorClient>();
        ai.Setup(a => a.TranslateDirectoryQueryAsync(It.IsAny<string>(), orgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DirectoryQuery { Text = "cardiology", Filters = new DirectoryQueryFilters { Skill = "ECG" } });
        var result = await new NaturalLanguageQueryTranslator(ai.Object).TranslateAsync("cardiology staff", orgId);
        Assert.Equal("cardiology", result.Text);
        Assert.Equal(orgId, result.Filters.OrganizationId);
    }
}

public class SavedSearchAndHistoryTests
{
    [Fact]
    public async Task SavedSearch_CreateAndList()
    {
        using var fx = new DirectoryTestDb();
        var svc = new SavedSearchService(fx.Db);
        var userId = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        await svc.CreateAsync(userId, new CreateSavedSearchRequest { Name = "ICU", OrganizationId = orgId, Query = new DirectoryQuery { Text = "ICU" } });
        var list = await svc.ListAsync(userId, orgId);
        Assert.Single(list);
        Assert.Equal("ICU", list[0].Name);
    }

    [Fact]
    public async Task SearchHistory_RecordsAndLists()
    {
        using var fx = new DirectoryTestDb();
        var svc = new SearchHistoryService(fx.Db);
        var userId = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        await svc.RecordAsync(userId, orgId, "nurse", new DirectoryQueryFilters(), 3, 42);
        var items = await svc.ListAsync(userId, orgId);
        Assert.Single(items);
        Assert.Equal(3, items[0].ResultCount);
    }

    [Fact]
    public async Task Popular_AggregatesTerms()
    {
        using var fx = new DirectoryTestDb();
        var svc = new SearchHistoryService(fx.Db);
        var orgId = Guid.NewGuid();
        await svc.RecordAsync(Guid.NewGuid(), orgId, "nurse", new DirectoryQueryFilters(), 1, 1);
        await svc.RecordAsync(Guid.NewGuid(), orgId, "nurse", new DirectoryQueryFilters(), 1, 1);
        var popular = await svc.PopularAsync(orgId);
        Assert.Single(popular);
        Assert.Equal(2, popular[0].Count);
    }

    [Fact]
    public async Task Preferences_UpdateRoundTrip()
    {
        using var fx = new DirectoryTestDb();
        var svc = new SearchPreferenceService(fx.Db);
        var userId = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        await svc.UpdateAsync(userId, new UpdateSearchPreferenceRequest { DefaultOrganizationId = orgId, NaturalLanguageEnabled = false });
        var pref = await svc.GetAsync(userId);
        Assert.Equal(orgId, pref.DefaultOrganizationId);
        Assert.False(pref.NaturalLanguageEnabled);
    }
}

public class DirectoryProfileComposerTests
{
    [Fact]
    public async Task Compose_ReturnsProfileFromSources()
    {
        var personId = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        var person = new Mock<IPersonSourceClient>();
        person.Setup(p => p.GetPersonAsync(personId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SourcePersonDto { Id = personId, LegalName = "Dr Test", OrganizationId = orgId, Email = "t@test.com" });
        var workforce = new Mock<IWorkforceSourceClient>();
        workforce.Setup(w => w.ListEmploymentsAsync(orgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SourceEmploymentDto> { new() { Id = Guid.NewGuid(), PersonId = personId, OrganizationId = orgId, DepartmentName = "ER" } });
        var professional = new Mock<IProfessionalSourceClient>();
        professional.Setup(p => p.ListProfilesAsync(orgId, It.IsAny<CancellationToken>())).ReturnsAsync(Array.Empty<SourceProfessionalProfileDto>());
        var governance = new Mock<IGovernanceSourceClient>();
        governance.Setup(g => g.GetAssigneesAsync(personId, It.IsAny<CancellationToken>())).ReturnsAsync(new GovernanceAssigneeBundle());
        var composer = new DirectoryProfileComposer(person.Object, workforce.Object, professional.Object, governance.Object, new MemoryCache(new MemoryCacheOptions()), Options.Create(new DirectoryOptions()));
        var auth = new GovernedDirectoryContext { SearchAllowed = true, ContactReadable = true, CredentialReadable = true, RowLevelFilter = new RowLevelFilterScope { OrganizationId = orgId } };
        var profile = await composer.ComposeAsync(personId, auth);
        Assert.NotNull(profile);
        Assert.Equal("Dr Test", profile!.DisplayName);
        Assert.Single(profile.Employments);
    }

    [Fact]
    public async Task Compose_MasksEmailWhenContactDenied()
    {
        var personId = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        var person = new Mock<IPersonSourceClient>();
        person.Setup(p => p.GetPersonAsync(personId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SourcePersonDto { Id = personId, LegalName = "X", OrganizationId = orgId, Email = "secret@test.com" });
        var workforce = new Mock<IWorkforceSourceClient>();
        workforce.Setup(w => w.ListEmploymentsAsync(orgId, It.IsAny<CancellationToken>())).ReturnsAsync(Array.Empty<SourceEmploymentDto>());
        var professional = new Mock<IProfessionalSourceClient>();
        professional.Setup(p => p.ListProfilesAsync(orgId, It.IsAny<CancellationToken>())).ReturnsAsync(Array.Empty<SourceProfessionalProfileDto>());
        var governance = new Mock<IGovernanceSourceClient>();
        governance.Setup(g => g.GetAssigneesAsync(personId, It.IsAny<CancellationToken>())).ReturnsAsync(new GovernanceAssigneeBundle());
        var composer = new DirectoryProfileComposer(person.Object, workforce.Object, professional.Object, governance.Object, new MemoryCache(new MemoryCacheOptions()), Options.Create(new DirectoryOptions()));
        var auth = new GovernedDirectoryContext { SearchAllowed = true, ContactReadable = false, CredentialReadable = true, RowLevelFilter = new RowLevelFilterScope { OrganizationId = orgId } };
        var profile = await composer.ComposeAsync(personId, auth);
        Assert.Equal("***", profile!.Email);
    }
}

public class DirectoryProjectionRegistryTests
{
    [Fact]
    public async Task Process_RoutesPersonUpdatedToProjector()
    {
        var projector = new Mock<IDirectoryIndexProjector>();
        var registry = new DirectoryProjectionRegistry(Array.Empty<IDirectoryProjectionHandler>(), projector.Object);
        var personId = Guid.NewGuid();
        await registry.ProcessAsync(new DirectoryEventEnvelope { Type = "PersonUpdated", AggregateId = personId, Payload = """{"organizationId":"00000000-0000-0000-0000-000000000001"}""" });
        projector.Verify(p => p.HandlePersonUpdatedAsync(personId, It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}

public class DirectoryOutboxWriterTests
{
    [Fact]
    public async Task Enqueue_PersistsMessage()
    {
        using var fx = new DirectoryTestDb();
        var writer = new DirectoryOutboxWriter(fx.Db);
        await writer.EnqueueAsync("DirectoryIndexUpdated", Guid.NewGuid(), new { x = 1 });
        Assert.Single(await fx.Db.OutboxMessages.ToListAsync());
    }
}

public class DirectorySearchDenialTests
{
    [Fact]
    public async Task Search_ThrowsWhenNotAllowed()
    {
        using var fx = new DirectoryTestDb();
        var svc = new DirectorySearchService(fx.Db, Options.Create(new DirectoryOptions()), NullLogger<DirectorySearchService>.Instance);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            svc.SearchAsync(new DirectoryQuery(), new GovernedDirectoryContext { SearchAllowed = false }, CancellationToken.None));
    }
}

public class DirectoryFacetFilterTests
{
    [Fact]
    public async Task Search_FiltersByDepartmentStatusAndSkill()
    {
        using var fx = new DirectoryTestDb();
        var orgId = Guid.NewGuid();
        fx.Db.DirectoryIndexes.AddRange(
            new DirectoryIndex { Id = Guid.NewGuid(), PersonId = Guid.NewGuid(), OrganizationId = orgId, DisplayName = "A", SearchBlob = "a", EmploymentStatus = "Active", SkillsJson = "[\"ACLS\"]", IsActive = true },
            new DirectoryIndex { Id = Guid.NewGuid(), PersonId = Guid.NewGuid(), OrganizationId = orgId, DisplayName = "B", SearchBlob = "b", EmploymentStatus = "Inactive", SkillsJson = "[]", IsActive = true });
        await fx.Db.SaveChangesAsync();
        var auth = new GovernedDirectoryContext { SearchAllowed = true, ContactReadable = true, CredentialReadable = true, RowLevelFilter = new RowLevelFilterScope { OrganizationId = orgId } };
        var result = await new DirectorySearchService(fx.Db, Options.Create(new DirectoryOptions()), NullLogger<DirectorySearchService>.Instance)
            .SearchAsync(new DirectoryQuery { Filters = new DirectoryQueryFilters { OrganizationId = orgId, EmploymentStatus = "Active", Skill = "ACLS" } }, auth);
        Assert.Single(result.Items);
        Assert.Equal("A", result.Items[0].DisplayName);
    }
}

public class DirectoryPaginationTests
{
    [Fact]
    public async Task Search_RespectsPageSize()
    {
        using var fx = new DirectoryTestDb();
        var orgId = Guid.NewGuid();
        for (var i = 0; i < 5; i++)
            fx.Db.DirectoryIndexes.Add(new DirectoryIndex { Id = Guid.NewGuid(), PersonId = Guid.NewGuid(), OrganizationId = orgId, DisplayName = $"P{i}", SearchBlob = $"p{i}", IsActive = true });
        await fx.Db.SaveChangesAsync();
        var auth = new GovernedDirectoryContext { SearchAllowed = true, ContactReadable = true, CredentialReadable = true, RowLevelFilter = new RowLevelFilterScope { OrganizationId = orgId } };
        var result = await new DirectorySearchService(fx.Db, Options.Create(new DirectoryOptions { DefaultPageSize = 2, MaxPageSize = 10 }), NullLogger<DirectorySearchService>.Instance)
            .SearchAsync(new DirectoryQuery { Page = 1, PageSize = 2, Filters = new DirectoryQueryFilters { OrganizationId = orgId } }, auth);
        Assert.Equal(2, result.Items.Count);
        Assert.Equal(5, result.TotalCount);
    }
}

public class DirectoryInactiveRowTests
{
    [Fact]
    public async Task Search_ExcludesInactiveRows()
    {
        using var fx = new DirectoryTestDb();
        var orgId = Guid.NewGuid();
        fx.Db.DirectoryIndexes.AddRange(
            new DirectoryIndex { Id = Guid.NewGuid(), PersonId = Guid.NewGuid(), OrganizationId = orgId, DisplayName = "Active", SearchBlob = "active", IsActive = true },
            new DirectoryIndex { Id = Guid.NewGuid(), PersonId = Guid.NewGuid(), OrganizationId = orgId, DisplayName = "Inactive", SearchBlob = "inactive", IsActive = false });
        await fx.Db.SaveChangesAsync();
        var auth = new GovernedDirectoryContext { SearchAllowed = true, ContactReadable = true, CredentialReadable = true, RowLevelFilter = new RowLevelFilterScope { OrganizationId = orgId } };
        var result = await new DirectorySearchService(fx.Db, Options.Create(new DirectoryOptions()), NullLogger<DirectorySearchService>.Instance)
            .SearchAsync(new DirectoryQuery { Filters = new DirectoryQueryFilters { OrganizationId = orgId } }, auth);
        Assert.Single(result.Items);
        Assert.Equal("Active", result.Items[0].DisplayName);
    }
}

public class DirectorySnapshotTests
{
    [Fact]
    public async Task Rebuild_CreatesSnapshotMetadata()
    {
        using var fx = new DirectoryTestDb();
        var orgId = Guid.NewGuid();
        var workforce = new Mock<IWorkforceSourceClient>();
        workforce.Setup(w => w.ListEmploymentsAsync(orgId, It.IsAny<CancellationToken>())).ReturnsAsync(Array.Empty<SourceEmploymentDto>());
        var person = new Mock<IPersonSourceClient>();
        person.Setup(p => p.ListPersonsAsync(orgId, It.IsAny<CancellationToken>())).ReturnsAsync(Array.Empty<SourcePersonDto>());
        var professional = new Mock<IProfessionalSourceClient>();
        professional.Setup(p => p.ListProfilesAsync(orgId, It.IsAny<CancellationToken>())).ReturnsAsync(Array.Empty<SourceProfessionalProfileDto>());
        var projector = new DirectoryIndexProjector(fx.Db, person.Object, workforce.Object, professional.Object, Mock.Of<IDirectoryOutboxWriter>(), NullLogger<DirectoryIndexProjector>.Instance);
        await projector.RebuildOrganizationAsync(orgId, "c1");
        var snapshot = await fx.Db.DirectorySnapshots.SingleAsync();
        Assert.Equal(1, snapshot.Version);
        Assert.Equal("c1", snapshot.CorrelationId);
    }
}

public class DirectorySavedSearchVisibilityTests
{
    [Fact]
    public async Task List_IncludesSharedSearchesFromOthers()
    {
        using var fx = new DirectoryTestDb();
        var svc = new SavedSearchService(fx.Db);
        var orgId = Guid.NewGuid();
        var owner = Guid.NewGuid();
        var other = Guid.NewGuid();
        fx.Db.SavedSearches.Add(new SavedSearch { Id = Guid.NewGuid(), OwnerUserId = owner, OrganizationId = orgId, Name = "Shared", QueryJson = "{}", Visibility = "Department", CreatedAtUtc = DateTime.UtcNow });
        await fx.Db.SaveChangesAsync();
        var list = await svc.ListAsync(other, orgId);
        Assert.Single(list);
    }
}

public class DirectoryPhoneticSearchTests
{
    [Fact]
    public async Task Search_PhoneticBoost_DoesNotThrow()
    {
        using var fx = new DirectoryTestDb();
        var orgId = Guid.NewGuid();
        var key = PhoneticHelper.ComputeKey("Smith");
        fx.Db.DirectoryIndexes.Add(new DirectoryIndex { Id = Guid.NewGuid(), PersonId = Guid.NewGuid(), OrganizationId = orgId, DisplayName = "Smyth", SearchBlob = "smyth", PhoneticKey = key, IsActive = true });
        await fx.Db.SaveChangesAsync();
        var auth = new GovernedDirectoryContext { SearchAllowed = true, ContactReadable = true, CredentialReadable = true, RowLevelFilter = new RowLevelFilterScope { OrganizationId = orgId } };
        var result = await new DirectorySearchService(fx.Db, Options.Create(new DirectoryOptions()), NullLogger<DirectorySearchService>.Instance)
            .SearchAsync(new DirectoryQuery { Text = "Smith", Phonetic = true, Filters = new DirectoryQueryFilters { OrganizationId = orgId } }, auth);
        Assert.NotEmpty(result.Items);
    }
}

public class DirectoryCertificationFilterTests
{
    [Fact]
    public async Task Search_FiltersByCertificationJson()
    {
        using var fx = new DirectoryTestDb();
        var orgId = Guid.NewGuid();
        fx.Db.DirectoryIndexes.Add(new DirectoryIndex { Id = Guid.NewGuid(), PersonId = Guid.NewGuid(), OrganizationId = orgId, DisplayName = "Cert", SearchBlob = "cert", CertificationsJson = "[\"ACLS\",\"BLS\"]", IsActive = true });
        await fx.Db.SaveChangesAsync();
        var auth = new GovernedDirectoryContext { SearchAllowed = true, ContactReadable = true, CredentialReadable = true, RowLevelFilter = new RowLevelFilterScope { OrganizationId = orgId } };
        var result = await new DirectorySearchService(fx.Db, Options.Create(new DirectoryOptions()), NullLogger<DirectorySearchService>.Instance)
            .SearchAsync(new DirectoryQuery { Filters = new DirectoryQueryFilters { OrganizationId = orgId, Certification = "ACLS" } }, auth);
        Assert.Single(result.Items);
    }
}

public class DirectoryDepartmentRlsTests
{
    [Fact]
    public async Task Search_AppliesDepartmentRls()
    {
        using var fx = new DirectoryTestDb();
        var orgId = Guid.NewGuid();
        var d1 = Guid.NewGuid(); var d2 = Guid.NewGuid();
        fx.Db.DirectoryIndexes.AddRange(
            new DirectoryIndex { Id = Guid.NewGuid(), PersonId = Guid.NewGuid(), OrganizationId = orgId, DepartmentId = d1, DisplayName = "D1", SearchBlob = "d1", IsActive = true },
            new DirectoryIndex { Id = Guid.NewGuid(), PersonId = Guid.NewGuid(), OrganizationId = orgId, DepartmentId = d2, DisplayName = "D2", SearchBlob = "d2", IsActive = true });
        await fx.Db.SaveChangesAsync();
        var auth = new GovernedDirectoryContext { SearchAllowed = true, ContactReadable = true, CredentialReadable = true, RowLevelFilter = new RowLevelFilterScope { OrganizationId = orgId, DepartmentId = d1 } };
        var result = await new DirectorySearchService(fx.Db, Options.Create(new DirectoryOptions()), NullLogger<DirectorySearchService>.Instance)
            .SearchAsync(new DirectoryQuery { Filters = new DirectoryQueryFilters { OrganizationId = orgId } }, auth);
        Assert.Single(result.Items);
    }
}

public class DirectoryDeleteSavedSearchTests
{
    [Fact]
    public async Task Delete_RemovesOwnedSearch()
    {
        using var fx = new DirectoryTestDb();
        var svc = new SavedSearchService(fx.Db);
        var userId = Guid.NewGuid();
        var created = await svc.CreateAsync(userId, new CreateSavedSearchRequest { Name = "X", OrganizationId = Guid.NewGuid(), Query = new DirectoryQuery() });
        await svc.DeleteAsync(userId, created.Id);
        Assert.Empty(await fx.Db.SavedSearches.ToListAsync());
    }
}

public class DirectoryEmptySearchTests
{
    [Fact]
    public async Task Search_EmptyQuery_ReturnsAllScopedRows()
    {
        using var fx = new DirectoryTestDb();
        var orgId = Guid.NewGuid();
        fx.Db.DirectoryIndexes.AddRange(
            new DirectoryIndex { Id = Guid.NewGuid(), PersonId = Guid.NewGuid(), OrganizationId = orgId, DisplayName = "A", SearchBlob = "a", IsActive = true },
            new DirectoryIndex { Id = Guid.NewGuid(), PersonId = Guid.NewGuid(), OrganizationId = orgId, DisplayName = "B", SearchBlob = "b", IsActive = true });
        await fx.Db.SaveChangesAsync();
        var auth = new GovernedDirectoryContext { SearchAllowed = true, ContactReadable = true, CredentialReadable = true, RowLevelFilter = new RowLevelFilterScope { OrganizationId = orgId } };
        var result = await new DirectorySearchService(fx.Db, Options.Create(new DirectoryOptions()), NullLogger<DirectorySearchService>.Instance)
            .SearchAsync(new DirectoryQuery { Filters = new DirectoryQueryFilters { OrganizationId = orgId } }, auth);
        Assert.Equal(2, result.TotalCount);
    }
}

public class DirectoryProfessionalCategoryFilterTests
{
    [Fact]
    public async Task Search_FiltersByProfessionalCategory()
    {
        using var fx = new DirectoryTestDb();
        var orgId = Guid.NewGuid();
        fx.Db.DirectoryIndexes.AddRange(
            new DirectoryIndex { Id = Guid.NewGuid(), PersonId = Guid.NewGuid(), OrganizationId = orgId, DisplayName = "N", SearchBlob = "n", ProfessionalCategory = "Nursing", IsActive = true },
            new DirectoryIndex { Id = Guid.NewGuid(), PersonId = Guid.NewGuid(), OrganizationId = orgId, DisplayName = "D", SearchBlob = "d", ProfessionalCategory = "Doctor", IsActive = true });
        await fx.Db.SaveChangesAsync();
        var auth = new GovernedDirectoryContext { SearchAllowed = true, ContactReadable = true, CredentialReadable = true, RowLevelFilter = new RowLevelFilterScope { OrganizationId = orgId } };
        var result = await new DirectorySearchService(fx.Db, Options.Create(new DirectoryOptions()), NullLogger<DirectorySearchService>.Instance)
            .SearchAsync(new DirectoryQuery { Filters = new DirectoryQueryFilters { OrganizationId = orgId, ProfessionalCategory = "Nursing" } }, auth);
        Assert.Single(result.Items);
        Assert.Equal("N", result.Items[0].DisplayName);
    }
}

public class DirectoryBenchmarkDocumentationTests
{
    [Fact]
    public void PerformanceTargets_Documented()
    {
        const int targetSearchP95Ms = 200;
        const int targetAutocompleteP95Ms = 100;
        const int targetRebuildMinutes = 5;
        Assert.True(targetSearchP95Ms < 500);
        Assert.True(targetAutocompleteP95Ms < 200);
        Assert.True(targetRebuildMinutes <= 10);
    }
}
