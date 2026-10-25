import { GapCategory } from './gap-analysis-api.model';

export type GapKpiColor = 'primary' | 'success' | 'warning' | 'danger';

export interface GapCategoryKpi {
  title: string;
  value: number;
  color: GapKpiColor;
  label: string;
}

export interface GapChapterScoreVm {
  chapterId: string;
  chapterName: string;
  percent: number;
}

export interface GapItemVm {
  nodeId: string;
  nodeCode: string;
  nodeTitle: string;
  score: number;
  classification: string;
  complianceStatus: string;
  category: GapCategory;
  recommendation: string;
}

export interface GapRecommendationVm {
  nodeId: string;
  nodeCode: string;
  nodeTitle: string;
  category: GapCategory;
  recommendation: string;
}

export interface GapNextActionVm {
  order: number;
  label: string;
  route: string;
}

export interface GapAnalysisResultsVm {
  overallPercent: number;
  readinessLabel: string;
  coreRulePassed: boolean;
  generatedAt: string | null;
  categoryKpis: GapCategoryKpi[];
  chapterScores: GapChapterScoreVm[];
  criticalGaps: GapItemVm[];
  majorGaps: GapItemVm[];
  recommendations: GapRecommendationVm[];
  nextActions: GapNextActionVm[];
}
