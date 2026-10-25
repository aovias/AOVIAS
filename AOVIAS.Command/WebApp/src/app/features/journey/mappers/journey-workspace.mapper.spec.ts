import {
  mergeJourneyWorkspaceFromSplit,
  mapChapterIndexItemToJourneyChapter,
  mapOverviewMetricsApiToDomain,
} from './journey-workspace.mapper';
import { JourneySessionDto, JourneyDashboardDto, JourneyChaptersIndexDto } from '../models/journey-api.model';

describe('journey-workspace.mapper split API', () => {
  it('unwraps chapters index items', () => {
    const chapters: JourneyChaptersIndexDto = {
      items: [
        {
          id: 'ch-1',
          code: 'AAC',
          name: 'AAC Chapter',
          complianceScorePercent: 80,
          risk: 'low',
          coreComplianceStatus: 'Compliant',
          openIssuesCount: 0,
        },
      ],
    };
    const session: JourneySessionDto = {
      journeyId: 'j1',
      frameworkCode: 'NABH',
      frameworkName: 'NABH 6th',
      operationalMode: 1,
      readOnly: false,
      snapshotReadOnly: false,
    };
    const dashboard: JourneyDashboardDto = {
      overview: {
        coreCompliancePercent: 90,
        failedCoreCount: 0,
        oesAtOrBelowThreeCount: 1,
        openActionPlansCount: 0,
      },
      accreditationReadinessPercent: 88,
    };
    const merged = mergeJourneyWorkspaceFromSplit('j1', session, dashboard, chapters);
    expect(merged.chapters.length).toBe(1);
    expect(merged.chapters[0].code).toBe('AAC');
    expect(merged.chapters[0].standards).toEqual([]);
    expect(merged.overview.coreCompliancePercent).toBe(90);
    expect(merged.accreditationReadinessPercent).toBe(88);
  });

  it('mapChapterIndexItemToJourneyChapter maps optional badges', () => {
    const ch = mapChapterIndexItemToJourneyChapter({
      id: 'x',
      code: 'COP',
      name: 'Care',
      issueBadgeLabel: '2 open',
      highlightHighRisk: true,
    });
    expect(ch.issueBadgeLabel).toBe('2 open');
    expect(ch.highlightHighRisk).toBe(true);
  });

  it('mapOverviewMetricsApiToDomain handles null', () => {
    expect(mapOverviewMetricsApiToDomain(null).coreCompliancePercent).toBe(0);
  });
});
