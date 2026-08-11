export interface WorkingHoursModel {
  start: string;
  end: string;
}

export interface OrganizationSettingsModel {
  organizationId: string;
  organizationName: string;
  organizationCode: string;
  logoUrl: string | null;
  timeZoneId: string;
  languageCode: string;
  workingDays: string[];
  workingHours: WorkingHoursModel;
  canEdit: boolean;
  rowVersion: string | null;
}

export interface UpdateOrganizationSettingsPayload {
  organizationName: string;
  organizationCode: string;
  timeZoneId: string;
  languageCode: string;
  workingDays: string[];
  workingHours: WorkingHoursModel;
  rowVersion: string | null;
}

export const WEEKDAYS = [
  'Monday',
  'Tuesday',
  'Wednesday',
  'Thursday',
  'Friday',
  'Saturday',
  'Sunday',
] as const;

export const SUPPORTED_LANGUAGES: { code: string; label: string }[] = [
  { code: 'en', label: 'English' },
];

export const DEFAULT_TIME_ZONE = 'Asia/Kolkata';
