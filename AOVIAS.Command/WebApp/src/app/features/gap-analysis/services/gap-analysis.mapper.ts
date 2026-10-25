import {
  GapAnalysisItemDto,
  GapAnalysisReportDto,
  GapCategory,
} from '../models/gap-analysis-api.model';
import {
  GapAnalysisResultsVm,
  GapCategoryKpi,
  GapChapterScoreVm,
  GapItemVm,
  GapNextActionVm,
  GapRecommendationVm,
} from '../models/gap-analysis-vm.model';

const CATEGORY_ORDER: Record<GapCategory, number> = {
  Critical: 0,
  Major: 1,
  Minor: 2,
  Compliant: 3,
};

export function mapGapAnalysisToVm(report: GapAnalysisReportDto | null): GapAnalysisResultsVm | null {
  if (!report) {
    return null;
  }

  const overallPercent = Math.round(Number(report.overallPercent) || 0);

  return {
    overallPercent,
    readinessLabel: overallPercent >= 80 ? 'Ready' : 'In Progress',
    coreRulePassed: report.coreRulePassed,
    generatedAt: report.generatedAt ?? null,
    categoryKpis: buildCategoryKpis(report),
    chapterScores: buildChapterScores(report),
    criticalGaps: filterGapItems(report.items, 'Critical'),
    majorGaps: filterGapItems(report.items, 'Major'),
    recommendations: buildRecommendations(report.items),
    nextActions: buildNextActions(report),
  };
}

function buildCategoryKpis(report: GapAnalysisReportDto): GapCategoryKpi[] {
  return [
    {
      title: 'Critical Gaps',
      value: report.criticalCount,
      color: 'danger',
      label: 'Immediate attention',
    },
    {
      title: 'Major Gaps',
      value: report.majorCount,
      color: 'warning',
      label: 'High priority',
    },
    {
      title: 'Minor Gaps',
      value: report.minorCount,
      color: 'primary',
      label: 'Follow-up',
    },
    {
      title: 'Compliant',
      value: report.compliantCount,
      color: 'success',
      label: 'Meeting threshold',
    },
  ];
}

function buildChapterScores(report: GapAnalysisReportDto): GapChapterScoreVm[] {
  const averages = report.summary?.chapterAverages ?? {};
  const chapterNames = new Map<string, string>();

  for (const item of report.items) {
    if (item.chapterId && !chapterNames.has(item.chapterId)) {
      const sibling = report.items.find(
        (i) => i.chapterId === item.chapterId && i.nodeId !== item.nodeId,
      );
      chapterNames.set(item.chapterId, resolveChapterLabel(item, sibling));
    }
  }

  return Object.entries(averages)
    .map(([chapterId, avgScore]) => ({
      chapterId,
      chapterName: chapterNames.get(chapterId) ?? `Chapter ${chapterId.slice(0, 8)}`,
      percent: scoreAverageToPercent(avgScore),
    }))
    .sort((a, b) => a.chapterName.localeCompare(b.chapterName));
}

function resolveChapterLabel(item: GapAnalysisItemDto, sibling?: GapAnalysisItemDto): string {
  if (item.nodeCode) {
    const parts = item.nodeCode.split('.');
    if (parts.length >= 1) {
      return parts[0];
    }
  }
  return sibling?.nodeTitle?.split(' ')[0] ?? 'Chapter';
}

export function scoreAverageToPercent(avgScore: number): number {
  const score = Number(avgScore);
  if (!Number.isFinite(score)) {
    return 0;
  }
  return Math.max(0, Math.min(100, Math.round((score / 5) * 100)));
}

function filterGapItems(items: GapAnalysisItemDto[], category: GapCategory): GapItemVm[] {
  return items
    .filter((i) => i.category === category)
    .map(toGapItemVm)
    .sort((a, b) => a.score - b.score);
}

function toGapItemVm(item: GapAnalysisItemDto): GapItemVm {
  return {
    nodeId: item.nodeId,
    nodeCode: item.nodeCode,
    nodeTitle: item.nodeTitle,
    score: item.score,
    classification: item.classification,
    complianceStatus: item.complianceStatus,
    category: item.category,
    recommendation: item.recommendation,
  };
}

function buildRecommendations(items: GapAnalysisItemDto[]): GapRecommendationVm[] {
  const seen = new Set<string>();
  const sorted = [...items]
    .filter((i) => i.category !== 'Compliant' && i.recommendation?.trim())
    .sort((a, b) => CATEGORY_ORDER[a.category] - CATEGORY_ORDER[b.category]);

  const result: GapRecommendationVm[] = [];
  for (const item of sorted) {
    if (seen.has(item.nodeId)) {
      continue;
    }
    seen.add(item.nodeId);
    result.push({
      nodeId: item.nodeId,
      nodeCode: item.nodeCode,
      nodeTitle: item.nodeTitle,
      category: item.category,
      recommendation: item.recommendation.trim(),
    });
  }
  return result;
}

function buildNextActions(report: GapAnalysisReportDto): GapNextActionVm[] {
  const actions: GapNextActionVm[] = [];
  let order = 1;

  if (report.criticalCount > 0) {
    actions.push({
      order: order++,
      label: `Resolve ${report.criticalCount} critical gap${report.criticalCount === 1 ? '' : 's'} (CORE priority)`,
      route: '/app/journey/execution',
    });
  }

  if (report.majorCount > 0) {
    actions.push({
      order: order++,
      label: `Review ${report.majorCount} major gap${report.majorCount === 1 ? '' : 's'} in affected standards`,
      route: '/app/journey/execution',
    });
  }

  if (!report.coreRulePassed) {
    actions.push({
      order: order++,
      label: 'Address CORE requirement failures before audit readiness review',
      route: '/app/reports/readiness',
    });
  }

  actions.push({
    order: order++,
    label: 'Open Compliance Command Center for executive overview',
    route: '/app/control-center',
  });

  actions.push({
    order: order++,
    label: 'Continue compliance journey execution',
    route: '/app/journey/execution',
  });

  return actions;
}
