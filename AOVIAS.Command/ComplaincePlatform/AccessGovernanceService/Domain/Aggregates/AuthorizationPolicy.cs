using AccessGovernanceService.Domain.Entities;
using AccessGovernanceService.Domain.Enums;

namespace AccessGovernanceService.Domain.Aggregates;

public class AuthorizationPolicy
{
    public Guid Id { get; private set; }
    public string PolicyCode { get; private set; } = string.Empty;
    public string PolicyName { get; private set; } = string.Empty;
    public PolicyStatus Status { get; private set; }
    public int Priority { get; private set; }
    public Guid? PolicyTypeValueId { get; private set; }
    public Guid? OrganizationId { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? PublishedAt { get; private set; }
    public byte[] RowVersion { get; private set; } = Array.Empty<byte>();

    private readonly List<PolicyRule> _rules = new();
    public IReadOnlyCollection<PolicyRule> Rules => _rules.AsReadOnly();

    private AuthorizationPolicy() { }

    public static AuthorizationPolicy Create(string policyCode, string policyName, int priority, Guid? policyTypeValueId, Guid? organizationId)
    {
        return new AuthorizationPolicy
        {
            Id = Guid.NewGuid(),
            PolicyCode = policyCode.Trim(),
            PolicyName = policyName.Trim(),
            Priority = priority,
            PolicyTypeValueId = policyTypeValueId,
            OrganizationId = organizationId,
            Status = PolicyStatus.Draft,
            CreatedAt = DateTime.UtcNow
        };
    }

    public PolicyRule AddRule(PolicyEffect effect, int rulePriority)
    {
        var rule = PolicyRule.Create(Id, effect, rulePriority);
        _rules.Add(rule);
        return rule;
    }

    public void UpdateDraft(string policyName, int priority)
    {
        if (Status != PolicyStatus.Draft)
            throw new InvalidOperationException("Only draft policies can be updated.");
        PolicyName = policyName.Trim();
        Priority = priority;
    }

    public void Publish()
    {
        if (Status != PolicyStatus.Draft)
            throw new InvalidOperationException("Only draft policies can be published.");
        Status = PolicyStatus.Published;
        PublishedAt = DateTime.UtcNow;
    }

    public void Archive()
    {
        Status = PolicyStatus.Archived;
    }

    public void SoftDelete() => IsDeleted = true;
}
