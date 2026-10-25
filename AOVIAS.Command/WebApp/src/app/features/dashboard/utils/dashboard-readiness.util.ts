import { DashboardSnapshot } from '../models/dashboard-snapshot.model';

/**
 * Normalizes completion rate into a 0..1 ratio for dashboard rendering.
 *
 * The backend now emits every compliance percentage on a canonical 0..100 scale
 * (single source of truth via IComplianceScoreService), so completionRate and the
 * score fallbacks are always percentages and are divided by 100 here. The previous
 * "value <= 1 means it is already a ratio" heuristic is intentionally removed: it
 * mis-scaled genuinely low percentages (e.g. 0.86% read as 86%).
 */
export function resolveDashboardCompletionRateRatio(snap: DashboardSnapshot | null): number {
  if (!snap) {
    return 0;
  }

  const summaryRate = snap.summary?.completionRate;
  if (typeof summaryRate === 'number' && !Number.isNaN(summaryRate)) {
    return Math.max(0, Math.min(1, summaryRate / 100));
  }

  const score =
    typeof snap.overallScore === 'number' && !Number.isNaN(snap.overallScore)
      ? snap.overallScore
      : snap.summary?.avgComplianceScore;

  if (typeof score !== 'number' || Number.isNaN(score)) {
    return 0;
  }

  return Math.max(0, Math.min(1, score / 100));
}

export function resolveDashboardCompletionRatePercent(snap: DashboardSnapshot | null): number {
  const ratio = resolveDashboardCompletionRateRatio(snap);
  return Math.max(0, Math.min(100, Math.round(ratio * 100)));
}

