using IdentityService.Application.Contracts;
using IdentityService.Application.Users;
using IdentityService.Domain;
using IdentityService.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Moq;
using Shared.Logging;
using Xunit;

namespace IdentityService.Tests;

public class UserAdminAppServiceTests
{
    [Fact]
    public async Task SuspendAsync_SetsStatusAndRevokesSessions()
    {
        var userId = Guid.NewGuid();
        var user = new AppUser { Id = userId, Email = "admin@test.com", Status = UserAccountStatus.Active };
        var userManager = CreateUserManager(user);
        var sessions = new Mock<ISessionService>();
        var audit = new Mock<IIdentityAuditService>();
        var svc = new UserAdminAppService(userManager.Object, Mock.Of<IRegistrationService>(), sessions.Object, audit.Object);

        var (success, error) = await svc.SuspendAsync(userId, Guid.NewGuid());

        Assert.True(success);
        Assert.Null(error);
        Assert.Equal(UserAccountStatus.Suspended, user.Status);
        sessions.Verify(s => s.RevokeAllSessionsAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
        audit.Verify(a => a.RecordSuccessAsync(EventCode.UserSuspended, "SuspendUser", It.IsAny<Guid?>(), userId, It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task BatchGetAsync_ReturnsOnlyExistingUsers()
    {
        var existingId = Guid.NewGuid();
        var missingId = Guid.NewGuid();
        var user = new AppUser { Id = existingId, Email = "exists@test.com", Status = UserAccountStatus.Active, PlatformId = "P1", CreatedAt = DateTime.UtcNow };
        var userManager = CreateUserManager(user);
        userManager.Setup(m => m.FindByIdAsync(missingId.ToString())).ReturnsAsync((AppUser?)null);
        userManager.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(new List<string> { "USER" });

        var svc = new UserAdminAppService(userManager.Object, Mock.Of<IRegistrationService>(), Mock.Of<ISessionService>(), Mock.Of<IIdentityAuditService>());
        var results = await svc.BatchGetAsync(new[] { existingId, missingId });

        Assert.Single(results);
        Assert.Equal(existingId, results[0].UserId);
    }

    private static Mock<UserManager<AppUser>> CreateUserManager(AppUser user)
    {
        var store = new Mock<IUserStore<AppUser>>();
        var userManager = new Mock<UserManager<AppUser>>(store.Object, null, null, null, null, null, null, null, null);
        userManager.Setup(m => m.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);
        userManager.Setup(m => m.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);
        userManager.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(new List<string>());
        userManager.Setup(m => m.SetLockoutEndDateAsync(user, null)).ReturnsAsync(IdentityResult.Success);
        userManager.Setup(m => m.ResetAccessFailedCountAsync(user)).ReturnsAsync(IdentityResult.Success);
        return userManager;
    }
}
