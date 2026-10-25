import type { ControlledDocumentStatus, ControlledDocumentType } from './documentation.enums';

export type AuthoringBlockType =
  | 'heading'
  | 'paragraph'
  | 'bulletList'
  | 'numberedList'
  | 'checklist'
  | 'table'
  | 'callout'
  | 'warning'
  | 'image'
  | 'reference'
  | 'crossRef'
  | 'collapsible'
  | 'pageBreak'
  | 'code'
  | 'nabhNote'
  | 'riskWarning'
  | 'legalRequirement'
  | 'patientSafetyAlert'
  | 'committeeDecision'
  | 'bestPractice'
  | 'clinicalTip'
  | 'clinicalProcedure'
  | 'whoGuideline'
  | 'drugSafety'
  | 'equipmentMaintenance'
  | 'fireSafety'
  | 'biomedicalWaste'
  | 'consent'
  | 'qualityIndicator'
  | 'jciRequirement';

export interface AuthoringBlock {
  id: string;
  type: AuthoringBlockType | string;
  data: Record<string, unknown>;
}

export interface AuthoringSection {
  id: string;
  sectionKey: string;
  title: string;
  sortOrder: number;
  isRequired: boolean;
  isCollapsed: boolean;
  blocks: AuthoringBlock[];
  lastEditedAt: string;
}

export interface DocumentHealthScore {
  completenessPercent: number;
  readabilityScore: number;
  approvalReadinessPercent: number;
  overallHealthScore: number;
  missingSections: string[];
  mappedObjectiveElements: number;
  unresolvedComments: number;
  linkedEvidenceCount: number;
  linkedCapaCount: number;
  linkedFindingCount: number;
}

export interface ComplianceMappingSuggestion {
  framework: string;
  chapter: string;
  standard: string;
  objectiveElement: string;
  objectiveElementId?: string | null;
  confidence: number;
  reason: string;
}

export interface AuthoringComment {
  id: string;
  sectionId?: string | null;
  blockId?: string | null;
  commentType: string;
  content: string;
  authorName?: string | null;
  mentionedUserName?: string | null;
  isResolved: boolean;
  createdAt: string;
}

export interface AuthoringCheckpoint {
  id: string;
  label?: string | null;
  createdByName?: string | null;
  createdAt: string;
}

export interface CrossReference {
  id: string;
  sectionId?: string | null;
  blockId?: string | null;
  referenceType: string;
  targetId: string;
  targetLabel: string;
  targetRoute?: string | null;
}

export interface AuthoringSession {
  documentId: string;
  versionId: string;
  documentNumber: string;
  title: string;
  documentType: ControlledDocumentType;
  status: ControlledDocumentStatus;
  authoringSchemaKey: string;
  department?: string | null;
  mappedFramework?: string | null;
  mappedChapter?: string | null;
  mappedStandard?: string | null;
  mappedObjectiveElement?: string | null;
  mappedObjectiveElementId?: string | null;
  sections: AuthoringSection[];
  health: DocumentHealthScore;
  mappingSuggestions: ComplianceMappingSuggestion[];
  comments: AuthoringComment[];
  crossReferences: CrossReference[];
  checkpoints: AuthoringCheckpoint[];
  lastSavedAt: string;
}

export interface CreateAuthoringRequest {
  title: string;
  documentType: ControlledDocumentType;
  templateId?: string;
  authoringSchemaKey?: string;
  department?: string;
}

export interface PatchAuthoringRequest {
  title?: string;
  department?: string;
  sections?: { id: string; isCollapsed?: boolean; blocks?: AuthoringBlock[] }[];
  createCheckpoint?: boolean;
  checkpointLabel?: string;
  acceptMappingObjectiveElementId?: string;
  manualMapping?: ComplianceMappingSuggestion;
}

export interface AuthoringAiDraftRequest {
  documentId: string;
  sectionId: string;
  action: string;
  instructions?: string;
}

export interface AuthoringAiDraftResponse {
  suggestedBlocks: AuthoringBlock[];
  explanation: string;
  isAdvisoryOnly: boolean;
}

export interface StructuredCompareResult {
  currentVersionId: string;
  previousVersionId?: string | null;
  sectionDiffs: SectionDiff[];
  aiSummary: string;
}

export interface SectionDiff {
  sectionKey: string;
  title: string;
  changeType: string;
  blockDiffs: BlockDiff[];
}

export interface BlockDiff {
  blockId: string;
  changeType: string;
  beforeText?: string | null;
  afterText?: string | null;
}

export type RightPanelTab = 'ai' | 'mapping' | 'properties' | 'comments' | 'health' | 'compare' | 'intelligence';

export const AI_ACTIONS = [
  { id: 'draft-purpose', label: 'Draft Purpose' },
  { id: 'draft-scope', label: 'Draft Scope' },
  { id: 'rewrite', label: 'Rewrite professionally' },
  { id: 'improve-readability', label: 'Improve readability' },
  { id: 'expand-procedure', label: 'Expand Procedure' },
  { id: 'suggest-responsibilities', label: 'Suggest Responsibilities' },
  { id: 'generate-checklist', label: 'Generate Checklist' },
  { id: 'suggest-references', label: 'Suggest References' },
  { id: 'summarize', label: 'Summarize document' },
] as const;

export const HEALTHCARE_BLOCKS = [
  { type: 'clinicalProcedure', label: 'Clinical Procedure' },
  { type: 'patientSafetyAlert', label: 'Patient Safety' },
  { type: 'riskWarning', label: 'Risk Warning' },
  { type: 'nabhNote', label: 'NABH Requirement' },
  { type: 'jciRequirement', label: 'JCI Requirement' },
  { type: 'whoGuideline', label: 'WHO Guideline' },
  { type: 'drugSafety', label: 'Drug Safety' },
  { type: 'equipmentMaintenance', label: 'Equipment Maintenance' },
  { type: 'fireSafety', label: 'Fire Safety' },
  { type: 'biomedicalWaste', label: 'Biomedical Waste' },
  { type: 'consent', label: 'Consent' },
  { type: 'committeeDecision', label: 'Committee Decision' },
  { type: 'qualityIndicator', label: 'Quality Indicator' },
  { type: 'bestPractice', label: 'Best Practice' },
  { type: 'legalRequirement', label: 'Legal Requirement' },
  { type: 'clinicalTip', label: 'Clinical Tip' },
] as const;

export const BASIC_BLOCKS = [
  { type: 'heading', label: 'Heading' },
  { type: 'paragraph', label: 'Paragraph' },
  { type: 'bulletList', label: 'Bullet list' },
  { type: 'numberedList', label: 'Numbered list' },
  { type: 'checklist', label: 'Checklist' },
  { type: 'callout', label: 'Callout' },
  { type: 'warning', label: 'Warning' },
  { type: 'table', label: 'Table' },
] as const;
