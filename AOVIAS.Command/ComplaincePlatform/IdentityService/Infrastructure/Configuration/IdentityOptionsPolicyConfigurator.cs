using IdentityService.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
namespace IdentityService.Infrastructure.Configuration;

/// <summary>
/// Applies <see cref="UserSecurityPolicy"/> values to ASP.NET Identity options at startup.
/// </summary>
public sealed class IdentityOptionsPolicyConfigurator : IConfigureOptions<IdentityOptions>
{
    public void Configure(IdentityOptions options)
    {
        var policy = IdentitySecurityPolicyState.Current;

        options.Password.RequiredLength = policy.PasswordMinLength;
        options.Password.RequireDigit = policy.PasswordRequireDigit;
        options.Password.RequireUppercase = policy.PasswordRequireUppercase;
        options.Password.RequireLowercase = policy.PasswordRequireLowercase;
        options.Password.RequireNonAlphanumeric = policy.PasswordRequireNonAlphanumeric;
        options.Lockout.MaxFailedAccessAttempts = policy.LockoutMaxFailedAttempts;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(policy.LockoutDurationMinutes);
        options.Lockout.AllowedForNewUsers = true;
    }
}

/// <summary>
/// In-memory snapshot of the effective security policy for JWT TTL and login gates.
/// </summary>
public static class IdentitySecurityPolicyState
{
    private static UserSecurityPolicy _current = new()
    {
        Id = Guid.Empty,
        Name = "Default",
        CreatedAt = DateTime.UtcNow,
        IsActive = true
    };

    public static UserSecurityPolicy Current => _current;

    public static void Update(UserSecurityPolicy policy) => _current = policy;
}
