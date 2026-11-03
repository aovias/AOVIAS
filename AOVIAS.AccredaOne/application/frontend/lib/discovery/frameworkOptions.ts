/** Eligible framework codes from the MVP development synthetic dataset (not official accreditation content). */
export const ELIGIBLE_FRAMEWORK_OPTIONS = [
  { code: "NOAF", label: "NOAF — Northstar Operational Assurance Framework (synthetic)" },
  { code: "MCQF", label: "MCQF — Meridian Care Quality Framework (synthetic)" },
] as const;

export type EligibleFrameworkCode = (typeof ELIGIBLE_FRAMEWORK_OPTIONS)[number]["code"];
