namespace Gateway.DTOs;

public sealed class ProvisionTrialRequestDto
{
    public Guid OrganizationId { get; set; }
    public string? CustomerName { get; set; }
    public int MaxSeats { get; set; } = 10;
    public int ValidDays { get; set; } = 30;
    public int GracePeriodDays { get; set; } = 7;
    public List<string>? Entitlements { get; set; }
}

public sealed class ProvisionTrialResponseDto
{
    public Guid OrganizationId { get; set; }
    public string? LicenseKeyCode { get; set; }
    public Guid? LicenseKeyId { get; set; }
    public DateTime? ExpiresAtUtc { get; set; }
    public int MaxSeats { get; set; }
    public IReadOnlyList<string> Checklist { get; set; } = Array.Empty<string>();
    public string? Message { get; set; }
}

public sealed class CustomerHealthDto
{
    public Guid OrganizationId { get; set; }
    public object? LicenseStatus { get; set; }
    public IReadOnlyList<ReadinessStub> Readiness { get; set; } = Array.Empty<ReadinessStub>();
}

public sealed class ReadinessStub
{
    public string Area { get; set; } = string.Empty;
    public string Status { get; set; } = "unknown";
    public string? Detail { get; set; }
}
