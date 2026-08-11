namespace Gateway.DTOs;

public sealed class CreateHospitalRequestDto
{
    public Guid OrganizationId { get; set; }
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

public sealed class UpdateHospitalRequestDto
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string? Type { get; set; }
    public int? BedCapacity { get; set; }
    public string? CountryCode { get; set; }
    public string? StateCode { get; set; }
    public bool IsOnboarded { get; set; }
    public string? NABHLevel { get; set; }
}

public sealed class HospitalResponseDto
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
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
    public DateTime? UpdatedAt { get; set; }
}

