namespace IdentityService.Domain.Entities;

public class LoginAttempt
{
    public Guid Id { get; set; }
    public Guid? UserId { get; set; }
    public string? Email { get; set; }
    public bool Success { get; set; }
    public string? FailureReason { get; set; }
    public string? IPAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? OperatingSystem { get; set; }
    public string? DeviceId { get; set; }
    public string? CorrelationId { get; set; }
    public Guid? SessionId { get; set; }
    public DateTime CreatedAt { get; set; }
}
