import type { AuditTypeCode } from '../../../core/configuration/configuration-catalog';

/** The wire models name the configured set rather than restating it. */
export type AuditType = AuditTypeCode;

export type AuditSessionStatus =
  | 'Planned'
  | 'InProgress'
  | 'Review'
  | 'DraftReport'
  | 'Submitted'
  | 'Closed';

export type AuditSessionResult =
  | 'NotDetermined'
  | 'Compliant'
  | 'ConditionallyCompliant'
  | 'NonCompliant'
  | 'PartiallyCompliant';

export type ObservationType =
  | 'PositivePractice'
  | 'Suggestion'
  | 'MinorObservation'
  | 'Strength'
  | 'Opportunity'
  | 'GeneralComment';

export type AuditCommentType =
  | 'GeneralComment'
  | 'ClarificationRequest'
  | 'Recommendation'
  | 'PositiveObservation'
  | 'Question';

export interface AuditorDashboard {
  activeSessions: number;
  areasReviewed: number;
  pendingReviews: number;
  findingsRaised: number;
  observations: number;
  criticalRisks: number;
  coveragePercent: number;
  evidenceReviewed: number;
  readinessSnapshot: number;
  recentSessions: AuditorSessionSummary[];
}

export interface AuditorSessionSummary {
  id: string;
  sessionNumber: string;
  auditType: AuditType;
  status: AuditSessionStatus;
  overallResult: AuditSessionResult;
  scope: string;
  leadAuditorName?: string;
  plannedStartDate?: string;
  plannedEndDate?: string;
  findingsCount: number;
  observationsCount: number;
  createdAt: string;
  /**
   * Scheduled audit this session was started from, when it came from planning.
   * Lets planning resume an already-started audit instead of creating a second
   * session for the same scheduled record.
   */
  scheduledAuditId?: string | null;
}

export interface AuditSessionDetail extends AuditorSessionSummary {
  hospitalId?: string;
  frameworkId?: string;
  leadAuditorUserId?: string;
  auditorTeam: AuditorTeamMember[];
  actualStartDate?: string;
  actualEndDate?: string;
  summary?: string;
  observations: AuditObservationItem[];
  comments: AuditCommentItem[];
  findings: AuditorFindingSummary[];
}

export interface AuditorTeamMember {
  userId?: string;
  name: string;
  role?: string;
}

export interface AuditObservationItem {
  id: string;
  observationNumber: string;
  type: ObservationType;
  title: string;
  description: string;
  objectiveElementId?: string;
  chapterId?: string;
  department?: string;
  createdByName?: string;
  createdAt: string;
}

export interface AuditCommentItem {
  id: string;
  type: AuditCommentType;
  content: string;
  authorUserId: string;
  authorName?: string;
  isAuditorAuthor: boolean;
  parentCommentId?: string;
  createdAt: string;
  replies: AuditCommentItem[];
}

export interface AuditorFindingSummary {
  id: string;
  findingNumber: string;
  title: string;
  severity: string;
  status: string;
  objectiveElementId?: string;
  promotedDeficiencyId?: string;
  dateRaised: string;
}

export interface EvidenceTraceability {
  objectiveElementId: string;
  objectiveElementCode: string;
  objectiveElementDescription: string;
  timeline: TraceabilityTimelineEntry[];
  currentReadinessScore?: number;
}

export interface TraceabilityTimelineEntry {
  layer: string;
  label: string;
  status: string;
  timestamp?: string;
  entityId?: string;
  detail?: string;
}

export interface AuditorSessionReport {
  sessionId: string;
  sessionNumber: string;
  title: string;
  generatedAt: string;
  summary: string;
  strengths: string[];
  observations: AuditObservationItem[];
  findings: AuditorFindingSummary[];
  riskSummary: string;
  recommendations: string[];
  evidenceGaps: string[];
  readinessSummary: number;
  markdownContent: string;
}

export interface AuditorAssessmentContext {
  frameworkId?: string;
  frameworkName?: string;
  chapters: AuditorChapterContext[];
}

export interface AuditorChapterContext {
  id: string;
  code: string;
  name: string;
  standards: AuditorStandardContext[];
}

export interface AuditorStandardContext {
  id: string;
  code: string;
  name: string;
  objectiveElements: AuditorObjectiveElementContext[];
}

export interface AuditorObjectiveElementContext {
  id: string;
  code: string;
  description: string;
  evidenceCount: number;
  verifiedEvidenceCount: number;
  deficiencyCount: number;
  openCapaCount: number;
  taskCount: number;
  readinessScore?: number;
}

export interface CreateAuditSessionRequest {
  auditType: AuditType;
  frameworkId?: string;
  scope: string;
  leadAuditorName?: string;
  auditorTeam?: AuditorTeamMember[];
  plannedStartDate?: string;
  plannedEndDate?: string;
  summary?: string;
  /** Links execution session to a scheduled audit (planning → execution). */
  scheduledAuditId?: string;
}

export interface CreateAuditObservationRequest {
  auditSessionId: string;
  type: ObservationType;
  title: string;
  description: string;
  objectiveElementId?: string;
  chapterId?: string;
  department?: string;
}

export interface CreateAuditCommentRequest {
  auditSessionId: string;
  type: AuditCommentType;
  content: string;
  parentCommentId?: string;
}
