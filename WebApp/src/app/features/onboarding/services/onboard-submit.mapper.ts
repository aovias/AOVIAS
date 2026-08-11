import { OnboardSubmitRequest, TeamInviteRow } from '../models/onboard-submit-request.model';
import { OnboardingState, TeamMember } from '../models/onboarding-state.model';

const WIZARD_VERSION = 4;

export function mapTeamMembersToInvites(members: TeamMember[]): TeamInviteRow[] {
  return members
    .filter((m) => m.name?.trim() && m.email?.trim() && m.role?.trim() && m.departmentId?.trim())
    .map((m) => ({
      name: m.name.trim(),
      email: m.email.trim().toLowerCase(),
      role: m.role.trim(),
      departmentId: m.departmentId.trim(),
    }));
}

/**
 * Builds the POST /onboarding/onboard body from wizard state and session context.
 */
export function buildOnboardSubmitRequest(
  state: OnboardingState,
  organizationId: string,
): OnboardSubmitRequest | null {
  const orgId = organizationId?.trim();
  const hid = state.hospitalProfile?.hospitalId?.trim();
  if (!orgId || !hid) {
    return null;
  }

  return {
    organizationId: orgId,
    hospitalId: hid,
    hospitalProfile: { ...state.hospitalProfile },
    frameworkCodes: [...state.selectedFrameworks],
    assessmentAnswers: state.assessmentAnswers.map((a) => ({ ...a })),
    teamInvites: mapTeamMembersToInvites(state.invitedUsers),
    wizardVersion: state.wizardVersion ?? WIZARD_VERSION,
  };
}
