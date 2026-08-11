import {
  resolveDashboardCompletionRatePercent,
  resolveDashboardCompletionRateRatio,
} from './dashboard-readiness.util';
import type { DashboardSnapshot } from '../models/dashboard-snapshot.model';
import { describe, expect, it } from 'vitest';

describe('dashboard-readiness.util', () => {
  it('treats summary completion rate as a canonical 0-100 percentage', () => {
    const snap = {
      summary: {
        completionRate: 72,
      },
    } as DashboardSnapshot;

    expect(resolveDashboardCompletionRateRatio(snap)).toBe(0.72);
  });

  it('does not inflate genuinely low percentages (e.g. 0.86% must not read as 86%)', () => {
    const snap = {
      summary: {
        completionRate: 0.86,
      },
    } as DashboardSnapshot;

    expect(resolveDashboardCompletionRateRatio(snap)).toBeCloseTo(0.0086, 6);
  });

  it('falls back to overallScore (0-100) when summary completion rate is missing', () => {
    const snap = {
      overallScore: 81,
      summary: {
        completionRate: undefined as unknown as number,
      },
    } as DashboardSnapshot;

    expect(resolveDashboardCompletionRateRatio(snap)).toBe(0.81);
  });

  it('returns 0 when snapshot is null', () => {
    expect(resolveDashboardCompletionRateRatio(null)).toBe(0);
  });

  describe('resolveDashboardCompletionRatePercent', () => {
    it('passes through a canonical 0-100 completion rate', () => {
      const snap = {
        summary: {
          completionRate: 72,
        },
      } as DashboardSnapshot;

      expect(resolveDashboardCompletionRatePercent(snap)).toBe(72);
    });

    it('normalizes percent-based completion rate into a bounded percent value', () => {
      const snap = {
        summary: {
          completionRate: 120,
        },
      } as DashboardSnapshot;

      expect(resolveDashboardCompletionRatePercent(snap)).toBe(100);
    });

    it('falls back to avgComplianceScore when summary completion rate is unavailable', () => {
      const snap = {
        summary: {
          completionRate: undefined as unknown as number,
          avgComplianceScore: 88,
        },
      } as DashboardSnapshot;

      expect(resolveDashboardCompletionRatePercent(snap)).toBe(88);
    });

    it('returns 0 when snapshot is null', () => {
      expect(resolveDashboardCompletionRatePercent(null)).toBe(0);
    });
  });
});

