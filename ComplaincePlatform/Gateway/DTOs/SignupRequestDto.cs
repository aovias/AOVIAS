namespace Gateway.DTOs;

public class SignupRequestDto
{
    public OrganizationDto Organization { get; set; } = new();
    public HospitalDto Hospital { get; set; } = new();
    public AdminUserDto AdminUser { get; set; } = new();
}

public class OrganizationDto
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
}

public class HospitalDto
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string? Type { get; set; }
    public int? BedCapacity { get; set; }
    public string? CountryCode { get; set; }
    public string? StateCode { get; set; }
    public bool? IsOnboarded { get; set; }
    public string? NABHLevel { get; set; }
}

public class AdminUserDto
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? AlternatePhoneNumber { get; set; }
    public string? ProfilePicUrl { get; set; }
    public string? Preferences { get; set; }
    public string Password { get; set; } = string.Empty;
}
