/**
 * Platform-wide compliance item classification (Sprint 1 — H8).
 * Mirrors ComplianceItemClassification on the backend.
 */
export type ComplianceItemClassification =
  | 'auditFinding'
  | 'complianceDeficiency'
  | 'predictedGap';

export const COMPLIANCE_CLASSIFICATION_LABEL: Record<ComplianceItemClassification, string> = {
  auditFinding: 'Audit Finding',
  complianceDeficiency: 'Compliance Deficiency',
  predictedGap: 'Predicted Gap',
};

export function normComplianceClassification(
  raw: string | null | undefined,
): ComplianceItemClassification | null {
  if (!raw) return null;
  const v = raw.toLowerCase();
  if (v === 'auditfinding' || v === 'audit_finding') return 'auditFinding';
  if (v === 'compliancedeficiency' || v === 'compliance_deficiency') return 'complianceDeficiency';
  if (v === 'predictedgap' || v === 'predicted_gap') return 'predictedGap';
  if (v === 'auditfinding') return 'auditFinding';
  return null;
}
