export interface OnboardingStageDto {
  code: string;
  title: string;
  status: 'Completed' | 'Current' | 'Pending' | 'Blocked' | 'Optional' | string;
  estimatedMinutes: number;
  ownerHint?: string | null;
  isOptional: boolean;
}

export interface OnboardingTrialSummaryDto {
  licenseKeyId?: string | null;
  licenseType?: string | null;
  licenseStatus?: string | null;
  daysRemaining?: number | null;
  expiresAtUtc?: string | null;
  maxSeats: number;
  usedSeats: number;
  entitlements: string[];
  isOperational: boolean;
}

export interface OnboardingQuickActionDto {
  code: string;
  label: string;
  href?: string | null;
}

export interface OnboardingJourneyDto {
  organizationId: string;
  organizationName: string;
  hospitalId?: string | null;
  hospitalName?: string | null;
  isOnboarded: boolean;
  role: string;
  administratorName?: string | null;
  administratorEmail?: string | null;
  emailVerified: boolean;
  progressPercent: number;
  currentStageCode?: string | null;
  nextAction?: string | null;
  estimatedMinutesRemaining: number;
  stages: OnboardingStageDto[];
  trial: OnboardingTrialSummaryDto;
  quickActions: OnboardingQuickActionDto[];
}
