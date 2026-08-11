namespace Shared.Infrastructure.Auth;

/// <summary>
/// Shared authentication constants used across all services
/// </summary>
public static class AuthConstants
{
    /// <summary>
    /// JWT claim types
    /// </summary>
    public static class Claims
    {
        public const string Scope = "scope";
        public const string OrganizationId = "organizationId";
        public const string PlatformId = "platform_id";
        public const string SessionId = "session_id";
        public const string TokenVersion = "token_version";
        public const string SecurityStamp = "security_stamp";
        public const string MembershipId = "membership_id";
        public const string ActiveDepartmentId = "active_department_id";
        public const string AuthenticationMethod = "authentication_method";
        public const string AiEnabled = "ai_enabled";
        public const string AutomationLevel = "automation_level";
        public const string AssistantScope = "assistant_scope";
        public const string PersonId = "person_id";
        public const string EmploymentId = "employment_id";
        public const string ProfessionalProfileId = "professional_profile_id";
    }

    /// <summary>
    /// Authorization policy names
    /// </summary>
    public static class Policies
    {
        public const string ComplianceScope = "ComplianceScope";
        /// <summary>Gateway onboarding wizard: user may have <c>compliance.api</c> and/or <c>organization.api</c> (see phase 23 auth notes).</summary>
        public const string ComplianceOrOrganizationScope = "ComplianceOrOrganizationScope";
        public const string OrganizationScope = "OrganizationScope";
        public const string AuditScope = "AuditScope";
        public const string AiScope = "AiScope";
        public const string AdminOnly = "AdminOnly";
        /// <summary>CRP Phase 4 — cross-tenant Commercial Administration Console.</summary>
        public const string CommercialAdmin = "CommercialAdmin";
    }

    /// <summary>
    /// API scope values
    /// </summary>
    public static class Scopes
    {
        public const string ComplianceApi = "compliance.api";
        public const string OrganizationApi = "organization.api";
        public const string AuditApi = "audit.api";
        public const string AiApi = "ai.api";
    }

    /// <summary>
    /// Authentication scheme names
    /// </summary>
    public static class Schemes
    {
        public const string JwtBearer = "Bearer";
    }
}
