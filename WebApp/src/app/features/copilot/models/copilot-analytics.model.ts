export interface CopilotGovernanceDashboard {
  usage: CopilotUsageSummary;
  recommendations: RecommendationEffectivenessMetrics;
  drafts: DraftEffectivenessMetrics;
  proposals: ProposalConversionMetrics;
  personas: PersonaAdoptionMetrics;
  activity: CopilotActivityAnalytics;
  trust: CopilotTrustMetrics;
  metricsAsOfUtc: string;
}

export interface CopilotUsageSummary {
  workspaceViews: number;
  activeUsers: number;
  personaChanges: number;
  totalActivityEvents: number;
}

export interface RecommendationEffectivenessMetrics {
  totalGenerated: number;
  proposed: number;
  accepted: number;
  dismissed: number;
  expired: number;
  viewed: number;
  withProposalCreated: number;
  withProposalExecuted: number;
  acceptanceRate: number;
  executionRate: number;
  dismissalRate: number;
  averageTimeToActionHours?: number | null;
  byCategory: CategoryMetric[];
  byPersona: CategoryMetric[];
}

export interface DraftEffectivenessMetrics {
  totalDrafts: number;
  activeDrafts: number;
  archivedDrafts: number;
  regeneratedGroups: number;
  withProposalCreated: number;
  utilizationRate: number;
  regenerationRate: number;
  proposalConversionRate: number;
  byDraftType: CategoryMetric[];
}

export interface ProposalConversionMetrics {
  proposed: number;
  viewed: number;
  approved: number;
  rejected: number;
  executed: number;
  expired: number;
  approvalRate: number;
  executionRate: number;
  rejectionRate: number;
  averageApprovalTimeHours?: number | null;
  averageExecutionTimeHours?: number | null;
  byProposalType: CategoryMetric[];
}

export interface PersonaAdoptionMetrics {
  personas: PersonaAdoptionRow[];
  mostUsedPersona?: string | null;
  leastUsedPersona?: string | null;
  highestConversionPersona?: string | null;
  manualOverrides: number;
}

export interface PersonaAdoptionRow {
  persona: string;
  sessionEvents: number;
  recommendationsViewed: number;
  draftsGenerated: number;
  proposalsApproved: number;
  conversionRate: number;
}

export interface CopilotActivityAnalytics {
  dailyEvents: number;
  weeklyEvents: number;
  monthlyEvents: number;
  topActivities: CategoryMetric[];
  dailyTrend: DailyActivityPoint[];
}

export interface CopilotTrustMetrics {
  proposalApprovalRate: number;
  recommendationAcceptanceRate: number;
  draftToProposalRate: number;
  ignoredRecommendations: number;
}

export interface CategoryMetric {
  label: string;
  count: number;
  rate: number;
}

export interface DailyActivityPoint {
  date: string;
  count: number;
}

export interface CopilotTrends {
  snapshots: CopilotEffectivenessSnapshotRow[];
  activityTrend: DailyActivityPoint[];
}

export interface CopilotEffectivenessSnapshotRow {
  snapshotDate: string;
  recommendationsGenerated: number;
  recommendationsAccepted: number;
  draftsGenerated: number;
  proposalsExecuted: number;
  personaSwitches: number;
  activeUsers: number;
}

export interface RecommendationImpactSummary {
  recommendationsWithExecution: number;
  recommendationsWithProposal: number;
  executionConversionRate: number;
  topImpacts: RecommendationImpactRow[];
}

export interface RecommendationImpactRow {
  recommendationId: string;
  title: string;
  category: string;
  persona: string;
  hasProposal: boolean;
  proposalExecuted: boolean;
  resultEntityType?: string | null;
  estimatedImpact?: string | null;
  actualOutcome?: string | null;
}

export type CopilotAnalyticsTab =
  | 'overview'
  | 'recommendations'
  | 'drafts'
  | 'proposals'
  | 'personas'
  | 'trends';
