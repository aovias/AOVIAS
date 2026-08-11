using AccessGovernanceService.Domain.Enums;

namespace AccessGovernanceService.Domain.Entities;

public class RoleAssignment
{
    public Guid Id { get; private set; }
    public Guid RoleDefinitionId { get; private set; }
    public Guid UserId { get; private set; }
    public Guid? OrganizationId { get; private set; }
    public Guid? HospitalId { get; private set; }
    public Guid? DepartmentId { get; private set; }
    public Guid? EmploymentId { get; private set; }
    public DateTime? EffectiveFrom { get; private set; }
    public DateTime? EffectiveTo { get; private set; }
    public bool IsRevoked { get; private set; }
    public Guid AssignedByUserId { get; private set; }
    public DateTime AssignedAt { get; private set; }

    private RoleAssignment() { }

    public static RoleAssignment Create(Guid roleDefinitionId, Guid userId, Guid? organizationId, Guid? hospitalId, Guid? departmentId, Guid? employmentId, DateTime? effectiveFrom, DateTime? effectiveTo, Guid assignedByUserId)
    {
        return new RoleAssignment
        {
            Id = Guid.NewGuid(),
            RoleDefinitionId = roleDefinitionId,
            UserId = userId,
            OrganizationId = organizationId,
            HospitalId = hospitalId,
            DepartmentId = departmentId,
            EmploymentId = employmentId,
            EffectiveFrom = effectiveFrom,
            EffectiveTo = effectiveTo,
            AssignedByUserId = assignedByUserId,
            AssignedAt = DateTime.UtcNow
        };
    }

    public void Revoke() => IsRevoked = true;

    public bool IsActiveAt(DateTime utcNow) =>
        !IsRevoked &&
        (!EffectiveFrom.HasValue || EffectiveFrom <= utcNow) &&
        (!EffectiveTo.HasValue || EffectiveTo > utcNow);
}

public class PermissionAssignment
{
    public Guid Id { get; private set; }
    public Guid RoleDefinitionId { get; private set; }
    public Guid PermissionId { get; private set; }
    public Guid GrantedByUserId { get; private set; }
    public DateTime GrantedAt { get; private set; }
    public bool IsRevoked { get; private set; }

    private PermissionAssignment() { }

    public static PermissionAssignment Create(Guid roleDefinitionId, Guid permissionId, Guid grantedByUserId)
    {
        return new PermissionAssignment
        {
            Id = Guid.NewGuid(),
            RoleDefinitionId = roleDefinitionId,
            PermissionId = permissionId,
            GrantedByUserId = grantedByUserId,
            GrantedAt = DateTime.UtcNow
        };
    }

    public void Revoke() => IsRevoked = true;
}

public class PolicyRule
{
    public Guid Id { get; private set; }
    public Guid AuthorizationPolicyId { get; private set; }
    public PolicyEffect Effect { get; private set; }
    public int Priority { get; private set; }

    private readonly List<PolicyCondition> _conditions = new();
    public IReadOnlyCollection<PolicyCondition> Conditions => _conditions.AsReadOnly();

    private PolicyRule() { }

    public static PolicyRule Create(Guid policyId, PolicyEffect effect, int priority)
    {
        return new PolicyRule { Id = Guid.NewGuid(), AuthorizationPolicyId = policyId, Effect = effect, Priority = priority };
    }

    public PolicyCondition AddCondition(string attribute, Guid operatorValueId, string value)
    {
        var c = PolicyCondition.Create(Id, attribute, operatorValueId, value);
        _conditions.Add(c);
        return c;
    }
}

public class PolicyCondition
{
    public Guid Id { get; private set; }
    public Guid PolicyRuleId { get; private set; }
    public string Attribute { get; private set; } = string.Empty;
    public Guid OperatorValueId { get; private set; }
    public string Value { get; private set; } = string.Empty;

    private PolicyCondition() { }

    public static PolicyCondition Create(Guid ruleId, string attribute, Guid operatorValueId, string value)
    {
        return new PolicyCondition { Id = Guid.NewGuid(), PolicyRuleId = ruleId, Attribute = attribute, OperatorValueId = operatorValueId, Value = value };
    }
}

public class DelegationHistory
{
    public Guid Id { get; private set; }
    public Guid DelegationId { get; private set; }
    public string EventType { get; private set; } = string.Empty;
    public Guid ActorUserId { get; private set; }
    public DateTime OccurredAt { get; private set; }

    private DelegationHistory() { }

    public static DelegationHistory Create(Guid delegationId, string eventType, Guid actorUserId)
    {
        return new DelegationHistory { Id = Guid.NewGuid(), DelegationId = delegationId, EventType = eventType, ActorUserId = actorUserId, OccurredAt = DateTime.UtcNow };
    }
}

public class AccessEvaluation
{
    public Guid Id { get; set; }
    public Guid SubjectUserId { get; set; }
    public string Resource { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public bool Allowed { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string DecisionJson { get; set; } = string.Empty;
    public DateTime EvaluatedAt { get; set; }
}

public class AccessAuditLog
{
    public Guid Id { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public string Action { get; set; } = string.Empty;
    public Guid ActorUserId { get; set; }
    public string Details { get; set; } = string.Empty;
    public DateTime OccurredAt { get; set; }
}

public class AccessGovernanceLookupCategory
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public ICollection<AccessGovernanceLookupValue> Values { get; set; } = new List<AccessGovernanceLookupValue>();
}

public class AccessGovernanceLookupValue
{
    public Guid Id { get; set; }
    public Guid CategoryId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public AccessGovernanceLookupCategory? Category { get; set; }
}

public class OutboxMessage
{
    public Guid Id { get; set; }
    public Guid AggregateId { get; set; }
    public DateTime OccurredOn { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public DateTime? ProcessedOn { get; set; }
    public int RetryCount { get; set; }
    public string? Error { get; set; }
}
