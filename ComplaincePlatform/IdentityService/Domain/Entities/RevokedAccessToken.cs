namespace IdentityService.Domain.Entities;

public class RevokedAccessToken
{
    public Guid Id { get; set; }
    public string Jti { get; set; } = string.Empty;
    public Guid? UserId { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime RevokedAt { get; set; }
}
