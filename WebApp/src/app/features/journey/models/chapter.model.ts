import { ChapterRiskLevel } from './nabh.enums';
import { ComplianceStandard } from './standard.model';

export interface JourneyChapter {
  id: string;
  code: string;
  name: string;
  /** 0–100 NABH-aligned compliance score for the chapter */
  complianceScorePercent: number;
  risk: ChapterRiskLevel;
  coreComplianceStatus: 'met' | 'at_risk' | 'failed';
  openIssuesCount: number;
  pendingAudit: boolean;
  /** True when any core OE in chapter is below threshold */
  coreFailureFlag: boolean;
  standards: ComplianceStandard[];
  /** Optional display fields from GET …/chapters index (Phase 48 / API). */
  issueBadgeLabel?: string;
  passFailLabel?: string;
  changedSinceLastAudit?: boolean | null;
  maturityLabel?: string;
  highlightHighRisk?: boolean;
}
