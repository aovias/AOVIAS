using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using OrganizationService.Application.Contracts;
using OrganizationService.Application.Services;
using OrganizationService.Configuration;
using OrganizationService.Domain;
using OrganizationService.Domain.Enums;
using OrganizationService.Infrastructure.Persistence;
using Shared.Communication;
using Shared.Infrastructure.Auth;
using Shared.Infrastructure.Domain;
using Shared.Infrastructure.Email;
using Xunit;

namespace OrganizationService.Tests;

public class OrganizationPhase4Tests
{
    private static OrganizationDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<OrganizationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new OrganizationDbContext(options);
    }

    private static LicenseAppService CreateLicenses(OrganizationDbContext db)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Licensing:SigningKey"] = "TEST",
                ["Licensing:SyncCatalogFromLicense"] = "false"
            })
            .Build();
        return new LicenseAppService(db, config, NullLogger<LicenseAppService>.Instance, new EntitlementEngine(config));
    }

    private static (MembershipAppService Membership, OrganizationDbContext Db) CreateMembershipStack()
    {
        var db = CreateDb();
        var timeline = new OrganizationTimelineProjector(db);
        var svc = new MembershipAppService(db, timeline, CreateLicenses(db));
        return (svc, db);
    }

    private static InvitationAppService CreateInvitations(OrganizationDbContext db, MembershipAppService membership)
    {
        var timeline = new OrganizationTimelineProjector(db);
        var communication = new Mock<ICommunicationClient>();
        communication.Setup(c => c.SendAsync(It.IsAny<CommunicationRequestDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CommunicationSendResultDto { Status = "Queued" });
        var email = new Mock<IEmailService>();
        email.Setup(e => e.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        return new InvitationAppService(
            db,
            membership,
            timeline,
            communication.Object,
            email.Object,
            Options.Create(new WorkforceOnboardingOptions()),
            NullLogger<InvitationAppService>.Instance);
    }

    private static async Task<Guid> SeedOrgAsync(OrganizationDbContext db)
    {
        var org = new Organization { Id = Guid.NewGuid(), Name = "Test Org", Type = "Hospital", Address = "A", PlatformId = "ORG001", OrganizationCode = "ORG001", CreatedAt = DateTime.UtcNow, RowVersion = new byte[8] };
        db.Organizations.Add(org);
        await db.SaveChangesAsync();
        return org.Id;
    }

    [Fact]
    public async Task Second_primary_membership_same_org_rejected()
    {
        var (svc, db) = CreateMembershipStack();
        var orgId = await SeedOrgAsync(db);
        var userId = Guid.NewGuid();
        var actor = Guid.NewGuid();
        await svc.CreateAsync(new CreateMembershipRequest { OrganizationId = orgId, UserId = userId, Role = ApplicationRoles.USER, IsPrimary = true }, actor);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            svc.CreateAsync(new CreateMembershipRequest { OrganizationId = orgId, UserId = userId, Role = ApplicationRoles.MANAGER, IsPrimary = true }, actor));
    }

    [Fact]
    public async Task EffectiveTo_before_EffectiveFrom_rejected()
    {
        var (svc, _) = CreateMembershipStack();
        var from = DateTime.UtcNow;
        await Assert.ThrowsAsync<InvalidOperationException>(() => svc.CreateAsync(new CreateMembershipRequest
        {
            OrganizationId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Role = ApplicationRoles.USER,
            EffectiveFrom = from,
            EffectiveTo = from.AddDays(-1)
        }, Guid.NewGuid()));
    }

    [Fact]
    public void Circular_hierarchy_detected()
    {
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var dict = new Dictionary<Guid, Guid?> { [a] = b, [b] = a };
        Assert.Throws<InvalidOperationException>(() => HierarchyValidator.EnsureNoCircularParent(dict, a, b));
    }

    [Fact]
    public async Task Invitation_pending_email_unique()
    {
        await using var db = CreateDb();
        var membership = new MembershipAppService(db, new OrganizationTimelineProjector(db), CreateLicenses(db));
        var invitations = CreateInvitations(db, membership);
        var orgId = await SeedOrgAsync(db);
        var hospitalId = Guid.NewGuid();
        db.Hospitals.Add(new Hospital { Id = hospitalId, OrganizationId = orgId, Name = "H1", Code = "H1", Address = "A", CreatedAt = DateTime.UtcNow, RowVersion = new byte[8] });
        await db.SaveChangesAsync();

        await invitations.CreateAsync(new CreateInvitationRequest
        {
            OrganizationId = orgId,
            HospitalId = hospitalId,
            Role = ApplicationRoles.USER,
            Email = "user@test.com",
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        }, Guid.NewGuid());

        await Assert.ThrowsAsync<InvalidOperationException>(() => invitations.CreateAsync(new CreateInvitationRequest
        {
            OrganizationId = orgId,
            HospitalId = hospitalId,
            Role = ApplicationRoles.USER,
            Email = "user@test.com",
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        }, Guid.NewGuid()));
    }

    [Fact]
    public async Task Soft_deleted_member_excluded_from_list()
    {
        var (svc, db) = CreateMembershipStack();
        var orgId = await SeedOrgAsync(db);
        var userId = Guid.NewGuid();
        var created = await svc.CreateAsync(new CreateMembershipRequest { OrganizationId = orgId, UserId = userId, Role = ApplicationRoles.USER, IsPrimary = false }, Guid.NewGuid());
        var entity = await db.Members.FirstAsync(m => m.Id == created.Id);
        entity.IsDeleted = true;
        await db.SaveChangesAsync();
        var list = await svc.ListAsync(orgId, null, 1, 20);
        Assert.Empty(list.Items);
    }

    [Fact]
    public async Task Membership_create_writes_timeline_and_outbox()
    {
        var (svc, db) = CreateMembershipStack();
        var orgId = await SeedOrgAsync(db);
        await svc.CreateAsync(new CreateMembershipRequest { OrganizationId = orgId, UserId = Guid.NewGuid(), Role = ApplicationRoles.USER }, Guid.NewGuid());
        Assert.True(await db.OutboxMessages.AnyAsync(m => m.Type == "MembershipCreated"));
        Assert.True(await db.OrganizationTimeline.AnyAsync(t => t.EventType == "MembershipCreated"));
    }

    [Fact]
    public async Task Auth_context_primary_membership_preferred()
    {
        await using var db = CreateDb();
        var orgId = await SeedOrgAsync(db);
        var userId = Guid.NewGuid();
        var secondary = new OrganizationMember
        {
            Id = Guid.NewGuid(), OrganizationId = orgId, UserId = userId, Role = ApplicationRoles.USER,
            MembershipStatus = MembershipStatuses.Active, EffectiveFrom = DateTime.UtcNow.AddDays(-10),
            IsPrimary = false, CreatedAt = DateTime.UtcNow.AddDays(-10), RowVersion = new byte[8]
        };
        var primary = new OrganizationMember
        {
            Id = Guid.NewGuid(), OrganizationId = orgId, UserId = userId, Role = ApplicationRoles.MANAGER,
            MembershipStatus = MembershipStatuses.Active, EffectiveFrom = DateTime.UtcNow.AddDays(-1),
            IsPrimary = true, CreatedAt = DateTime.UtcNow, RowVersion = new byte[8]
        };
        db.Members.AddRange(secondary, primary);
        await db.SaveChangesAsync();

        var now = DateTime.UtcNow;
        var member = await db.Members.Where(m => m.UserId == userId && !m.IsDeleted
            && m.MembershipStatus == MembershipStatuses.Active
            && m.EffectiveFrom <= now && (m.EffectiveTo == null || m.EffectiveTo > now))
            .OrderByDescending(m => m.IsPrimary).ThenBy(m => m.CreatedAt).FirstAsync();

        Assert.Equal(primary.Id, member.Id);
        Assert.Equal(ApplicationRoles.MANAGER, member.Role);
    }

    [Fact]
    public async Task Legacy_member_create_sets_defaults()
    {
        await using var db = CreateDb();
        var orgId = await SeedOrgAsync(db);
        var uow = new OrganizationService.Infrastructure.Repositories.UnitOfWork(db);
        var repo = new OrganizationService.Infrastructure.Repositories.OrganizationMemberRepository(db);
        var legacy = new OrganizationMemberAppService(repo, uow);
        var member = await legacy.CreateAsync(new OrganizationService.Application.DTOs.CreateOrganizationMemberDto
        {
            OrganizationId = orgId,
            UserId = Guid.NewGuid(),
            Role = ApplicationRoles.USER,
            Email = "legacy@test.com"
        });
        Assert.Equal(MembershipStatuses.Active, member.MembershipStatus);
        Assert.True(member.IsPrimary);
        Assert.True(member.EffectiveFrom <= DateTime.UtcNow);
    }
}
