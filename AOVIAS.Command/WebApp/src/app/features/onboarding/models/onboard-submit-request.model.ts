import { AssessmentAnswer, HospitalProfileData, TeamMember } from './onboarding-state.model';

/**
 * Body for `POST /onboarding/onboard` (AI Setup Assistant completion).
 * Backend should accept this JSON shape; field names align with gateway conventions (camelCase).
 */
export interface OnboardSubmitRequest {
  organizationId: string;
  /** Active / selected hospital from wizard (matches `HospitalProfileData.hospitalId`). */
  hospitalId: string;
  hospitalProfile: HospitalProfileData;
  /** Selected compliance framework codes (`FrameworkLookup.code`). */
  frameworkCodes: string[];
  assessmentAnswers: AssessmentAnswer[];
  /** Non-empty invites after validation; may be empty if user skipped team step. */
  teamInvites: TeamInviteRow[];
  /** Client draft version for forward-compatible migrations. */
  wizardVersion: number;
}

export interface TeamInviteRow {
  name: string;
  email: string;
  role: string;
  departmentId: string;
}
