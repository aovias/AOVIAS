namespace Gateway.DTOs;

/// <summary>
/// DTO representing the complete organization hierarchy with hospitals and members.
/// </summary>
public class OrganizationDetailDto
{
    public Guid TenantId { get; set; }
    public string OrganizationName { get; set; } = string.Empty;
    public string OrganizationCode { get; set; } = string.Empty;
    public string OrganizationType { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }
    public string? TimeZoneId { get; set; }
    public string? LanguageCode { get; set; }
    public List<HospitalDetailDto> Hospitals { get; set; } = new();
    public List<MemberDto> Members { get; set; } = new();
    public DateTime CreatedOn { get; set; }
}

/// <summary>
/// DTO representing a hospital within an organization.
/// </summary>
public class HospitalDetailDto
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
    public List<JourneyDto> Journey { get; set; } = new();
}

/// <summary>
/// DTO representing an organization member.
/// </summary>
public class MemberDto
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

/// <summary>
/// DTO for compliance journey summary used in organization detail.
/// </summary>
public class JourneyDto
{
    public Guid JourneyId { get; set; }

    /// <summary>
    /// Framework the journey enrolls in. Additive field — existing clients that only read
    /// <see cref="JourneyId"/> are unaffected. Audit and compliance APIs filter by this id, never by
    /// <see cref="JourneyId"/>.
    /// </summary>
    public Guid FrameworkId { get; set; }

    public string FrameworkCode { get; set; } = string.Empty;
    public string FrameworkName { get; set; } = string.Empty;
    public DateTime? StartDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
}
