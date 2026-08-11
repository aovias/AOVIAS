/**
 * NABH Hospital Accreditation Standards 6th Edition (effective 1 Jan 2025) —
 * accreditation decision criteria (PDF pp. 603–678, summary table pp. 686–700).
 */

import { NABH_THRESHOLDS } from '../constants/nabh-thresholds';
import { AccreditationEligibility, JourneyWorkspaceSnapshot } from '../models/journey-tree.model';
import { JourneyChapter } from '../models/chapter.model';
import { ObjectiveElement } from '../models/objective-element.model';
import { NabhAssessmentPhase } from '../models/nabh.enums';
import {
  averageScore,
  flattenActiveOes,
  getCumulativeCompliancePercent,
  isOeInAssessmentScope,
} from './journey-scoring.util';

function mean(nums: number[]): number {
  if (!nums.length) return 0;
  return nums.reduce((a, b) => a + b, 0) / nums.length;
}

function hasAcceptedActionPlan(oe: ObjectiveElement): boolean {
  if (oe.isNa || oe.score > 3) return true;
  return oe.actionPlans.some((p) => p.accepted === true || p.status === 'closed');
}

function coreOes(chapters: JourneyChapter[]): ObjectiveElement[] {
  const out: ObjectiveElement[] = [];
  for (const ch of chapters) {
    for (const st of ch.standards) {
      for (const oe of st.objectiveElements) {
        if (!oe.isNa && oe.classification === 'core') out.push(oe);
      }
    }
  }
  return out;
}

function oesForBand(chapters: JourneyChapter[], band: 'commitment' | 'achievement' | 'excellence'): ObjectiveElement[] {
  const out: ObjectiveElement[] = [];
  for (const ch of chapters) {
    for (const st of ch.standards) {
      for (const oe of st.objectiveElements) {
        if (!oe.isNa && oe.classification === band) out.push(oe);
      }
    }
  }
  return out;
}

function standardLowScoreViolations(
  standards: import('../models/standard.model').ComplianceStandard[],
  maxAllowed: number,
): string[] {
  const v: string[] = [];
  for (const st of standards) {
    const active = st.objectiveElements.filter((o) => !o.isNa);
    const low = active.filter((o) => o.score <= 2).length;
    if (low > maxAllowed) {
      v.push(
        `Standard ${st.code}: ${low} objective element(s) scored ≤2 (max allowed ${maxAllowed}).`,
      );
    }
  }
  return v;
}

/**
 * Evaluate accreditation readiness against NABH 6th-edition rules for the given phase.
 */
export function evaluateAccreditationEligibility(
  snapshot: JourneyWorkspaceSnapshot,
  phase: NabhAssessmentPhase,
  thresholds = NABH_THRESHOLDS,
): AccreditationEligibility {
  const violations: string[] = [];
  const chapters = snapshot.chapters;
  const flat = flattenActiveOes(snapshot);
  const scoped = flat.filter((x) => isOeInAssessmentScope(x.oe, phase)).map((x) => x.oe);

  const overallPct = getCumulativeCompliancePercent(scoped);
  const coreList = coreOes(chapters);
  const coreAvg = averageScore(coreList);

  const standardAvgs: number[] = [];
  const chapterAvgs: number[] = [];

  for (const ch of chapters) {
    for (const st of ch.standards) {
      const stOes = st.objectiveElements.filter((o) => !o.isNa);
      if (stOes.length) {
        const av = averageScore(stOes);
        standardAvgs.push(av);
        if (av < thresholds.minAverageStandard) {
          violations.push(
            `Standard ${st.code}: average score ${av.toFixed(2)} is below ${thresholds.minAverageStandard}.`,
          );
        }
      }
    }
    const chOes: ObjectiveElement[] = [];
    for (const st of ch.standards) {
      chOes.push(...st.objectiveElements.filter((o) => !o.isNa));
    }
    if (chOes.length) {
      const cav = averageScore(chOes);
      chapterAvgs.push(cav);
      if (cav < thresholds.minAverageChapter) {
        violations.push(
          `Chapter ${ch.code}: average score ${cav.toFixed(2)} is below ${thresholds.minAverageChapter}.`,
        );
      }
    }
  }

  const avgStd = mean(standardAvgs);
  const avgCh = mean(chapterAvgs);

  if (overallPct < thresholds.minOverallCompliancePercent) {
    violations.push(
      `Overall compliance ${overallPct.toFixed(1)}% is below ${thresholds.minOverallCompliancePercent}% (scoped OEs for ${phase}).`,
    );
  }

  for (const oe of coreList) {
    if (oe.score < thresholds.minCoreOeAverage) {
      violations.push(`Core OE ${oe.code} scored ${oe.score}; minimum is ${thresholds.minCoreOeAverage}.`);
    }
  }

  const commitmentOes = oesForBand(chapters, 'commitment');
  if (commitmentOes.length) {
    const cPct = getCumulativeCompliancePercent(commitmentOes);
    if (cPct < thresholds.minOverallCompliancePercent) {
      violations.push(
        `Commitment-level cumulative compliance ${cPct.toFixed(1)}% is below ${thresholds.minOverallCompliancePercent}%.`,
      );
    }
  }

  if (phase === 'surveillance') {
    const ach = oesForBand(chapters, 'achievement');
    if (ach.length) {
      const aPct = getCumulativeCompliancePercent(ach);
      if (aPct < thresholds.minOverallCompliancePercent) {
        violations.push(
          `Achievement-level cumulative compliance ${aPct.toFixed(1)}% is below ${thresholds.minOverallCompliancePercent}%.`,
        );
      }
    }
  }

  if (phase === 're_accreditation') {
    const exc = oesForBand(chapters, 'excellence');
    if (exc.length) {
      const ePct = getCumulativeCompliancePercent(exc);
      if (ePct < thresholds.minOverallCompliancePercent) {
        violations.push(
          `Excellence-level cumulative compliance ${ePct.toFixed(1)}% is below ${thresholds.minOverallCompliancePercent}%.`,
        );
      }
    }
  }

  const maxLowPerStd = phase === 're_accreditation' ? 0 : 1;
  for (const ch of chapters) {
    violations.push(...standardLowScoreViolations(ch.standards, maxLowPerStd));
  }

  for (const row of flat) {
    const oe = row.oe;
    if (!isOeInAssessmentScope(oe, phase)) continue;
    if (oe.score <= 3 && !hasAcceptedActionPlan(oe)) {
      violations.push(
        `OE ${oe.code} (score ${oe.score}) requires an accepted action plan with timelines.`,
      );
    }
  }

  return {
    overallCompliancePercent: overallPct,
    coreOeAverageScore: coreAvg,
    averageStandardScore: avgStd,
    averageChapterScore: avgCh,
    violations,
  };
}
