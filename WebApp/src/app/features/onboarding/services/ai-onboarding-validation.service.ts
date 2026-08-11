import { Injectable } from '@angular/core';
import {
  AssessmentAnswer,
  HospitalProfileData,
  OnboardingState,
  TeamMember,
} from '../models/onboarding-state.model';

export interface StepValidationResult {
  valid: boolean;
  errors: string[];
}

const EMAIL_RE = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

@Injectable({
  providedIn: 'root',
})
export class AiOnboardingValidationService {
  validateHospitalProfile(data: HospitalProfileData): StepValidationResult {
    const errors: string[] = [];
    if (!data.hospitalId?.trim()) {
      errors.push('Select a hospital.');
    }
    if (!data.hospitalType?.trim()) {
      errors.push('Select a hospital type.');
    }
    if (!data.bedCapacity?.trim()) {
      errors.push('Select bed capacity.');
    }
    if (!data.countryCode?.trim() || !data.stateCode?.trim()) {
      errors.push('Select country and state.');
    }
    if (!data.departmentIds?.length) {
      errors.push('Select at least one department.');
    }
    return { valid: errors.length === 0, errors };
  }

  validateFrameworks(codes: string[]): StepValidationResult {
    if (!codes?.length) {
      return { valid: false, errors: ['Select at least one compliance framework.'] };
    }
    return { valid: true, errors: [] };
  }

  validateAssessment(answers: AssessmentAnswer[], expectedCount: number): StepValidationResult {
    if (expectedCount > 0 && answers.length < expectedCount) {
      return { valid: false, errors: ['Answer all assessment questions.'] };
    }
    const unanswered = answers.filter((a) => !a.answer);
    if (unanswered.length > 0) {
      return { valid: false, errors: ['Answer all assessment questions.'] };
    }
    return { valid: true, errors: [] };
  }

  validateTeamMembers(members: TeamMember[], allowedDepartmentIds: Set<string>): StepValidationResult {
    const errors: string[] = [];
    const emails = new Map<string, number>();

    for (const m of members) {
      const any =
        m.name?.trim() || m.email?.trim() || m.role?.trim() || m.departmentId?.trim();
      if (!any) {
        continue;
      }
      if (!m.name?.trim() || !m.email?.trim() || !m.role?.trim() || !m.departmentId?.trim()) {
        errors.push('Complete all fields for each team member row, or remove empty rows.');
        break;
      }
      if (!EMAIL_RE.test(m.email.trim())) {
        errors.push('Enter a valid email address for each team member.');
        break;
      }
      const key = m.email.trim().toLowerCase();
      emails.set(key, (emails.get(key) ?? 0) + 1);
      if (!allowedDepartmentIds.has(m.departmentId.trim())) {
        errors.push('Each team member must be assigned to a department selected in Hospital Profile.');
        break;
      }
    }

    for (const [, count] of emails) {
      if (count > 1) {
        errors.push('Duplicate email addresses are not allowed.');
        break;
      }
    }

    return { valid: errors.length === 0, errors };
  }

  /** True when profile, frameworks, and assessment are complete enough to submit. */
  canSubmitWizard(state: OnboardingState, assessmentQuestionCount: number): StepValidationResult {
    const e: string[] = [];
    const p = this.validateHospitalProfile(state.hospitalProfile);
    if (!p.valid) {
      e.push(...p.errors);
    }
    const f = this.validateFrameworks(state.selectedFrameworks);
    if (!f.valid) {
      e.push(...f.errors);
    }
    const a = this.validateAssessment(state.assessmentAnswers, assessmentQuestionCount);
    if (!a.valid) {
      e.push(...a.errors);
    }
    const deptIds = new Set(state.hospitalProfile.departmentIds ?? []);
    const t = this.validateTeamMembers(state.invitedUsers, deptIds);
    if (!t.valid) {
      e.push(...t.errors);
    }
    return { valid: e.length === 0, errors: e };
  }
}
