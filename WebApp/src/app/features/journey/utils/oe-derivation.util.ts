import { OeClassification, OeClassificationNumeric } from '../models/nabh.enums';
import { ObjectiveElement } from '../models/objective-element.model';

const DOC_KEYWORDS = [
  'policy',
  'record',
  'document',
  'procedure',
  'log',
  'register',
  'report',
] as const;

const TAG_PATTERNS: { tag: string; test: (t: string) => boolean }[] = [
  { tag: 'patient safety', test: (t) => t.includes('patient safety') },
  { tag: 'infection control', test: (t) => t.includes('infection') },
  { tag: 'audit', test: (t) => t.includes('audit') },
  { tag: 'training', test: (t) => t.includes('training') },
  { tag: 'documentation', test: (t) => t.includes('documentation') },
];

export interface DerivedOeColumns {
  isMandatory: boolean;
  isScorable: boolean;
  isApplicable: boolean;
  isDocumentationRequired: boolean;
  tags: string[];
  departmentHint: string;
  responsibleRoleHint: string;
  frequencyHint: string;
  evidenceCategoryHint: string;
  classificationWeight: number;
}

export function classificationToNumeric(c: OeClassification): OeClassificationNumeric {
  const map: Record<OeClassification, OeClassificationNumeric> = {
    core: OeClassificationNumeric.Core,
    commitment: OeClassificationNumeric.Commitment,
    achievement: OeClassificationNumeric.Achievement,
    excellence: OeClassificationNumeric.Excellence,
  };
  return map[c] ?? OeClassificationNumeric.Core;
}

export function getClassificationWeight(c: OeClassification): number {
  return classificationToNumeric(c) - 1;
}

export function isDocumentationRequiredText(text: string): boolean {
  const t = text.toLowerCase();
  return DOC_KEYWORDS.some((k) => t.includes(k));
}

export function extractTags(text: string): string[] {
  const t = text.toLowerCase();
  return TAG_PATTERNS.filter((p) => p.test(t)).map((p) => p.tag);
}

export function inferDepartment(text: string): string {
  const t = text.toLowerCase();
  if (t.includes('infection')) return 'Infection Control';
  if (t.includes('medication') || t.includes('pharmacy')) return 'Pharmacy';
  if (t.includes('radiology') || t.includes('imaging')) return 'Radiology';
  if (t.includes('laboratory') || t.includes('lab ')) return 'Laboratory';
  if (t.includes('emergency')) return 'Emergency';
  return 'Quality / Multi-department';
}

export function inferResponsibleRole(text: string): string {
  const t = text.toLowerCase();
  if (t.includes('infection')) return 'Infection Control Nurse';
  if (t.includes('clinical') || t.includes('nursing') || t.includes('doctor')) return 'Doctor / Nurse';
  if (t.includes('admin') || t.includes('governance')) return 'Administrator';
  return 'Quality Manager';
}

export function inferFrequency(text: string): string {
  const t = text.toLowerCase();
  if (t.includes('monthly')) return 'Monthly';
  if (t.includes('each patient') || t.includes('per patient')) return 'Per Patient';
  if (t.includes('annual')) return 'Annual';
  return 'As Applicable';
}

export function inferEvidenceCategory(text: string): string {
  const t = text.toLowerCase();
  if (t.includes('policy') || t.includes('protocol')) return 'Document';
  if (t.includes('log') || t.includes('register')) return 'Record';
  if (t.includes('audit')) return 'Audit';
  if (t.includes('training')) return 'Training Evidence';
  return 'General Evidence';
}

export function deriveOeColumns(oe: ObjectiveElement): DerivedOeColumns {
  const text = `${oe.description} ${oe.code}`;
  const c = oe.classification as OeClassification;
  return {
    isMandatory: c === 'core',
    isScorable: true,
    isApplicable: !oe.isNa,
    isDocumentationRequired: isDocumentationRequiredText(text),
    tags: extractTags(text),
    departmentHint: inferDepartment(text),
    responsibleRoleHint: inferResponsibleRole(text),
    frequencyHint: inferFrequency(text),
    evidenceCategoryHint: inferEvidenceCategory(text),
    classificationWeight: getClassificationWeight(c),
  };
}
