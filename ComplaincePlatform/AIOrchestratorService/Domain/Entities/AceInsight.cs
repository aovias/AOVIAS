namespace AIOrchestratorService.Domain.Entities;

/// <summary>
/// Durable Autonomous Compliance Engine (ACE) insight — advisory only.
/// </summary>
public sealed class AceInsight
{
    public Guid Id { get; set; }
    public Guid HospitalId { get; set; }
    public Guid? FrameworkId { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string Reasoning { get; set; } = string.Empty;
    public decimal Confidence { get; set; }
    public int Priority { get; set; }
    public string Status { get; set; } = "Open";
    public string CitationsJson { get; set; } = "[]";
    public string SuggestedActionsJson { get; set; } = "[]";
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? AcknowledgedAtUtc { get; set; }
    public Guid? AcknowledgedByUserId { get; set; }
}
