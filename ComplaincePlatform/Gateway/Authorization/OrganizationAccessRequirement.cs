using Microsoft.AspNetCore.Authorization;

namespace Gateway.Authorization;

/// <summary>
/// Authorization requirement for organization access control.
/// Ensures users can only access organizations they belong to, unless they are SUPER_ADMIN.
/// This requirement is used by the OrgAccess policy to prevent multi-tenant data breaches.
/// </summary>
public class OrganizationAccessRequirement : IAuthorizationRequirement
{
    // No additional properties needed - the requirement is validated against route parameters
}
