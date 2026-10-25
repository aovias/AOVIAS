import { OeWizardPersistedState } from '../models/oe-execution-wizard.model';

/** Fixed key order — must match `OeWizardPayloadCanonicalizer` in ComplianceService. */
const KEY_ORDER = [
  'auditChecklistChecked',
  'completeAck',
  'evalJustification',
  'evalScore',
  'evalSubmitted',
  'evalTone',
  'evidenceAck',
  'executeReady',
  'improveReady',
  'needsImprove',
  'savedAt',
  'stepIndex',
  'understandAck',
] as const satisfies readonly (keyof OeWizardPersistedState)[];

/**
 * Stable JSON string for comparing payloads (no deep object compare).
 */
export function canonicalizeOeWizardPayload(s: OeWizardPersistedState): string {
  const o: Record<string, unknown> = {};
  for (const k of KEY_ORDER) {
    o[k] = s[k];
  }
  return JSON.stringify(o);
}
