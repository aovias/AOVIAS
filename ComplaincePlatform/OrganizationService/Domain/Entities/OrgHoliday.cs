namespace OrganizationService.Domain.Entities;

/// <summary>
/// Organization business-calendar holiday / non-working date (Phase 2 Sprint 5).
/// </summary>
public class OrgHoliday
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OrganizationId { get; set; }
    public DateOnly Date { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
