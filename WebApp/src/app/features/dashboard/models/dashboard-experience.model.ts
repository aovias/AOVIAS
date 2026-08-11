export enum DashboardLoadingPhase {
  Shell = 0,
  Summary = 1,
  CriticalWidgets = 2,
  OperationalWidgets = 3,
  Analytics = 4,
  Ai = 5,
}

export interface DashboardApplicationInfo {
  applicationId: string;
  displayName: string;
  description?: string | null;
  order: number;
  isEnabled: boolean;
}

export interface DashboardOrganizationInfo {
  organizationId: string;
  name?: string | null;
}

export interface DashboardCurrentContext {
  hospitalId?: string | null;
  hospitalName?: string | null;
  frameworkId?: string | null;
  frameworkName?: string | null;
}

export interface DashboardOverallHealth {
  status: string;
  message?: string | null;
  score?: number | null;
}

export interface DashboardSummaryResult {
  organization: DashboardOrganizationInfo;
  currentContext: DashboardCurrentContext;
  applications: DashboardApplicationInfo[];
  overallHealth: DashboardOverallHealth;
  criticalAlerts: number;
  unreadNotifications: number;
  pendingTasks: number;
  lastUpdated: string;
}

export interface DashboardWidgetDescriptor {
  id: string;
  application: string;
  title: string;
  icon?: string | null;
  priority: number;
  phase: DashboardLoadingPhase;
  permissions: string[];
  cacheTtl?: string | null;
  lazy: boolean;
  critical: boolean;
  sectionEndpoint?: string | null;
  visibility: string;
}

export interface DashboardSectionDescriptor {
  id: string;
  application: string;
  title: string;
  icon?: string | null;
  priority: number;
  phase: DashboardLoadingPhase;
  permissions: string[];
  cacheTtl?: string | null;
  lazy: boolean;
  critical: boolean;
  sectionEndpoint?: string | null;
  visibility: string;
  widgetIds: string[];
}

export interface DashboardExperienceResult {
  summary: DashboardSummaryResult;
  widgets: DashboardWidgetDescriptor[];
  sections: DashboardSectionDescriptor[];
}

export interface DashboardExperienceQueryParams {
  hospitalId?: string | null;
  frameworkId?: string | null;
}

interface ApiResponse<T> {
  success: boolean;
  data: T;
  message?: string;
  errors?: Array<{ message?: string }>;
}

export type DashboardExperienceApiResponse = ApiResponse<DashboardExperienceResult>;
