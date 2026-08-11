namespace Gateway.DTOs;

public class SignupResponseDto
{
    public string Token { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public int ExpiresIn { get; set; }
    public Guid UserId { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid HospitalId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;

    /// <summary>Commercial trial license key id when provisioned.</summary>
    public Guid? LicenseKeyId { get; set; }

    /// <summary>Issued | Active | Grace | …</summary>
    public string? LicenseStatus { get; set; }

    public DateTime? TrialExpiresAtUtc { get; set; }
    public int? MaxSeats { get; set; }

    /// <summary>
    /// Completed | Skipped | LicenseIssuedPendingActivation | ProvisioningFailed
    /// </summary>
    public string ProvisioningStatus { get; set; } = "Skipped";
}
