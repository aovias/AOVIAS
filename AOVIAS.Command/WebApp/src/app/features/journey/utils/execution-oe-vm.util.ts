import { ObjectiveElement } from '../models/objective-element.model';
import {
  ExecutionOeComplianceTone,
  ExecutionOeStatusTone,
} from '../models/execution-screen-vm.model';

export function oeRowStatus(oe: ObjectiveElement): {
  statusLabel: string;
  statusTone: ExecutionOeStatusTone;
} {
  if (oe.isNa) {
    return { statusLabel: 'N/A', statusTone: 'na' };
  }
  if (oe.auditStatus === 'non_compliant') {
    return { statusLabel: 'Non-compliant', statusTone: 'non-compliant' };
  }
  if (oe.auditStatus === 'not_applicable') {
    return { statusLabel: 'N/A', statusTone: 'not-applicable' };
  }
  if (oe.score <= 3) {
    return { statusLabel: 'Partial', statusTone: 'partial' };
  }
  return { statusLabel: 'Compliant', statusTone: 'compliant' };
}

export function oeCompliancePresentation(oe: ObjectiveElement): {
  complianceLabel: string;
  complianceTone: ExecutionOeComplianceTone;
} {
  if (oe.isNa) {
    return { complianceLabel: 'Not applicable', complianceTone: 'not-applicable' };
  }
  if (oe.auditStatus === 'non_compliant') {
    return { complianceLabel: 'Non compliant', complianceTone: 'non-compliant' };
  }
  if (oe.auditStatus === 'not_applicable') {
    return { complianceLabel: 'Not applicable', complianceTone: 'not-applicable' };
  }
  if (oe.score <= 3) {
    return { complianceLabel: 'Partial compliance', complianceTone: 'partial' };
  }
  return { complianceLabel: 'Compliant', complianceTone: 'compliant' };
}

function titleCaseWord(s: string): string {
  if (!s) return '';
  return s.charAt(0).toUpperCase() + s.slice(1).toLowerCase();
}

export function oeContextTags(oe: ObjectiveElement): string[] {
  const tags: string[] = [];
  if (oe.departmentLabel?.trim()) {
    tags.push(oe.departmentLabel.trim());
  }
  if (oe.classification) {
    tags.push(titleCaseWord(oe.classification));
  }
  if (oe.assigneeDisplayName?.trim()) {
    tags.push(oe.assigneeDisplayName.trim());
  }
  return tags;
}

export function oeFrequencyLabel(oe: ObjectiveElement): string {
  if (oe.nextReviewAt?.trim()) {
    return `Next review · ${oe.nextReviewAt.trim()}`;
  }
  if (oe.dueAt?.trim()) {
    return `Due · ${oe.dueAt.trim()}`;
  }
  return 'No schedule';
}
