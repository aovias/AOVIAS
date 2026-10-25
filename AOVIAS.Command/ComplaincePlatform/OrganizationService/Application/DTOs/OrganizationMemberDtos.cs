namespace OrganizationService.Application.DTOs;

public class CreateOrganizationMemberDto
{
    public Guid OrganizationId { get; set; }
    public Guid UserId { get; set; }

    /// <summary>Optional master department id (onboarding team member).</summary>
    public Guid? DepartmentId { get; set; }

    public string? Designation { get; set; }
    public Guid? ReportsToUserId { get; set; }

    public string Role { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string? AlternatePhoneNumber { get; set; }
    public string? ProfilePicUrl { get; set; }
    public string? Preferences { get; set; }
}

public class UpdateOrganizationMemberDto
{
    public string? Role { get; set; }
    public Guid? DepartmentId { get; set; }
    public string? Designation { get; set; }
    public Guid? ReportsToUserId { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string? AlternatePhoneNumber { get; set; }
    public string? ProfilePicUrl { get; set; }
    public string? Preferences { get; set; }
}

public class OrganizationMemberResponseDto
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid UserId { get; set; }
    public Guid? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public string? Designation { get; set; }
    public Guid? ReportsToUserId { get; set; }
    public string Role { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string? AlternatePhoneNumber { get; set; }
    public string? ProfilePicUrl { get; set; }
    public string? Preferences { get; set; }
    public DateTime CreatedAt { get; set; }
}
