import { AssessmentAnswer } from './onboarding-state.model';

/** Data for Review & Submit (completion) step. */
export interface CompletionStepSummary {
  frameworksCount: number;
  departmentsCount: number;
  initialScore: number;
  frameworks: string[];
  hospitalName: string;
  hospitalType: string;
  bedCapacity: string;
  geoLabel: string;
  departmentNames: string[];
  assessmentAnswers: AssessmentAnswer[];
  /** `role` is a display label for the review table (not the canonical id). */
  teamRows: { name: string; email: string; role: string; departmentLabel: string }[];
}
