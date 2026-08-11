/** NABH 1–5 maturity / compliance score for an Objective Element */
export type NabhScore = 1 | 2 | 3 | 4 | 5;

export type ScoreLabel =
  | 'Poor'
  | 'Limited'
  | 'Partial'
  | 'Substantial'
  | 'Full Compliance';

/** OE classification (NABH-style bands) */
export type OeClassification = 'core' | 'commitment' | 'achievement' | 'excellence';

export type EvidenceStatus = 'complete' | 'missing' | 'pending_verification';

/** Aligns with backend `OperationalMode` (integer 0–4 in API payloads). */
export enum OperationalMode {
  SelfAssessment = 0,
  InternalAudit = 1,
  FinalAudit = 2,
  Surveillance = 3,
  ReAccreditation = 4,
}

/** Audit outcome on an OE (not N/A) */
export type AuditComplianceStatus = 'compliant' | 'non_compliant' | 'not_applicable';

export type ActionPlanStatus = 'open' | 'in_progress' | 'closed';

export type ChapterRiskLevel = 'low' | 'medium' | 'high';

export type OeHistoryEventType = 'audit' | 'evidence' | 'task' | 'action_plan';

/** Maps to NABH assessment types (hospital 6th ed.) for eligibility rules. */
export type NabhAssessmentPhase = 'final' | 'surveillance' | 're_accreditation';

/** Numeric classification bands (PDF: Core, Commitment, Achievement, Excellence). */
export enum OeClassificationNumeric {
  Core = 1,
  Commitment = 2,
  Achievement = 3,
  Excellence = 4,
}
