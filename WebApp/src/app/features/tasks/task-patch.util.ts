import type { TaskApiStatus } from './task-risk.util';

/** Backend PatchTaskExecutionRequestDto uses ComplianceTaskStatus (string JSON enum). */
export type PatchComplianceTaskStatus = 'Pending' | 'InProgress' | 'OnHold' | 'Blocked';

export function patchStatusForApi(s: TaskApiStatus): PatchComplianceTaskStatus {
  switch (s) {
    case 'NotStarted':
      return 'Pending';
    case 'InProgress':
      return 'InProgress';
    case 'Blocked':
      return 'Blocked';
    case 'Completed':
      throw new Error('Completed must use the complete-task endpoint');
    default:
      return 'Pending';
  }
}
