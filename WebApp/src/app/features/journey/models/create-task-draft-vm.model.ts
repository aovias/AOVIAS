/** Draft VM for Create & Assign Task drawer (Phase 59). Serializable subset for sessionStorage. */

/** Matches backend `ComplianceTaskPriority`: Urgent=1, High=2, Medium=3, Low=4. */
export type ComplianceTaskPriority = 1 | 2 | 3 | 4;

export interface CreateTaskDraftVm {
  title: string;
  description: string;
  /** Single assignee user id (Compliance API `assignedToUserId`). */
  assignedToUserId: string;
  departmentId: string | null;
  /** Denormalized label for drafts / display. */
  departmentLabel: string;
  dueAt: string | null;
  /** Free text or preset (e.g. 1h, 2h) for planning; optional. */
  effortEstimate: string;
  priority: ComplianceTaskPriority;
  standardId: string | null;
  standardLabel: string;
  objectiveElementId: string | null;
  objectiveLabel: string;
}

export function createEmptyCreateTaskDraftVm(): CreateTaskDraftVm {
  return {
    title: '',
    description: '',
    assignedToUserId: '',
    departmentId: null,
    departmentLabel: '',
    dueAt: null,
    effortEstimate: '',
    priority: 3,
    standardId: null,
    standardLabel: '',
    objectiveElementId: null,
    objectiveLabel: '',
  };
}

/** Merge persisted session JSON with contextual OE linkage; migrates legacy `assigneeIds[]` and old priority strings. */
export function mergeTaskDraftFromSession(
  contextual: CreateTaskDraftVm,
  saved: Partial<CreateTaskDraftVm> & { assigneeIds?: string[]; tags?: string[]; status?: string; type?: string },
): CreateTaskDraftVm {
  const assignedToUserId =
    (typeof saved.assignedToUserId === 'string' ? saved.assignedToUserId : undefined) ??
    (Array.isArray(saved.assigneeIds) && saved.assigneeIds.length ? saved.assigneeIds[0] : undefined) ??
    contextual.assignedToUserId;

  return {
    title: saved.title ?? contextual.title,
    description: saved.description ?? contextual.description,
    assignedToUserId,
    departmentId: saved.departmentId ?? contextual.departmentId,
    departmentLabel: saved.departmentLabel ?? contextual.departmentLabel,
    dueAt: saved.dueAt ?? contextual.dueAt,
    effortEstimate: saved.effortEstimate ?? contextual.effortEstimate,
    priority: migratePriority(saved.priority ?? contextual.priority),
    standardId: contextual.standardId,
    standardLabel: contextual.standardLabel,
    objectiveElementId: contextual.objectiveElementId,
    objectiveLabel: contextual.objectiveLabel,
  };
}

function migratePriority(p: unknown): ComplianceTaskPriority {
  if (typeof p === 'number' && p >= 1 && p <= 4 && Number.isInteger(p)) {
    return p as ComplianceTaskPriority;
  }
  const legacy = String(p ?? '').toLowerCase();
  const fromString: Record<string, ComplianceTaskPriority> = {
    urgent: 1,
    critical: 1,
    high: 2,
    medium: 3,
    low: 4,
    '1': 1,
    '2': 2,
    '3': 3,
    '4': 4,
  };
  return fromString[legacy] ?? 3;
}
