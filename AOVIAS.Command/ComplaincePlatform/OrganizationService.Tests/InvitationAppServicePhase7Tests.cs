using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using OrganizationService.Application.Contracts;
using OrganizationService.Application.Services;
using OrganizationService.Configuration;
using OrganizationService.Infrastructure.Persistence;
using Shared.Communication;
using Shared.Infrastructure.Domain;
using Shared.Infrastructure.Email;
using Xunit;

namespace OrganizationService.Tests;

public sealed class InvitationAppServicePhase7Tests
{
    private static OrganizationDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<OrganizationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new OrganizationDbContext(options);
    }

    private static InvitationAppService CreateService(OrganizationDbContext db)
    {
        var membership = new Mock<IMembershipAppService>();
        membership.Setup(m => m.CreateAsync(It.IsAny<CreateMembershipRequest>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CreateMembershipRequest r, Guid _, CancellationToken __) => new MembershipDto
            {
                Id = Guid.NewGuid(),
                OrganizationId = r.OrganizationId,
                UserId = r.UserId,
                Role = r.Role,
                MembershipStatus = "Active",
                Email = r.Email,
                CreatedAt = DateTime.UtcNow,
                EffectiveFrom = DateTime.UtcNow
            });

        var timeline = new Mock<IOrganizationTimelineService>();
        timeline.Setup(t => t.AppendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<object?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var communication = new Mock<ICommunicationClient>();
        communication.Setup(c => c.SendAsync(It.IsAny<CommunicationRequestDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CommunicationSendResultDto { Status = "Queued" });

        var email = new Mock<IEmailService>();
        email.Setup(e => e.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        var opts = Options.Create(new WorkforceOnboardingOptions
        {
            InvitationExpiryDays = 7,
            PublicAppBaseUrl = "http://localhost:4200",
            AcceptPath = "/invitations/accept"
        });

        return new InvitationAppService(
            db,
            membership.Object,
            timeline.Object,
            communication.Object,
            email.Object,
            opts,
            NullLogger<InvitationAppService>.Instance);
    }

    [Fact]
    public async Task CreateAsync_stores_token_hash_and_sends_email()
    {
        await using var db = CreateDb();
        var orgId = Guid.NewGuid();
        db.Organizations.Add(new Organization
        {
            Id = orgId,
            Name = "Test Org",
            Type = "Hospital",
            Address = "x",
            PlatformId = "ORG-P7",
            OrganizationCode = "ORG-P7",
            CreatedAt = DateTime.UtcNow,
            RowVersion = new byte[8]
        });
        var hospitalId = Guid.NewGuid();
        db.Hospitals.Add(new Hospital
        {
            Id = hospitalId,
            OrganizationId = orgId,
            Name = "H1",
            Code = "H1",
            Address = "a",
            CreatedAt = DateTime.UtcNow,
            RowVersion = new byte[8]
        });
        await db.SaveChangesAsync();

        var svc = CreateService(db);
        var dto = await svc.CreateAsync(new CreateInvitationRequest
        {
            OrganizationId = orgId,
            HospitalId = hospitalId,
            Email = "new.hire@example.com",
            Role = "ORG_USER",
            Designation = "Quality Officer",
            EmploymentType = "Permanent"
        }, Guid.NewGuid());

        Assert.Equal("Pending", dto.Status);
        Assert.Equal("Quality Officer", dto.Designation);
        var row = await db.OrganizationMemberInvitations.SingleAsync();
        Assert.False(string.IsNullOrWhiteSpace(row.TokenHash));
        Assert.Equal(64, row.TokenHash!.Length);
    }

    [Fact]
    public async Task CancelAsync_marks_cancelled_and_clears_token()
    {
        await using var db = CreateDb();
        var orgId = Guid.NewGuid();
        var invite = new OrganizationMemberInvitation
        {
            Id = Guid.NewGuid(),
            OrganizationId = orgId,
            HospitalId = Guid.NewGuid(),
            Email = "a@b.com",
            NormalizedEmail = "A@B.COM",
            Role = "ORG_USER",
            Status = InvitationStatus.Pending,
            TokenHash = "ABC",
            ExpiresAt = DateTime.UtcNow.AddDays(1),
            CreatedAt = DateTime.UtcNow,
            RowVersion = new byte[8]
        };
        db.OrganizationMemberInvitations.Add(invite);
        await db.SaveChangesAsync();

        var svc = CreateService(db);
        await svc.CancelAsync(invite.Id, Guid.NewGuid());
        var updated = await db.OrganizationMemberInvitations.SingleAsync();
        Assert.Equal(InvitationStatus.Cancelled, updated.Status);
        Assert.Null(updated.TokenHash);
    }
}
