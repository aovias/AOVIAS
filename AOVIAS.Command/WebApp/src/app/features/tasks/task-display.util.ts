import type { TaskApiStatus, TaskRiskLevelApi, ComplianceTaskPriority } from './task-risk.util';
import type { TaskListItem } from './models';

export function taskStatusLabel(s: TaskApiStatus): string {
  switch (s) {
    case 'NotStarted':
      return 'Pending';
    case 'InProgress':
      return 'In progress';
    case 'Blocked':
      return 'Blocked';
    case 'Completed':
      return 'Done';
    default:
      return s;
  }
}

export function taskStatusBadgeClass(s: TaskApiStatus): string {
  switch (s) {
    case 'NotStarted':
      return 'st-pending';
    case 'InProgress':
      return 'st-active';
    case 'Blocked':
      return 'st-blocked';
    case 'Completed':
      return 'st-done';
    default:
      return 'st-pending';
  }
}

export function taskPriorityBadgeClass(p: ComplianceTaskPriority): string {
  switch (p) {
    case 'Urgent':
      return 'pr-urgent';
    case 'High':
      return 'pr-high';
    case 'Medium':
      return 'pr-mid';
    case 'Low':
      return 'pr-low';
  }
}

export function taskRiskBadgeClass(level: TaskRiskLevelApi): string {
  switch (level) {
    case 'Low':
      return 'rk-low';
    case 'Medium':
      return 'rk-medium';
    case 'High':
      return 'rk-high';
    case 'Critical':
      return 'rk-critical';
  }
}

/** Evidence cell: none / partial / all approved */
export function evidenceStateClass(t: TaskListItem): 'ev-none' | 'ev-partial' | 'ev-complete' {
  const tot = t.evidenceTotalCount ?? 0;
  const app = t.evidenceApprovedCount ?? 0;
  if (tot === 0) return 'ev-none';
  if (app >= tot) return 'ev-complete';
  return 'ev-partial';
}

export function isHighRiskRow(t: TaskListItem): boolean {
  return t.riskLevel === 'High' || t.riskLevel === 'Critical';
}
