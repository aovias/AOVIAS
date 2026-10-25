namespace OrganizationService.Domain.Entities;

/// <summary>CRP Phase 4 — lightweight email delivery attempt log for commercial ops.</summary>
public sealed class EmailDeliveryLog
{
    public Guid Id { get; set; }
    public Guid? OrganizationId { get; set; }
    public Guid? UserId { get; set; }
    public string TemplateKey { get; set; } = string.Empty;
    public string ToAddress { get; set; } = string.Empty;
    public string? Subject { get; set; }
    public string Status { get; set; } = "Sent";
    public string? Error { get; set; }
    public string? CorrelationId { get; set; }
    public DateTime SentAtUtc { get; set; }
}
