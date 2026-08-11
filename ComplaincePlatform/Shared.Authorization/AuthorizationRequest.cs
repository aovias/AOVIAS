namespace Shared.Authorization;

public sealed class AuthorizationRequest
{
    public required Guid SubjectId { get; init; }
    public Guid? PersonId { get; init; }
    public Guid? EmploymentId { get; init; }
    public Guid? ProfessionalProfileId { get; init; }
    public Guid? OrganizationId { get; init; }
    public Guid? HospitalId { get; init; }
    public Guid? DepartmentId { get; init; }
    public required string Resource { get; init; }
    public required string Action { get; init; }
    public IReadOnlyDictionary<string, string> Context { get; init; } = new Dictionary<string, string>();
}
