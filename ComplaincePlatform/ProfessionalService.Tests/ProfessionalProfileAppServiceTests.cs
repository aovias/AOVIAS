using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;
using ProfessionalService.Application.Contracts;
using ProfessionalService.Application.DTOs;
using ProfessionalService.Application.Services;
using ProfessionalService.Domain.Aggregates;
using ProfessionalService.Domain.Entities;
using ProfessionalService.Domain.Enums;
using ProfessionalService.Infrastructure.Persistence;
using Shared.Infrastructure.Services;
using Shared.Logging.Audit;
using Xunit;

namespace ProfessionalService.Tests;

public class ProfessionalProfileAppServiceTests
{
    private static ProfessionalDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<ProfessionalDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ProfessionalDbContext(options);
    }

    private static readonly Guid PersonId = Guid.NewGuid();
    private static readonly Guid EmploymentId = Guid.NewGuid();

    [Fact]
    public async Task CreateAsync_SucceedsWithMockWorkforceClient()
    {
        await using var db = CreateDb();
        var service = BuildService(db);
        var created = await service.CreateAsync(new CreateProfessionalProfileRequest { EmploymentId = EmploymentId });
        Assert.StartsWith("PRO", created.ProfessionalNumber);
        Assert.Equal(PersonId, created.PersonId);
    }

    [Fact]
    public async Task CreateAsync_RejectsDuplicatePerson()
    {
        await using var db = CreateDb();
        var service = BuildService(db);
        await service.CreateAsync(new CreateProfessionalProfileRequest { EmploymentId = EmploymentId });
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateAsync(new CreateProfessionalProfileRequest { EmploymentId = EmploymentId }));
    }

    [Fact]
    public async Task UpdateAsync_PreservesProfessionalNumber()
    {
        await using var db = CreateDb();
        var service = BuildService(db);
        var created = await service.CreateAsync(new CreateProfessionalProfileRequest { EmploymentId = EmploymentId });
        var updated = await service.UpdateAsync(created.Id, new UpdateProfessionalProfileRequest { Biography = "Updated bio" });
        Assert.Equal(created.ProfessionalNumber, updated!.ProfessionalNumber);
    }

    [Fact]
    public async Task AddRegistration_RejectsDuplicateWithinAuthority()
    {
        await using var db = CreateDb();
        var service = BuildService(db);
        var profile = await service.CreateAsync(new CreateProfessionalProfileRequest { EmploymentId = EmploymentId });
        var authorityId = Guid.NewGuid();
        await service.AddRegistrationAsync(profile.Id, new AddRegistrationRequest { RegistrationAuthorityValueId = authorityId, RegistrationNumber = "REG001", IssueDate = DateTime.UtcNow.AddYears(-1), ExpiryDate = DateTime.UtcNow.AddYears(1) });
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.AddRegistrationAsync(profile.Id, new AddRegistrationRequest { RegistrationAuthorityValueId = authorityId, RegistrationNumber = "REG001" }));
    }

    [Fact]
    public async Task AddLicense_RejectsOverlappingActiveLicense()
    {
        await using var db = CreateDb();
        var service = BuildService(db);
        var profile = await service.CreateAsync(new CreateProfessionalProfileRequest { EmploymentId = EmploymentId });
        var typeId = Guid.NewGuid();
        await service.AddLicenseAsync(profile.Id, new AddLicenseRequest { LicenseTypeValueId = typeId, LicenseNumber = "LIC001", IssueDate = DateTime.UtcNow.AddYears(-1), ExpiryDate = DateTime.UtcNow.AddYears(1) });
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.AddLicenseAsync(profile.Id, new AddLicenseRequest { LicenseTypeValueId = typeId, LicenseNumber = "LIC002" }));
    }

    [Fact]
    public async Task AddRegistration_RejectsExpiryBeforeIssue()
    {
        await using var db = CreateDb();
        var service = BuildService(db);
        var profile = await service.CreateAsync(new CreateProfessionalProfileRequest { EmploymentId = EmploymentId });
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.AddRegistrationAsync(profile.Id, new AddRegistrationRequest { RegistrationNumber = "X", IssueDate = DateTime.UtcNow, ExpiryDate = DateTime.UtcNow.AddDays(-1) }));
    }

    [Fact]
    public async Task ApproveCredentialing_RequiresValidState()
    {
        await using var db = CreateDb();
        var service = BuildService(db);
        var profile = await service.CreateAsync(new CreateProfessionalProfileRequest { EmploymentId = EmploymentId });
        var record = await service.SubmitCredentialingAsync(profile.Id, new SubmitCredentialingRequest());
        Assert.NotNull(record);
        var approved = await service.ApproveCredentialingAsync(profile.Id, record!.Id, new ApproveCredentialingRequest { ApproverUserId = Guid.NewGuid() });
        Assert.Equal(CredentialingStatus.Approved, approved!.Status);
    }

    [Fact]
    public async Task GrantPrivilege_WithoutApprover_StaysPending()
    {
        await using var db = CreateDb();
        var service = BuildService(db);
        var profile = await service.CreateAsync(new CreateProfessionalProfileRequest { EmploymentId = EmploymentId });
        var priv = await service.GrantPrivilegeAsync(profile.Id, new GrantClinicalPrivilegeRequest { PrivilegeName = "ICU", EffectiveFrom = DateTime.UtcNow });
        Assert.Equal("PendingApproval", priv!.Status);
    }

    [Fact]
    public async Task DeleteAsync_SoftDeletesProfile()
    {
        await using var db = CreateDb();
        var service = BuildService(db);
        var profile = await service.CreateAsync(new CreateProfessionalProfileRequest { EmploymentId = EmploymentId });
        Assert.True(await service.DeleteAsync(profile.Id, Guid.NewGuid()));
        var stored = await db.ProfessionalProfiles.FirstAsync(p => p.Id == profile.Id);
        Assert.True(stored.IsDeleted);
    }

    [Fact]
    public async Task AddRegistration_WritesOutboxEvent()
    {
        await using var db = CreateDb();
        var service = BuildService(db);
        var profile = await service.CreateAsync(new CreateProfessionalProfileRequest { EmploymentId = EmploymentId });
        await service.AddRegistrationAsync(profile.Id, new AddRegistrationRequest { RegistrationNumber = "REG99", IssueDate = DateTime.UtcNow.AddYears(-1), ExpiryDate = DateTime.UtcNow.AddYears(1) });
        Assert.Contains(db.OutboxMessages, m => m.Type == "RegistrationAdded");
    }

    private static ProfessionalProfileAppService BuildService(ProfessionalDbContext db)
    {
        var idGen = new Mock<IPlatformIdGenerator>();
        idGen.Setup(x => x.GenerateAsync("PRO", It.IsAny<CancellationToken>())).ReturnsAsync("PRO000001");
        var audit = new ProfessionalAuditService(db, Mock.Of<IAuditLogger>());
        var workforce = new Mock<IWorkforceServiceClient>();
        workforce.Setup(x => x.GetEmploymentAsync(EmploymentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EmploymentRefDto { EmploymentId = EmploymentId, PersonId = PersonId, OrganizationId = Guid.NewGuid(), HospitalId = Guid.NewGuid(), PersonSearchName = "DR TEST" });
        var org = new Mock<IOrganizationServiceClient>();
        org.Setup(x => x.ValidateDepartmentAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var http = new Mock<IHttpContextAccessor>();
        return new ProfessionalProfileAppService(db, idGen.Object, audit, workforce.Object, org.Object, http.Object);
    }
}
