/**
 * Gateway onboarding questionnaire DTOs (`GET /api/v1/onboarding/questionnaire/{frameworkId}`).
 * Response is unwrapped to `data` by `apiResponseInterceptor`.
 */

export interface OnboardingQuestionnaireQuestionDto {
  id: string;
  complianceNodeId: string;
  questionText: string;
  helpText: string;
  category: string;
  weight: number;
}

export interface OnboardingQuestionnaireSectionDto {
  chapterId: string;
  chapterName: string;
  questions: OnboardingQuestionnaireQuestionDto[];
}

export interface FrameworkQuestionnaireDto {
  frameworkId: string;
  frameworkName: string;
  sections: OnboardingQuestionnaireSectionDto[];
}
