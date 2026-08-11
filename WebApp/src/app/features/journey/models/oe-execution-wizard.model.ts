/** Persisted OE execution wizard state (sessionStorage, user + OE scoped). */
export interface OeWizardPersistedState {
  stepIndex: number;
  understandAck: boolean;
  executeReady: boolean;
  /** Phase 50: Evidence step acknowledged (local wizard gate). */
  evidenceAck: boolean;
  evalSubmitted: boolean;
  evalScore: number;
  evalTone: 'compliant' | 'partial' | 'non';
  evalJustification?: string;
  needsImprove: boolean;
  improveReady: boolean;
  completeAck: boolean;
  /** Phase 52: KB audit checklist line checks (length matches split KB `auditChecklist` lines). */
  auditChecklistChecked?: boolean[];
  /** ISO timestamp when last written (merge with server draft in Phase 52 wave 2). */
  savedAt?: string;
}

/** Split KB `auditChecklist` text into non-empty lines (same rules as wizard UI). */
export function splitAuditChecklistLines(text: string): string[] {
  const t = text?.trim() ?? '';
  if (!t) return [];
  return t
    .split(/\n+/)
    .map((s) => s.trim())
    .filter(Boolean);
}

export const OE_WIZARD_STEP_LABELS = [
  'Understanding',
  'Compliance Guidance',
  'Evidence',
  'Audit Checklist',
  'Review & Submit',
] as const;

export type OeWizardEvalTone = 'compliant' | 'partial' | 'non';
