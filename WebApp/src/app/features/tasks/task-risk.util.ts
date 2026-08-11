/**
 * Mirrors ComplianceService `TaskRiskCalculator` — keep in sync with backend.
 */
export type TaskCriticalityLevel = 'Critical' | 'High' | 'Medium' | 'Low';
export type ComplianceTaskPriority = 'Urgent' | 'High' | 'Medium' | 'Low';
export type TaskApiStatus = 'NotStarted' | 'InProgress' | 'Blocked' | 'Completed';

export type TaskRiskLevelApi = 'Low' | 'Medium' | 'High' | 'Critical';

const criticalityWeight = (c: TaskCriticalityLevel): number =>
  c === 'Critical' ? 4 : c === 'High' ? 3 : c === 'Medium' ? 2 : 1;

const priorityWeight = (p: ComplianceTaskPriority): number =>
  p === 'Urgent' ? 4 : p === 'High' ? 3 : p === 'Medium' ? 2 : 1;

export function isOverdue(dueDate: string | null | undefined, status: TaskApiStatus, todayUtc: Date): boolean {
  if (!dueDate || status === 'Completed') return false;
  const d = new Date(dueDate);
  const du = Date.UTC(d.getUTCFullYear(), d.getUTCMonth(), d.getUTCDate());
  const tu = Date.UTC(todayUtc.getUTCFullYear(), todayUtc.getUTCMonth(), todayUtc.getUTCDate());
  return du < tu;
}

export function computeRiskScore(
  dueDate: string | null | undefined,
  status: TaskApiStatus,
  criticality: TaskCriticalityLevel,
  priority: ComplianceTaskPriority,
  todayUtc: Date = new Date(),
): number {
  const overdueW = isOverdue(dueDate, status, todayUtc) ? 2 : 0;
  return criticalityWeight(criticality) + overdueW + priorityWeight(priority);
}

export function toRiskLevel(score: number): TaskRiskLevelApi {
  if (score <= 2) return 'Low';
  if (score <= 4) return 'Medium';
  if (score <= 6) return 'High';
  return 'Critical';
}
