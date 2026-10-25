/** Aligned with `COMPLIANCE_TASK_API_SPEC.md` — create body. */

export type TaskCriticalityLevel = 'Critical' | 'High' | 'Medium' | 'Low';

/** Matches backend `ComplianceTaskPriority`: Urgent=1, High=2, Medium=3, Low=4. */
export type ComplianceTaskPriorityValue = 1 | 2 | 3 | 4;

export interface CreateComplianceTaskRequest {
  objectiveElementId: string;
  complianceStandardId: string;
  assignedToUserId: string;
  departmentId: string;
  departmentName?: string;
  dueDate?: string;
  title?: string;
  description?: string;
  /** Preferred; maps to `ComplianceTaskPriority` enum on server. */
  priority?: ComplianceTaskPriorityValue;
  criticality?: TaskCriticalityLevel;
  programId?: string;
}

/** Minimal fields used after create (extend as needed when UI reads task detail). */
export interface ComplianceTaskDetailDto {
  id: string;
  hospitalId: string;
  objectiveElementId: string;
  complianceStandardId: string;
  departmentId?: string | null;
  assignedToUserId: string;
  title?: string | null;
  description?: string | null;
  status?: string;
  criticality?: TaskCriticalityLevel;
}
