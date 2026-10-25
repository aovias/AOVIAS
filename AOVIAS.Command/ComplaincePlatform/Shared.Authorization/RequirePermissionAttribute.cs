using Microsoft.AspNetCore.Authorization;

namespace Shared.Authorization;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class RequirePermissionAttribute : AuthorizeAttribute
{
    public RequirePermissionAttribute(string permissionCode)
    {
        Policy = PermissionPolicyRegistry.BuildPolicyName(permissionCode);
    }
}

public static class PermissionPolicyRegistry
{
    private const string Prefix = "Permission:";

    public static string BuildPolicyName(string permissionCode) => Prefix + permissionCode;

    public static bool TryParsePolicyName(string policyName, out string permissionCode)
    {
        if (policyName.StartsWith(Prefix, StringComparison.Ordinal))
        {
            permissionCode = policyName[Prefix.Length..];
            return true;
        }
        permissionCode = string.Empty;
        return false;
    }
}
