using IdentityService.Application.Contracts;
using IdentityService.Application.Services;
using IdentityService.Domain;
using IdentityService.Domain.Entities;
using IdentityService.Domain.Enums;
using IdentityService.Infrastructure;
using IdentityService.Infrastructure.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Shared.Logging;
using Xunit;

namespace IdentityService.Tests;

public class IdentityTestFixture : IDisposable
{
    public ServiceProvider Provider { get; }
    public IdentityDbContext Db { get; }

    public IdentityTestFixture()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<IdentityDbContext>(o => o.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        services.AddIdentity<AppUser, IdentityRole<Guid>>(options =>
            {
                options.Password.RequireDigit = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequiredLength = 6;
            })
            .AddEntityFrameworkStores<IdentityDbContext>()
            .AddDefaultTokenProviders();

        services.AddScoped<IRevokedAccessTokenService, RevokedAccessTokenService>();
        services.AddScoped<ITokenValidationService, TokenValidationService>();

        Provider = services.BuildServiceProvider();
        Db = Provider.GetRequiredService<IdentityDbContext>();
    }

    public void Dispose() => Provider.Dispose();
}

public class TokenValidationServiceTests
{
    [Fact]
    public async Task ValidateAsync_RejectsRevokedJti()
    {
        using var fx = new IdentityTestFixture();
        var userManager = fx.Provider.GetRequiredService<UserManager<AppUser>>();
        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            UserName = "test@example.com",
            Email = "test@example.com",
            PlatformId = "USR000000000001",
            Status = UserAccountStatus.Active,
            EmailConfirmed = true
        };
        await userManager.CreateAsync(user, "Password1!");

        var revoked = fx.Provider.GetRequiredService<IRevokedAccessTokenService>();
        await revoked.RevokeAsync("jti-revoked", user.Id, DateTime.UtcNow.AddHours(1), CancellationToken.None);

        var validator = fx.Provider.GetRequiredService<ITokenValidationService>();
        var stamp = await userManager.GetSecurityStampAsync(user) ?? string.Empty;
        var valid = await validator.ValidateAsync(user.Id, "jti-revoked", user.TokenVersion, stamp, CancellationToken.None);

        Assert.False(valid);
    }

    [Fact]
    public async Task ValidateAsync_RejectsStaleTokenVersion()
    {
        using var fx = new IdentityTestFixture();
        var userManager = fx.Provider.GetRequiredService<UserManager<AppUser>>();
        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            UserName = "v2@example.com",
            Email = "v2@example.com",
            PlatformId = "USR000000000002",
            Status = UserAccountStatus.Active,
            TokenVersion = 2,
            EmailConfirmed = true
        };
        await userManager.CreateAsync(user, "Password1!");

        var validator = fx.Provider.GetRequiredService<ITokenValidationService>();
        var stamp = await userManager.GetSecurityStampAsync(user) ?? string.Empty;
        var valid = await validator.ValidateAsync(user.Id, "jti-live", tokenVersion: 1, stamp, CancellationToken.None);

        Assert.False(valid);
    }

    [Fact]
    public async Task ValidateAsync_AcceptsValidToken()
    {
        using var fx = new IdentityTestFixture();
        var userManager = fx.Provider.GetRequiredService<UserManager<AppUser>>();
        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            UserName = "ok@example.com",
            Email = "ok@example.com",
            PlatformId = "USR000000000003",
            Status = UserAccountStatus.Active,
            EmailConfirmed = true
        };
        await userManager.CreateAsync(user, "Password1!");

        var validator = fx.Provider.GetRequiredService<ITokenValidationService>();
        var stamp = await userManager.GetSecurityStampAsync(user) ?? string.Empty;
        var valid = await validator.ValidateAsync(user.Id, "jti-ok", user.TokenVersion, stamp, CancellationToken.None);

        Assert.True(valid);
    }
}
