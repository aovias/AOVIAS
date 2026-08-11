namespace IdentityService.Domain.Entities;

public class PasswordHistory
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string PasswordHash { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
