namespace ComplianceService.Application.Contracts;

/// <summary>
/// Service to extract and provide user context from JWT claims
/// </summary>
public interface IUserContextService
{
    /// <summary>
    /// Organization ID from the JWT token (organization_id claim)
    /// </summary>
    Guid OrganizationId { get; }

    /// <summary>
    /// Hospital ID from the JWT token (hospital_id claim)
    /// </summary>
    Guid HospitalId { get; }

    /// <summary>
    /// All role claims from the JWT (may be multiple).
    /// </summary>
    IReadOnlyList<string> Roles { get; }

    /// <summary>
    /// First role claim for backward compatibility; prefer <see cref="Roles"/>.
    /// </summary>
    string Role { get; }

    /// <summary>
    /// User ID from the JWT token (sub or NameIdentifier claim)
    /// </summary>
    Guid UserId { get; }

    /// <summary>
    /// Email from the JWT token (email claim)
    /// </summary>
    string Email { get; }
}
