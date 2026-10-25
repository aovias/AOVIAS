/** Mirrors backend RecommendationStatus enum. */
export enum RecommendationStatus {
  Proposed  = 'Proposed',
  Accepted  = 'Accepted',
  Dismissed = 'Dismissed',
  Expired   = 'Expired',
}

/** Mirrors backend RecommendationCategory enum (Sprint 13B). */
export enum RecommendationCategory {
  Accreditation = 'Accreditation',
  Deficiency    = 'Deficiency',
  CAPA          = 'CAPA',
  SLA           = 'SLA',
  Escalation    = 'Escalation',
  Evidence      = 'Evidence',
  Verification  = 'Verification',
  Task          = 'Task',
  Executive     = 'Executive',
  Department    = 'Department',
  Risk          = 'Risk',
  Analytics     = 'Analytics',
}

/** Mirrors backend RecommendationSeverity enum (Sprint 13B). */
export enum RecommendationSeverity {
  Low      = 'Low',
  Medium   = 'Medium',
  High     = 'High',
  Critical = 'Critical',
}

/** Mirrors backend RecommendationRationale — machine-readable evidence for each recommendation. */
export interface RecommendationRationale {
  ruleName: string;
  metrics: Record<string, string>;
  thresholds: Record<string, string>;
  sourceEntities: Array<{ entityType: string; entityId: string; entityRef: string }>;
  dataAsOfUtc: string;
  scopeRef: string;
}

/** Mirrors backend CopilotRecommendationDto (Sprint 13B extended). */
export interface CopilotRecommendation {
  id: string;
  organizationId: string;
  recommendationType: string;

  // Sprint 13B taxonomy
  category: RecommendationCategory;
  severity: RecommendationSeverity;
  confidence: number;

  persona: string;
  priority: number;
  title: string;
  description: string;
  rationale: string;
  expectedOutcome: string;
  riskIfIgnored: string;
  sourceReferences: string | null;

  // Sprint 13B traceability
  sourceEntityType: string | null;
  sourceEntityId: string | null;
  deepLink: string | null;
  rationaleJson: string | null;

  // Sprint 13C: impact + draft integration
  impactJson: string | null;
  suggestedDraftAction: string | null;

  status: RecommendationStatus;
  generatedAtUtc: string;
  expiresAtUtc: string | null;
  acceptedAtUtc: string | null;
  dismissedAtUtc: string | null;
}

/** Helper: parse RationaleJson safely. */
export function parseRationale(rec: CopilotRecommendation): RecommendationRationale | null {
  if (!rec.rationaleJson) return null;
  try { return JSON.parse(rec.rationaleJson) as RecommendationRationale; }
  catch { return null; }
}

/** Severity order for sorting (Critical = highest). */
export const SEVERITY_ORDER: Record<RecommendationSeverity, number> = {
  [RecommendationSeverity.Critical]: 4,
  [RecommendationSeverity.High]:     3,
  [RecommendationSeverity.Medium]:   2,
  [RecommendationSeverity.Low]:      1,
};
