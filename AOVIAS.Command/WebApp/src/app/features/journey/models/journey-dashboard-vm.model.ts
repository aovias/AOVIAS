import { OperationalMode, ChapterRiskLevel } from './nabh.enums';

export type JourneyModeState = 'active' | 'inactive' | 'locked';

/** Visual / interaction tier for journey mode tiles. */
export type JourneyModeCardVariant = 'active' | 'available' | 'locked';

/** Lucide icon keys for journey mode tiles (mapped in {@link JourneyCardComponent}). */
export type JourneyModeIconKey = 'self' | 'internal' | 'final' | 'surveillance';

export interface JourneyModeCardVm {
  mode: OperationalMode;
  label: string;
  description: string;
  state: JourneyModeState;
  isSelected: boolean;
  iconKey: JourneyModeIconKey;
  /** 1-based position in the lifecycle sequence (Self → Internal → Final → Surveillance). */
  stepIndex: number;
  totalSteps: number;
  /** Shown when {@link state} is `locked` (read-only phase, prerequisites, etc.). */
  lockReason: string | null;
  variant: JourneyModeCardVariant;
  /** True when this tile is the immediate next phase after the current mode (transition hint). */
  isNextPhase: boolean;
}

export type RecommendationIntent = 'open-priority-standard' | 'review-readiness' | 'view-oe-gaps';

export interface RecommendationPanelVm {
  title: string;
  summary: string;
  severity: 'info' | 'warning' | 'danger';
  actionLabel: string;
  actionIntent: RecommendationIntent;
  disabled: boolean;
}

export type PrimaryActionIntent =
  | 'resume-selected-standard'
  | 'open-most-risky-chapter'
  | 'review-open-action-plans';

export interface PrimaryActionVm {
  id: string;
  label: string;
  detail: string;
  intent: PrimaryActionIntent;
  disabled: boolean;
}

/** Ng-icon Lucide registry name (e.g. `lucidePercent`). */
export type MetricCardIconName = string;

export interface MetricCardVm {
  id: string;
  label: string;
  value: string;
  tone: 'neutral' | 'success' | 'warning' | 'danger';
  helperText: string;
  iconName: MetricCardIconName;
}

export type ChartCardIntent = 'oe-trend' | 'chapter-improvement' | 'risk-evolution';

export interface ChartCardVm {
  id: ChartCardIntent;
  title: string;
  subtitle: string;
  metricValue: string;
  intent: ChartCardIntent;
  trendDirection: 'up' | 'down' | 'flat';
  chartColorToken: string;
}

export interface JourneyDashboardContextVm {
  frameworkCode: string;
  frameworkName: string;
  selectedChapterCode: string | null;
  selectedStandardCode: string | null;
  selectedOeCode: string | null;
  chapterRisk: ChapterRiskLevel | null;
}
