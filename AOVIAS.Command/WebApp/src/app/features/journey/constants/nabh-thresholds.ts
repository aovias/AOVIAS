/**
 * Central NABH UI thresholds — accreditation panel & copy.
 * Aligned with NABH Hospital Standards 6th ed. (Jan 2025) decision criteria:
 * overall compliance ≥80%, core OE scores ≥4, standard/chapter averages ≥4 (PDF).
 */
export const NABH_THRESHOLDS = {
  minOverallCompliancePercent: 80,
  minCoreOeAverage: 4,
  minAverageStandard: 4,
  minAverageChapter: 4,
} as const;
