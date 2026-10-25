export interface IncidentDashboard {
  openIncidents: number;
  criticalIncidents: number;
  nearMisses: number;
  sentinelEvents: number;
  investigationsInProgress: number;
  rcaPending: number;
  repeatedIncidents: number;
  departmentsAtRisk: number;
  averageClosureDays: number;
  openCapas: number;
  patientSafetyScore: number;
  byCategory: IncidentCountByLabel[];
  byDepartment: IncidentCountByLabel[];
  bySeverity: IncidentCountByLabel[];
  trend: IncidentTrendPoint[];
}

export interface IncidentCountByLabel {
  label: string;
  count: number;
}

export interface IncidentTrendPoint {
  period: string;
  count: number;
}

export interface IncidentListItem {
  id: string;
  incidentNumber: string;
  title: string;
  description?: string;
  incidentType: string;
  severity: string;
  priority: string;
  category: string;
  location?: string;
  department: string;
  reporterName?: string;
  isAnonymous: boolean;
  patientInvolved: boolean;
  incidentDateTime: string;
  status: string;
  investigationStatus: string;
  rcaRequired: boolean;
  ownerName?: string;
  findingId?: string;
  deficiencyId?: string;
  /** Legacy — CAPA SSOT is deficiencyId → ComplianceDeficiency. */
  capaId?: string;
  createdDate: string;
  lastUpdated: string;
  attachmentCount: number;
  timelineCount: number;
}

