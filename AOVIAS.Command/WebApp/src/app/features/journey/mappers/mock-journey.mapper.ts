import { MOCK_JOURNEY, MockChapterSeed, MockOeSeed, MockStandardSeed } from '../data/mock-journey-data';
import { JourneyCatalogMeta, JourneyWorkspaceSnapshot } from '../models/journey-tree.model';
import { JourneyChapter } from '../models/chapter.model';
import { ComplianceStandard } from '../models/standard.model';
import {
  ObjectiveElement,
  OeActionPlan,
  OeEvidenceItem,
  OeHistoryEvent,
} from '../models/objective-element.model';
import { ChapterRiskLevel, EvidenceStatus, NabhScore, OperationalMode, ScoreLabel } from '../models/nabh.enums';
import { evaluateAccreditationEligibility } from '../utils/nabh-accreditation-decision.util';
import { mapLifecycleToNabhAssessmentPhase } from '../utils/journey-lifecycle-map';

const SCORE_LABELS: ScoreLabel[] = [
  'Poor',
  'Limited',
  'Partial',
  'Substantial',
  'Full Compliance',
];

function scoreLabel(score: NabhScore): ScoreLabel {
  return SCORE_LABELS[score - 1] ?? 'Partial';
}

function mockOe(seed: MockOeSeed, standardId: string): ObjectiveElement {
  const score = seed.score as NabhScore;
  const actionPlans: OeActionPlan[] =
    score <= 3
      ? [
          {
            id: `ap-${seed.id}`,
            title: 'Corrective action',
            ownerName: 'Quality Manager',
            deadline: new Date().toISOString().slice(0, 10),
            status: 'closed',
            accepted: true,
          },
        ]
      : [];

  const ev: EvidenceStatus = score >= 4 ? 'complete' : 'pending_verification';

  return {
    id: seed.id,
    standardId,
    code: seed.code,
    description: seed.description,
    classification: seed.classification,
    score,
    scoreLabel: scoreLabel(score),
    evidenceStatus: ev,
    isNa: false,
    auditStatus: 'compliant',
    actionPlans,
    evidence: [],
    history: [],
    departmentLabel: seed.departmentLabel,
  };
}

function mockStandard(st: MockStandardSeed, chapterId: string, chapterRisk: ChapterRiskLevel): ComplianceStandard {
  const oes = st.objectiveElements.map((o) => mockOe(o, st.id));
  const avg = oes.length ? oes.reduce((a, o) => a + o.score, 0) / oes.length : 0;
  return {
    id: st.id,
    chapterId,
    code: st.code,
    title: st.title,
    description: st.description,
    completionPercent: Math.round((avg / 5) * 100),
    riskBadge: chapterRisk,
    objectiveElements: oes,
  };
}

function chapterRiskFromScores(standards: MockStandardSeed[]): ChapterRiskLevel {
  let sum = 0;
  let n = 0;
  for (const st of standards) {
    for (const oe of st.objectiveElements) {
      sum += oe.score;
      n += 1;
    }
  }
  const avg = n ? sum / n : 3;
  if (avg <= 2.5) return 'high';
  if (avg <= 3.5) return 'medium';
  return 'low';
}

function mockChapter(ch: MockChapterSeed): JourneyChapter {
  const risk = chapterRiskFromScores(ch.standards);
  const standards = ch.standards.map((s) => mockStandard(s, ch.id, risk));
  let coreFail = false;
  let openIssues = 0;
  for (const st of standards) {
    for (const oe of st.objectiveElements) {
      if (oe.classification === 'core' && oe.score < 4) coreFail = true;
      if (oe.score <= 3) openIssues += 1;
    }
  }
  const compliance = Math.round(
    (standards.reduce((a, st) => a + st.completionPercent, 0) / Math.max(standards.length, 1)),
  );
  return {
    id: ch.id,
    code: ch.code,
    name: ch.name,
    complianceScorePercent: compliance,
    risk,
    coreComplianceStatus: coreFail ? 'failed' : 'met',
    openIssuesCount: openIssues,
    pendingAudit: false,
    coreFailureFlag: coreFail,
    standards,
  };
}

function catalogMetaFromMockJourney(): JourneyCatalogMeta {
  const totalStandards = MOCK_JOURNEY.chapters.reduce((n, ch) => n + ch.standards.length, 0);
  const totalOes = MOCK_JOURNEY.chapters.reduce(
    (n, ch) => n + ch.standards.reduce((m, st) => m + st.objectiveElements.length, 0),
    0,
  );
  return {
    standardEdition: '6',
    effectiveFrom: '2025-01-01',
    totalStandardsExpected: totalStandards,
    totalOesExpected: totalOes,
    isPartialCatalog: true,
  };
}

export function buildMockWorkspaceSnapshot(
  journeyId: string = MOCK_JOURNEY.journeyId,
  lifecycleMode: OperationalMode = OperationalMode.SelfAssessment,
): JourneyWorkspaceSnapshot {
  const chapters = MOCK_JOURNEY.chapters.map(mockChapter);
  const base: JourneyWorkspaceSnapshot = {
    journeyId,
    frameworkCode: MOCK_JOURNEY.frameworkCode,
    frameworkName: MOCK_JOURNEY.frameworkName,
    catalog: catalogMetaFromMockJourney(),
    chapters,
    accreditation: {
      overallCompliancePercent: 0,
      coreOeAverageScore: 0,
      averageStandardScore: 0,
      averageChapterScore: 0,
      violations: [],
    },
    overview: {
      coreCompliancePercent: 0,
      failedCoreCount: 0,
      oesAtOrBelowThreeCount: 0,
      openActionPlansCount: 0,
      stageBreakdown: [],
    },
    oeScoreTrend: [
      { label: 'T1', value: 72 },
      { label: 'T2', value: 78 },
      { label: 'T3', value: 82 },
    ],
    chapterImprovementTrend: [
      { label: 'T1', value: 2 },
      { label: 'T2', value: 4 },
      { label: 'T3', value: 5 },
    ],
    riskEvolutionTrend: [
      { label: 'T1', value: 12 },
      { label: 'T2', value: 9 },
      { label: 'T3', value: 7 },
    ],
    lifecycleMode,
    readOnly: false,
    lastSyncedAt: new Date(Date.now() - 3 * 60 * 60 * 1000).toISOString(),
  };

  const phase = mapLifecycleToNabhAssessmentPhase(lifecycleMode);
  base.accreditation = evaluateAccreditationEligibility(base, phase);

  let corePct = 0;
  let failedCore = 0;
  let le3 = 0;
  let plans = 0;
  for (const ch of chapters) {
    for (const st of ch.standards) {
      for (const oe of st.objectiveElements) {
        if (oe.classification === 'core') {
          if (oe.score < 4) failedCore += 1;
        }
        if (oe.score <= 3) le3 += 1;
        plans += oe.actionPlans.filter((p) => p.status === 'open' || p.status === 'in_progress').length;
      }
    }
  }
  const coreOes = chapters.flatMap((c) =>
    c.standards.flatMap((s) => s.objectiveElements.filter((o) => o.classification === 'core')),
  );
  corePct =
    coreOes.length > 0
      ? Math.round((coreOes.filter((o) => o.score >= 4).length / coreOes.length) * 100)
      : 100;

  base.overview = {
    coreCompliancePercent: corePct,
    failedCoreCount: failedCore,
    oesAtOrBelowThreeCount: le3,
    openActionPlansCount: plans,
    stageBreakdown: [],
  };

  return base;
}
