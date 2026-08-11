import { HeatmapItem } from '../../dashboard/components/compliance-heatmap.component';
import { ComplianceMaturityDashboardModel, GapSeverity } from '../models/compliance-maturity.model';

const SEVERITY_PENALTY: Record<GapSeverity, number> = {
  critical: 30,
  major: 15,
  minor: 5,
};

/**
 * Derives chapter-level readiness scores from actual core gaps.
 * Chapters without gaps receive the overall core readiness score.
 */
export function buildChapterHeatmapItems(dashboard: ComplianceMaturityDashboardModel): HeatmapItem[] {
  const coreReadiness = Math.round(dashboard.coreSummary.readinessScore);
  const chapterMap = new Map<string, { penalty: number; gapCount: number }>();

  for (const gap of dashboard.coreSummary.items) {
    const chapter = gap.chapter?.trim() || 'General';
    const existing = chapterMap.get(chapter) ?? { penalty: 0, gapCount: 0 };
    existing.penalty += SEVERITY_PENALTY[gap.severity] ?? 5;
    existing.gapCount += 1;
    chapterMap.set(chapter, existing);
  }

  const predictedChapters = new Set<string>();
  for (const summary of [
    dashboard.commitmentSummary,
    dashboard.achievementSummary,
    dashboard.excellenceSummary,
  ]) {
    for (const item of summary.items) {
      if (item.chapter?.trim()) {
        predictedChapters.add(item.chapter.trim());
      }
    }
  }

  const items: HeatmapItem[] = [];

  for (const [chapter, data] of chapterMap) {
    const score = Math.max(0, Math.min(100, 100 - data.penalty));
    items.push({
      label: chapter,
      score,
      metadata: `${data.gapCount} gap${data.gapCount === 1 ? '' : 's'}`,
    });
  }

  for (const chapter of predictedChapters) {
    if (!chapterMap.has(chapter)) {
      items.push({
        label: chapter,
        score: coreReadiness,
        metadata: 'Predicted',
      });
    }
  }

  return items.sort((a, b) => a.label.localeCompare(b.label));
}
