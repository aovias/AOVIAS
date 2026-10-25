namespace Gateway.DTOs;

public class UserAdminListItemDto
{
    public Guid MembershipId { get; init; }
    public Guid UserId { get; init; }
    public Guid OrganizationId { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string FullName { get; init; } = string.Empty;
    public string? Email { get; init; }
    public string? UserName { get; init; }
    public string? PhoneNumber { get; init; }
    public string? ProfilePicUrl { get; init; }
    public Guid? DepartmentId { get; init; }
    public string? DepartmentName { get; init; }
    public string? Designation { get; init; }
    public string Role { get; init; } = string.Empty;
    public IReadOnlyList<string> Roles { get; init; } = Array.Empty<string>();
    public string MembershipStatus { get; init; } = string.Empty;
    public string AccountStatus { get; init; } = string.Empty;
    public bool IsDeleted { get; init; }
    public bool ForcePasswordChange { get; init; }
    public bool IsLockedOut { get; init; }
    public DateTime? LastLoginAt { get; init; }
    public DateTime CreatedAt { get; init; }
    public string? EmployeeNumber { get; init; }
    public Guid? EmploymentId { get; init; }
    public Guid? PersonId { get; init; }
}

public sealed class UserAdminDetailResponseDto : UserAdminListItemDto
{
    public string PlatformId { get; init; } = string.Empty;
    public bool PasswordResetRequired { get; init; }
    public DateTimeOffset? LockoutEnd { get; init; }
}

public sealed class CreateIdentityUserAdminRequest
{
    public string Email { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string TemporaryPassword { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? Role { get; set; }
    public Guid? PersonId { get; set; }
    public Guid? DepartmentId { get; set; }
    public string? Designation { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public Guid? HospitalId { get; set; }
    public Guid? EmploymentId { get; set; }
}

public sealed class UpdateIdentityUserAdminRequest
{
    public string? Email { get; set; }
    public string? UserName { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Role { get; set; }
    public string? Status { get; set; }
    public Guid? DepartmentId { get; set; }
    public string? Designation { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? MembershipStatus { get; set; }
}

public sealed class PagedUserAdminListDto
{
    public IReadOnlyList<UserAdminListItemDto> Items { get; init; } = Array.Empty<UserAdminListItemDto>();
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
}
