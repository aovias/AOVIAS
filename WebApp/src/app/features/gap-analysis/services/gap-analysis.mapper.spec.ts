import { describe, expect, it } from 'vitest';
import {
  mapGapAnalysisToVm,
  scoreAverageToPercent,
} from './gap-analysis.mapper';
import { GapAnalysisReportDto } from '../models/gap-analysis-api.model';

function sampleReport(overrides: Partial<GapAnalysisReportDto> = {}): GapAnalysisReportDto {
  const chapterId = '11111111-1111-1111-1111-111111111111';
  return {
    id: 'report-1',
    assessmentId: 'assessment-1',
    frameworkId: 'fw-1',
    facilityId: 'fac-1',
    generatedAt: '2026-06-15T10:00:00Z',
    overallPercent: 60,
    meetsOverallPercent: false,
    coreRulePassed: false,
    chapterAveragesPassed: true,
    standardAveragesPassed: true,
    commitmentLevelPassed: true,
    criticalCount: 1,
    majorCount: 1,
    minorCount: 0,
    compliantCount: 0,
    summary: {
      overallPercent: 60,
      meetsOverallPercent: false,
      coreRulePassed: false,
      coreFailureNodeIds: ['oe-critical'],
      chapterAverages: { [chapterId]: 3 },
      standardAverages: {},
      chapterAveragesPassed: true,
      standardAveragesPassed: true,
      commitmentLevelPassed: true,
      tooManyLowScoresInStandard: false,
      standardsWithMultipleLowScores: [],
      actionPlanViolationNodeIds: [],
    },
    items: [
      {
        nodeId: 'oe-critical',
        nodeCode: 'AAC.1.a',
        nodeTitle: 'Critical OE',
        chapterId,
        standardId: null,
        score: 1,
        complianceStatus: 'NotMet',
        classification: 'CORE',
        category: 'Critical',
        recommendation: 'Fix immediately',
      },
      {
        nodeId: 'oe-major',
        nodeCode: 'AAC.2.a',
        nodeTitle: 'Major OE',
        chapterId,
        standardId: null,
        score: 2,
        complianceStatus: 'NotMet',
        classification: 'Achievement',
        category: 'Major',
        recommendation: 'Improve process',
      },
    ],
    ...overrides,
  };
}

describe('gap-analysis.mapper', () => {
  it('returns null when report is null', () => {
    expect(mapGapAnalysisToVm(null)).toBeNull();
  });

  it('converts chapter average score to percent', () => {
    expect(scoreAverageToPercent(5)).toBe(100);
    expect(scoreAverageToPercent(3)).toBe(60);
    expect(scoreAverageToPercent(NaN)).toBe(0);
  });

  it('maps overall readiness label from percent', () => {
    const ready = mapGapAnalysisToVm(sampleReport({ overallPercent: 85 }));
    const inProgress = mapGapAnalysisToVm(sampleReport({ overallPercent: 60 }));
    expect(ready?.readinessLabel).toBe('Ready');
    expect(inProgress?.readinessLabel).toBe('In Progress');
  });

  it('filters critical and major gaps sorted by score', () => {
    const vm = mapGapAnalysisToVm(sampleReport());
    expect(vm?.criticalGaps).toHaveLength(1);
    expect(vm?.majorGaps).toHaveLength(1);
    expect(vm?.criticalGaps[0].nodeCode).toBe('AAC.1.a');
  });

  it('builds recommendations for non-compliant items', () => {
    const vm = mapGapAnalysisToVm(sampleReport());
    expect(vm?.recommendations).toHaveLength(2);
    expect(vm?.recommendations[0].category).toBe('Critical');
  });

  it('builds next actions including critical and command center links', () => {
    const vm = mapGapAnalysisToVm(sampleReport());
    expect(vm?.nextActions.some((a) => a.label.includes('critical gap'))).toBe(true);
    expect(vm?.nextActions.some((a) => a.route === '/app/control-center')).toBe(true);
  });

  it('maps chapter scores from summary averages', () => {
    const vm = mapGapAnalysisToVm(sampleReport());
    expect(vm?.chapterScores).toHaveLength(1);
    expect(vm?.chapterScores[0].percent).toBe(60);
  });
});
