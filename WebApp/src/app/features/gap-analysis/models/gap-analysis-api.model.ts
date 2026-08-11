/** Gateway KB gap analysis DTOs (`/api/v1/knowledge-base/assessments/{id}/gap-analysis`). */

export type GapCategory = 'Critical' | 'Major' | 'Minor' | 'Compliant';

export interface AssessmentSummaryResponseDto {
  overallPercent: number;
  meetsOverallPercent: boolean;
  coreRulePassed: boolean;
  coreFailureNodeIds: string[];
  chapterAverages: Record<string, number>;
  standardAverages: Record<string, number>;
  chapterAveragesPassed: boolean;
  standardAveragesPassed: boolean;
  commitmentLevelPassed: boolean;
  tooManyLowScoresInStandard: boolean;
  standardsWithMultipleLowScores: string[];
  actionPlanViolationNodeIds: string[];
}

export interface GapAnalysisItemDto {
  nodeId: string;
  nodeCode: string;
  nodeTitle: string;
  chapterId: string | null;
  standardId: string | null;
  score: number;
  complianceStatus: string;
  classification: string;
  category: GapCategory;
  recommendation: string;
}

export interface GapAnalysisReportDto {
  id: string;
  assessmentId: string;
  frameworkId: string;
  facilityId: string;
  generatedAt: string;
  overallPercent: number;
  meetsOverallPercent: boolean;
  coreRulePassed: boolean;
  chapterAveragesPassed: boolean;
  standardAveragesPassed: boolean;
  commitmentLevelPassed: boolean;
  criticalCount: number;
  majorCount: number;
  minorCount: number;
  compliantCount: number;
  summary: AssessmentSummaryResponseDto;
  items: GapAnalysisItemDto[];
}
