/**
 * Demo catalog — NABH Hospital 6th Edition–style structure.
 * Bulk dimensions support UI performance testing (scroll, expand, filters).
 */

import { OeClassification } from '../models/nabh.enums';

export interface MockOeSeed {
  id: string;
  code: string;
  description: string;
  classification: OeClassification;
  score: 1 | 2 | 3 | 4 | 5;
  /** Owning department for filters / badges (execution workspace). */
  departmentLabel?: string;
}

export interface MockStandardSeed {
  id: string;
  code: string;
  title: string;
  description: string;
  objectiveElements: MockOeSeed[];
}

export interface MockChapterSeed {
  id: string;
  code: string;
  name: string;
  standards: MockStandardSeed[];
}

export interface MockJourneyRoot {
  journeyId: string;
  frameworkCode: string;
  frameworkName: string;
  chapters: MockChapterSeed[];
}

/** Standards per chapter in the generated demo catalog (≥ 20). */
export const BULK_STANDARDS_PER_CHAPTER = 20;
/** Objective elements per standard (≥ 20). */
export const BULK_OES_PER_STANDARD = 20;

const DEPT_LABELS = ['Clinical', 'Pharmacy', 'HR', 'Facility', 'Infection Control'] as const;

const CLASSIFICATION_ROTATION: OeClassification[] = [
  'core',
  'commitment',
  'achievement',
  'excellence',
];

function buildBulkObjectiveElements(
  chapterId: string,
  chapterCode: string,
  standardIndex: number,
): MockOeSeed[] {
  const oes: MockOeSeed[] = [];
  for (let i = 1; i <= BULK_OES_PER_STANDARD; i++) {
    const letter = String.fromCharCode(96 + i);
    const score = (((standardIndex + i) % 5) + 1) as 1 | 2 | 3 | 4 | 5;
    oes.push({
      id: `oe-${chapterId}-s${standardIndex}-oe${i}`,
      code: `${chapterCode}.${standardIndex}.${letter}`,
      description: `Mock objective element ${letter} — standard ${chapterCode}.${standardIndex} (${chapterId}).`,
      classification: CLASSIFICATION_ROTATION[(standardIndex + i) % CLASSIFICATION_ROTATION.length],
      score,
      departmentLabel: DEPT_LABELS[(standardIndex + i) % DEPT_LABELS.length],
    });
  }
  return oes;
}

function buildBulkStandards(ch: Pick<MockChapterSeed, 'id' | 'code' | 'name'>): MockStandardSeed[] {
  const standards: MockStandardSeed[] = [];
  for (let s = 1; s <= BULK_STANDARDS_PER_CHAPTER; s++) {
    standards.push({
      id: `st-${ch.id}-${s}`,
      code: `${ch.code}.${s}`,
      title: `${ch.code}.${s} — ${ch.name} (mock)`,
      description: `Demonstration standard ${s} for chapter ${ch.code}.`,
      objectiveElements: buildBulkObjectiveElements(ch.id, ch.code, s),
    });
  }
  return standards;
}

/** Ten NABH Hospital 6th–style chapter bands (labels aligned with common guidebook structure). */
const CHAPTER_SKELETONS: Pick<MockChapterSeed, 'id' | 'code' | 'name'>[] = [
  {
    id: 'ch-aac',
    code: 'AAC',
    name: 'Access, Assessment and Continuity of Care',
  },
  {
    id: 'ch-cop',
    code: 'COP',
    name: 'Care of Patients',
  },
  {
    id: 'ch-mom',
    code: 'MOM',
    name: 'Management of Medication',
  },
  {
    id: 'ch-pre',
    code: 'PRE',
    name: 'Patient Rights and Education',
  },
  {
    id: 'ch-ipc',
    code: 'IPC',
    name: 'Hospital Infection Control',
  },
  {
    id: 'ch-psq',
    code: 'PSQ',
    name: 'Patient Safety and Quality Improvement',
  },
  {
    id: 'ch-rom',
    code: 'ROM',
    name: 'Responsibilities of Management',
  },
  {
    id: 'ch-fms',
    code: 'FMS',
    name: 'Facility Management and Safety',
  },
  {
    id: 'ch-hrm',
    code: 'HRM',
    name: 'Human Resource Management',
  },
  {
    id: 'ch-ims',
    code: 'IMS',
    name: 'Information Management System',
  },
];

export const MOCK_JOURNEY: MockJourneyRoot = {
  journeyId: 'mock-nabh-hospital-6',
  frameworkCode: 'NABH-HOSP-6',
  frameworkName: 'NABH Hospital Accreditation Standards (6th Edition)',
  chapters: CHAPTER_SKELETONS.map((ch) => ({
    ...ch,
    standards: buildBulkStandards(ch),
  })),
};
