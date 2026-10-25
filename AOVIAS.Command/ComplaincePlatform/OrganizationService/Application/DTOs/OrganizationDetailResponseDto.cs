namespace OrganizationService.Application.DTOs;

/// <summary>
/// DTO representing the complete organization hierarchy with hospitals and members.
/// </summary>
public class OrganizationDetailResponseDto
{
    public Guid TenantId { get; set; }
    public string OrganizationName { get; set; } = string.Empty;
    public string OrganizationCode { get; set; } = string.Empty;
    public string OrganizationType { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }
    public string? TimeZoneId { get; set; }
    public string? LanguageCode { get; set; }
    public List<HospitalDetailResponseDto> Hospitals { get; set; } = new();
    public List<MemberDetailResponseDto> Members { get; set; } = new();
    public DateTime CreatedOn { get; set; }
}

/// <summary>
/// DTO representing a hospital within an organization.
/// </summary>
public class HospitalDetailResponseDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string? Type { get; set; }
    public int? BedCapacity { get; set; }
    public string? CountryCode { get; set; }
    public string? StateCode { get; set; }
    public bool IsOnboarded { get; set; }
    public string? NABHLevel { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// DTO representing an organization member.
/// </summary>
public class MemberDetailResponseDto
{
    public Guid Id { get; set; }
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