export interface IncidentList {
  items: IncidentListItem[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface IncidentDetail extends IncidentListItem {
  staffInvolvedJson?: string;
  timeline: IncidentTimelineEntry[];
  attachments: IncidentAttachment[];
  relatedDocumentation: IncidentDocumentationLink[];
  investigation?: IncidentInvestigation;
  rootCauseAnalysis?: IncidentRca;
}

export interface IncidentTimelineEntry {
  id: string;
  occurredAt: string;
  eventType: string;
  description: string;
  actorName?: string;
}

export interface IncidentAttachment {
  id: string;
  fileName: string;
  fileUrl: string;
  contentType?: string;
  fileSizeBytes?: number;
  uploadedAt: string;
}

export interface IncidentDocumentationLink {
  documentId: string;
  documentNumber: string;
  title: string;
  documentType: string;
  currentVersion?: string;
  acknowledgementPercent: number;
}

export interface IncidentInvestigation {
  id: string;
  incidentSummary?: string;
  contributingFactorsJson?: string;
  peopleFactorsJson?: string;
  environmentFactorsJson?: string;
  equipmentFactorsJson?: string;
  processFactorsJson?: string;
  communicationFactorsJson?: string;
  timelineJson?: string;
  recommendations?: string;
  reviewersJson?: string;
  status: string;
  approvedAt?: string;
}

export interface FiveWhys {
  why1?: string;
  why2?: string;
  why3?: string;
  why4?: string;
  why5?: string;
  finalRootCause?: string;
}

export interface CauseCategories {
  human: string[];
  process: string[];
  environmental: string[];
  equipment: string[];
  organization: string[];
}

export interface FishboneCategory {
  name: string;
  causes: string[];
}

export interface FishboneSchema {
  effect?: string;
  categories: FishboneCategory[];
}

export interface IncidentRca {
  id: string;
  method: string;
  fiveWhys?: FiveWhys;
  causeCategories?: CauseCategories;
  fishbone?: FishboneSchema;
  rootCauseSummary?: string;
  correctiveActionSummary?: string;
  preventiveActionSummary?: string;
  rootCauseCategory?: string;
  /** Legacy persisted JSON — prefer typed fiveWhys. */
  fiveWhysJson?: string;
  fishboneJson?: string;
  humanFactorsJson?: string;
  processFactorsJson?: string;
  environmentalFactorsJson?: string;
  equipmentFactorsJson?: string;
  organizationFactorsJson?: string;
}

export interface SaveRcaRequest {
  incidentId: string;
  method: string;
  fiveWhys?: FiveWhys;
  causeCategories?: CauseCategories;
  fishbone?: FishboneSchema;
  rootCauseSummary?: string;
  correctiveActionSummary?: string;
  preventiveActionSummary?: string;
  rootCauseCategory?: string;
}

export interface IncidentQuery {
  status?: string;
  severity?: string;
  category?: string;
  department?: string;
  incidentType?: string;
  quickFilter?: string;
  search?: string;
  sortBy?: string;
  sortDesc?: boolean;
  page?: number;
  pageSize?: number;
}

export interface CreateIncidentRequest {
  title: string;
  description: string;
  incidentType?: string;
  severity?: string;
  priority?: string;
  category?: string;
  location?: string;
  department?: string;
  isAnonymous?: boolean;
  patientInvolved?: boolean;
  staffInvolvedJson?: string;
  incidentDateTime?: string;
  rcaRequired?: boolean;
}

export interface PatientFeedback {
  id: string;
  feedbackNumber: string;
  feedbackType: string;
  title: string;
  description: string;
  patientName?: string;
  department?: string;
  linkedIncidentId?: string;
  status: string;
  receivedAt: string;
}

export interface LessonLearned {
  id: string;
  lessonNumber: string;
  summary: string;
  recommendation: string;
  departmentsJson?: string;
  relatedSopsJson?: string;
  trainingNeeded?: string;
  preventiveActions?: string;
  sourceIncidentId: string;
  sourceIncidentNumber?: string;
  publishedAt: string;
}

export interface IncidentAnalytics {
  incidentTrends: IncidentTrendPoint[];
  severityDistribution: IncidentCountByLabel[];
  departmentHeatmap: IncidentCountByLabel[];
  rootCauseTrends: IncidentCountByLabel[];
  nearMissTrends: IncidentTrendPoint[];
  repeatIncidents: number;
  averageResolutionDays: number;
  lessonsLearnedCount: number;
  capaConversionRate: number;
  documentationRevisionRate: number;
}

export const INCIDENT_CATEGORIES = [
  { value: 'PatientFall', label: 'Patient Fall' },
  { value: 'MedicationError', label: 'Medication Error' },
  { value: 'HospitalAcquiredInfection', label: 'Hospital Acquired Infection' },
  { value: 'NeedleStickInjury', label: 'Needle Stick Injury' },
  { value: 'EquipmentFailure', label: 'Equipment Failure' },
  { value: 'FireSafety', label: 'Fire Safety' },
  { value: 'BiomedicalWaste', label: 'Biomedical Waste' },
  { value: 'Security', label: 'Security' },
  { value: 'Violence', label: 'Violence' },
  { value: 'PatientIdentification', label: 'Patient Identification' },
  { value: 'Consent', label: 'Consent' },
  { value: 'BloodTransfusion', label: 'Blood Transfusion' },
  { value: 'Laboratory', label: 'Laboratory' },
  { value: 'Radiology', label: 'Radiology' },
  { value: 'ClinicalCare', label: 'Clinical Care' },
  { value: 'NearMiss', label: 'Near Miss' },
  { value: 'SentinelEvent', label: 'Sentinel Event' },
  { value: 'Other', label: 'Other' },
] as const;

export const ROOT_CAUSE_CATEGORIES = [
  { value: 'PolicyMissing', label: 'Policy Missing' },
  { value: 'EvidenceMissing', label: 'Evidence Missing' },
  { value: 'TrainingMissing', label: 'Training Missing' },
  { value: 'ProcessFailure', label: 'Process Failure' },
  { value: 'DocumentationIssue', label: 'Documentation Issue' },
  { value: 'VerificationFailure', label: 'Verification Failure' },
  { value: 'Other', label: 'Other' },
] as const;

export const INCIDENT_QUICK_FILTERS = [
  { key: 'open', label: 'Open' },
  { key: 'critical', label: 'Critical' },
  { key: 'nearMiss', label: 'Near Misses' },
  { key: 'sentinel', label: 'Sentinel Events' },
  { key: 'rcaPending', label: 'RCA Pending' },
] as const;

export const FEEDBACK_TYPES = [
  { value: 'Compliment', label: 'Compliment' },
  { value: 'Complaint', label: 'Complaint' },
  { value: 'Suggestion', label: 'Suggestion' },
  { value: 'Grievance', label: 'Grievance' },
  { value: 'Feedback', label: 'Feedback' },
  { value: 'PatientSafetyConcern', label: 'Patient Safety Concern' },
] as const;

export function formatIncidentLabel(value: string): string {
  return value.replace(/([A-Z])/g, ' $1').trim();
}

export function emptyCauseCategories(): CauseCategories {
  return { human: [], process: [], environmental: [], equipment: [], organization: [] };
}

export function emptyFishbone(effect = ''): FishboneSchema {
  return {
    effect,
    categories: [
      { name: 'People', causes: [] },
      { name: 'Process', causes: [] },
      { name: 'Equipment', causes: [] },
      { name: 'Environment', causes: [] },
      { name: 'Materials', causes: [] },
      { name: 'Management', causes: [] },
    ],
  };
}
