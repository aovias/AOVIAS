export interface OnboardingState {
  currentStep: number;
  /** Draft format version for sessionStorage migration. */
  wizardVersion?: number;
  hospitalProfile: HospitalProfileData;
  /** Selected framework `code` values from GET /frameworks/all-frameworks (`FrameworkLookup.code`). */
  selectedFrameworks: string[];
  /** Display names aligned with `selectedFrameworks` order (for completion summary). */
  selectedFrameworkDisplayNames?: string[];
  assessmentAnswers: AssessmentAnswer[];
  invitedUsers: TeamMember[];
  completed: boolean;
}

export interface HospitalProfileData {
  hospitalId: string;
  hospitalType: string;
  bedCapacity: string;
  countryCode: string;
  stateCode: string;
  departmentIds: string[];
}

export interface AssessmentAnswer {
  questionId: string;
  question: string;
  answer: 'compliant' | 'partially_compliant' | 'non_compliant' | 'not_applicable';
  /** OE Code (e.g. "AAC.1.a") parsed from the question title prefix. Used by ComplianceService to bridge the KB→CS GUID boundary. */
  objectiveElementCode?: string;
}

export interface TeamMember {
  name: string;
  email: string;
  /** Canonical role id (e.g. `ORG_ADMIN`) for APIs — use `RoleRegistryService.labelOf` in UI. */
  role: string;
  /** `DepartmentLookup.id` from Hospital Profile (Step 1) selections only. */
  departmentId: string;
}

export interface AssessmentQuestion {
  id: string;
  question: string;
  category: string;
  helpText?: string;
  weight?: number;
  isMandatory?: boolean;
  isCritical?: boolean;
  /** OE Code parsed from questionTitle prefix (e.g. "AAC.1.a: Title" → "AAC.1.a"). */
  objectiveElementCode?: string;
}
