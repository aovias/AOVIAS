namespace AIOrchestratorService.Domain;

public class RequestContext
{
    public Guid? UserId { get; set; }
    public string? Email { get; set; }
    public Guid? OrganizationId { get; set; }
    public List<string> Roles { get; set; } = new();
    public List<string> Scopes { get; set; } = new();
    public string? CorrelationId { get; set; }
}
