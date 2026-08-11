import { ChapterRiskLevel } from './nabh.enums';
import { ObjectiveElement } from './objective-element.model';

/** Standard (e.g. AAC.1) — expandable section containing OEs */
export interface ComplianceStandard {
  id: string;
  chapterId: string;
  code: string;
  title: string;
  description: string;
  completionPercent: number;
  riskBadge: ChapterRiskLevel;
  coreSummary?: string;
  objectiveElements: ObjectiveElement[];
}
