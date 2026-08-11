namespace Shared.Infrastructure.Domain;

/// <summary>
/// Lifecycle for <see cref="OrganizationMemberInvitation"/> (CRP Phase 7 workforce onboarding).
/// </summary>
public enum InvitationStatus
{
    Pending = 0,
    Accepted = 1,
    Expired = 2,
    Revoked = 3,
    Cancelled = 4
}
