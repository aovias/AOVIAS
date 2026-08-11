namespace IdentityService.Application.DTOs;

public sealed class UserAuthContextDto
{
    public Guid OrganizationId { get; init; }
    public Guid HospitalId { get; init; }
    public Guid MembershipId { get; init; }
    public Guid? DepartmentId { get; init; }
    public string Role { get; init; } = string.Empty;
}
