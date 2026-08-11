using Shared.Infrastructure.DTO;

namespace Gateway.DTOs;

public class SubscriptionPlanDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int MaxUsers { get; set; }
    public int MaxFrameworks { get; set; }
    public string BillingPeriod { get; set; } = string.Empty;
    public decimal PriceAmount { get; set; }
    public string Currency { get; set; } = string.Empty;
}

public class OrganizationSubscriptionSummaryDto
{
    public Guid SubscriptionId { get; set; }
    public Guid OrganizationId { get; set; }
    public SubscriptionPlanDto Plan { get; set; } = new();
    public DateTime StartsAt { get; set; }
    public DateTime? EndsAt { get; set; }
    public bool IsTrial { get; set; }
    public bool IsActive { get; set; }
    public int CurrentUserCount { get; set; }
    public int CurrentFrameworkCount { get; set; }
}

public class CreateSubscriptionRequestDto
{
    public Guid OrganizationId { get; set; }
    public Guid SubscriptionPlanId { get; set; }
    public bool IsTrial { get; set; }
}

public class UpgradeSubscriptionRequestDto
{
    public Guid OrganizationId { get; set; }
    public Guid TargetPlanId { get; set; }
}

