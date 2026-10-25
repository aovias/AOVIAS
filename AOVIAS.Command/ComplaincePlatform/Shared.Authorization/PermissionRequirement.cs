using Microsoft.AspNetCore.Authorization;

namespace Shared.Authorization;

public sealed class PermissionRequirement : IAuthorizationRequirement
{
    public PermissionRequirement(string permissionCode)
    {
        if (string.IsNullOrWhiteSpace(permissionCode))
            throw new ArgumentException("Permission code is required.", nameof(permissionCode));
        PermissionCode = permissionCode.Trim();
    }

    public string PermissionCode { get; }
}
