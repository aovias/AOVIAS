namespace Shared.Infrastructure.Email;

/// <summary>CRP Phase 0/1 — configurable email verification enforcement.</summary>
public enum EmailVerificationMode
{
    /// <summary>No verification required; accounts are treated as confirmed.</summary>
    None = 0,

    /// <summary>Default commercial SaaS: allow login/signup; send verify email and nudge.</summary>
    Async = 1,

    /// <summary>Block password login until EmailConfirmed.</summary>
    RequiredBeforeLogin = 2,

    /// <summary>Block organization provisioning (Gateway signup) until identity is verified.</summary>
    RequiredBeforeOrganizationProvisioning = 3
}
