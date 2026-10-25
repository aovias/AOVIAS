namespace AccessGovernanceService.Domain.Aggregates;

public class Permission
{
    public Guid Id { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Module { get; private set; } = string.Empty;
    public string Feature { get; private set; } = string.Empty;
    public string Action { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public Guid? PermissionGroupValueId { get; private set; }
    public Guid? ResourceTypeValueId { get; private set; }
    public Guid? ActionTypeValueId { get; private set; }
    public string? FieldMask { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public byte[] RowVersion { get; private set; } = Array.Empty<byte>();

    private Permission() { }

    public static Permission Create(string code, string module, string feature, string action, string description, Guid? permissionGroupValueId, Guid? resourceTypeValueId, Guid? actionTypeValueId, string? fieldMask = null)
    {
        return new Permission
        {
            Id = Guid.NewGuid(),
            Code = code.Trim(),
            Module = module.Trim(),
            Feature = feature.Trim(),
            Action = action.Trim(),
            Description = description.Trim(),
            PermissionGroupValueId = permissionGroupValueId,
            ResourceTypeValueId = resourceTypeValueId,
            ActionTypeValueId = actionTypeValueId,
            FieldMask = fieldMask,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void SoftDelete() => IsDeleted = true;
}
