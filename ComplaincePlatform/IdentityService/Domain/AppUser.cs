using IdentityService.Domain.Enums;
using Microsoft.AspNetCore.Identity;

namespace IdentityService.Domain;

public class AppUser : IdentityUser<Guid>
{
    public string PlatformId { get; set; } = string.Empty;
    public UserAccountStatus Status { get; set; } = UserAccountStatus.Active;
    public int TokenVersion { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public DateTime? LastFailedLoginAt { get; set; }
    public DateTime? PasswordChangedAt { get; set; }
    public bool ForcePasswordChange { get; set; }
    public bool PasswordResetRequired { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public Guid? PersonId { get; set; }
}
