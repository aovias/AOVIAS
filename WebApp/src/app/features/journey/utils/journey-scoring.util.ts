import { JourneyChapter } from '../models/chapter.model';
import { JourneyWorkspaceSnapshot } from '../models/journey-tree.model';
import { ComplianceStandard } from '../models/standard.model';
import { ObjectiveElement } from '../models/objective-element.model';
import { NabhAssessmentPhase, OeClassification } from '../models/nabh.enums';

export function flattenActiveOes(
  snapshot: JourneyWorkspaceSnapshot,
): { oe: ObjectiveElement; chapter: JourneyChapter; standard: ComplianceStandard }[] {
  const out: { oe: ObjectiveElement; chapter: JourneyChapter; standard: ComplianceStandard }[] = [];
  for (const ch of snapshot.chapters) {
    for (const st of ch.standards) {
      for (const oe of st.objectiveElements) {
        if (!oe.isNa) out.push({ oe, chapter: ch, standard: st });
      }
    }
  }
  return out;
}

export function isOeInAssessmentScope(oe: ObjectiveElement, phase: NabhAssessmentPhase): boolean {
  if (oe.isNa) return false;
  const c = oe.classification as OeClassification;
  if (phase === 'final') return c === 'core' || c === 'commitment';
  if (phase === 'surveillance') {
    return c === 'core' || c === 'commitment' || c === 'achievement';
  }
  return c === 'core' || c === 'commitment' || c === 'achievement' || c === 'excellence';
}

/** NABH cumulative compliance: sum(scores) / (5 × n) × 100 — PDF scoring. */
export function getCumulativeCompliancePercent(oes: ObjectiveElement[]): number {
  if (oes.length === 0) return 0;
  const sum = oes.reduce((a, o) => a + o.score, 0);
  return (sum / (5 * oes.length)) * 100;
}

export function filterByClassification(
  oes: ObjectiveElement[],
  classifications: OeClassification[],
): ObjectiveElement[] {
  const set = new Set(classifications);
  return oes.filter((o) => set.has(o.classification as OeClassification));
}

export function averageScore(oes: ObjectiveElement[]): number {
  if (oes.length === 0) return 0;
  return oes.reduce((a, o) => a + o.score, 0) / oes.length;
}

export function getStandardScore(standard: ComplianceStandard): number {
  const active = standard.objectiveElements.filter((o) => !o.isNa);
  return averageScore(active);
}

export function getChapterScore(chapter: JourneyChapter): number {
  const oes: ObjectiveElement[] = [];
  for (const st of chapter.standards) {
    for (const oe of st.objectiveElements) {
      if (!oe.isNa) oes.push(oe);
    }
  }
  return averageScore(oes);
}

export function getCoreCompliancePercent(snapshot: JourneyWorkspaceSnapshot): number {
  const core: ObjectiveElement[] = [];
  for (const ch of snapshot.chapters) {
    for (const st of ch.standards) {
      for (const oe of st.objectiveElements) {
        if (!oe.isNa && oe.classification === 'core') core.push(oe);
      }
    }
  }
  if (core.length === 0) return 100;
  const met = core.filter((o) => o.score >= 4).length;
  return (met / core.length) * 100;
}
