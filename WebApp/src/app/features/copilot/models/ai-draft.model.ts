/** Mirrors backend DraftStatus enum (Domain/Copilot/Enums/DraftStatus.cs). */
export enum DraftStatus {
  Draft    = 'Draft',
  Reviewed = 'Reviewed',
  Approved = 'Approved',
  Archived = 'Archived',
}

/** Mirrors backend DraftType enum (Domain/Copilot/Enums/DraftType.cs). Sprint 13C. */
export enum DraftType {
  CapaDraft                    = 0,
  ExecutiveComplianceBriefing  = 1,
  ReadinessSummary             = 2,
  EvidenceChecklist            = 3,
  AuditPreparationBrief        = 4,
  ManagementReviewSummary      = 5,
}

/** Mirrors backend DraftSourceType enum. Sprint 13C. */
export enum DraftSourceType {
  Recommendation = 0,
  Deficiency     = 1,
  Capa           = 2,
  Analytics      = 3,
  Maturity       = 4,
  Manual         = 5,
}

/** Source Intelligence embedded in every generated draft. Sprint 13C. */
export interface DraftSourceIntelligence {
  recommendationIds: string[];
  kpiValues: Record<string, string>;
  deficiencyRefs: string[];
  capaRefs: string[];
  obligationRefs: string[];
  analyticsMetrics: Record<string, string>;
  rationaleObjects: string[];
  templateName: string;
  generatorName: string;
  organizationId: string;
  generatedAtUtc: string;
  schemaVersion: string;
}

/** Mirrors backend AiDraftDto (Sprint 13C enhanced). */
export interface AiDraft {
  id: string;
  /** Groups all versions of the same logical draft. */
  groupId: string;
  organizationId: string;

  // Classification
  draftType: string;
  draftTypeEnum: DraftType;
  sourceType: string;
  sourceEntityId: string | null;
  recommendationId: string | null;
  persona: string;

  // Content
  title: string;
  summary: string;
  content: string;

  // Generation provenance
  generatedBy: string;
  confidence: number;
  metadataJson: string | null;

  // Lifecycle
  status: DraftStatus;
  version: number;
  generatedAtUtc: string;
  createdUtc: string;
  updatedUtc: string;
}

/** Request body for POST /copilot/drafts/generate. Sprint 13C. */
export interface GenerateDraftRequest {
  draftType: DraftType;
  sourceType: DraftSourceType;
  persona: string;
  recommendationId?: string;
  sourceEntityId?: string;
  additionalInstructions?: string;
}

/** Parses the Source Intelligence JSON from a draft's metadataJson field. */
export function parseDraftSourceIntelligence(draft: AiDraft): DraftSourceIntelligence | null {
  if (!draft.metadataJson) return null;
  try {
    return JSON.parse(draft.metadataJson) as DraftSourceIntelligence;
  } catch {
    return null;
  }
}

/** Returns a human-readable label for a DraftType enum value. */
export function getDraftTypeLabel(draftType: DraftType | string): string {
  const map: Record<number, string> = {
    [DraftType.CapaDraft]:                   'CAPA Draft',
    [DraftType.ExecutiveComplianceBriefing]:  'Executive Briefing',
    [DraftType.ReadinessSummary]:             'Readiness Summary',
    [DraftType.EvidenceChecklist]:            'Evidence Checklist',
    [DraftType.AuditPreparationBrief]:        'Audit Brief',
    [DraftType.ManagementReviewSummary]:      'Management Review',
  };
  const num = typeof draftType === 'string' ? parseInt(draftType) : draftType;
  return map[num] ?? 'Draft';
}

/** Maps a recommendation category string to the matching DraftType. */
export function recommendationCategoryToDraftType(category: string): DraftType {
  switch (category) {
    case 'Deficiency':
    case 'Capa':
    case 'Risk':
      return DraftType.CapaDraft;
    case 'Accreditation':
    case 'Verification':
    case 'Department':
      return DraftType.ReadinessSummary;
    case 'Evidence':
      return DraftType.EvidenceChecklist;
    case 'Executive':
    case 'Analytics':
    case 'Escalation':
      return DraftType.ExecutiveComplianceBriefing;
    default:
      return DraftType.ReadinessSummary;
  }
}

/** Returns the action label for a "Generate Draft" button on a recommendation card. */
export function getDraftActionLabel(draftType: DraftType): string {
  switch (draftType) {
    case DraftType.CapaDraft:                   return 'Generate CAPA Draft';
    case DraftType.ExecutiveComplianceBriefing:  return 'Generate Executive Briefing';
    case DraftType.ReadinessSummary:             return 'Generate Readiness Summary';
    case DraftType.EvidenceChecklist:            return 'Generate Evidence Checklist';
    case DraftType.AuditPreparationBrief:        return 'Generate Audit Brief';
    case DraftType.ManagementReviewSummary:      return 'Generate Management Review';
    default:                                     return 'Generate Draft';
  }
}
