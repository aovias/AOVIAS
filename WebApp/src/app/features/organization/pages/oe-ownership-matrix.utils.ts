import type { OeOwnershipMatrixItem } from '../services/oe-ownership-api.service';
import type { DepartmentTreeNode } from '../services/organization-api.service';

export type OeDisplayStatus = 'Unassigned' | 'Assigned' | 'Due Soon' | 'Overdue';

export type OeActivityState = 'idle' | 'dirty' | 'saving' | 'saved' | 'error';

export interface FlatDepartment {
  id: string;
  code: string;
  name: string;
}

export interface OeColumnDef {
  id: string;
  header: string;
  enabled: boolean;
}

/** Declarative columns — future fields stay disabled until product ships them. */
export const OE_OWNERSHIP_COLUMNS: OeColumnDef[] = [
  { id: 'select', header: '', enabled: true },
  { id: 'code', header: 'OE', enabled: true },
  { id: 'title', header: 'Objective Element', enabled: true },
  { id: 'department', header: 'Department', enabled: true },
  { id: 'owner', header: 'Owner', enabled: true },
  { id: 'dueDate', header: 'Due Date', enabled: true },
  { id: 'status', header: 'Status', enabled: true },
  { id: 'activity', header: 'Activity', enabled: true },
  { id: 'secondaryOwner', header: 'Secondary Owner', enabled: false },
  { id: 'reviewer', header: 'Reviewer', enabled: false },
  { id: 'approver', header: 'Approver', enabled: false },
  { id: 'targetCompletion', header: 'Target Completion', enabled: false },
  { id: 'riskLevel', header: 'Risk Level', enabled: false },
  { id: 'lastUpdated', header: 'Last Updated', enabled: false },
];

export const DUE_SOON_DAYS = 7;
export const VIRTUAL_SCROLL_THRESHOLD = 300;
export const ROW_HEIGHT_PX = 44;
export const SAVE_DEBOUNCE_MS = 400;
export const SAVE_CONCURRENCY = 5;

export function flattenDepartments(nodes: DepartmentTreeNode[] | undefined | null): FlatDepartment[] {
  const out: FlatDepartment[] = [];
  const walk = (list: DepartmentTreeNode[]) => {
    for (const n of list) {
      if (n?.id) {
        out.push({ id: n.id, code: n.code || '', name: n.name || '' });
      }
      if (n.children?.length) walk(n.children);
    }
  };
  walk(nodes ?? []);
  return out;
}

export function isAssigned(row: Pick<OeOwnershipMatrixItem, 'departmentId' | 'primaryOwnerUserId' | 'status'>): boolean {
  const hasPeople = !!(row.departmentId && row.primaryOwnerUserId);
  if (!hasPeople) return false;
  if (row.status && row.status.toLowerCase() === 'inactive') return false;
  return true;
}

export function computeDisplayStatus(
  row: Pick<OeOwnershipMatrixItem, 'departmentId' | 'primaryOwnerUserId' | 'dueDate' | 'status'>,
  now = new Date(),
): OeDisplayStatus {
  if (!isAssigned(row)) return 'Unassigned';
  if (!row.dueDate) return 'Assigned';
  const due = new Date(row.dueDate);
  if (Number.isNaN(due.getTime())) return 'Assigned';
  const startOfToday = new Date(now.getFullYear(), now.getMonth(), now.getDate());
  const dueDay = new Date(due.getFullYear(), due.getMonth(), due.getDate());
  if (dueDay < startOfToday) return 'Overdue';
  const soon = new Date(startOfToday);
  soon.setDate(soon.getDate() + DUE_SOON_DAYS);
  if (dueDay <= soon) return 'Due Soon';
  return 'Assigned';
}

export interface OwnershipKpis {
  total: number;
  assigned: number;
  pending: number;
  overdue: number;
  completionPct: number;
}

export function computeKpis(rows: OeOwnershipMatrixItem[]): OwnershipKpis {
  let assigned = 0;
  let overdue = 0;
  for (const r of rows) {
    const s = computeDisplayStatus(r);
    if (s === 'Unassigned') continue;
    assigned++;
    if (s === 'Overdue') overdue++;
  }
  const total = rows.length;
  const pending = total - assigned;
  const completionPct = total === 0 ? 0 : Math.round((assigned / total) * 100);
  return { total, assigned, pending, overdue, completionPct };
}

export function formatDueDisplay(value?: string | null): string {
  if (!value) return 'No Due Date';
  const d = new Date(value);
  if (Number.isNaN(d.getTime())) return 'No Due Date';
  return d.toLocaleDateString('en-GB', { day: '2-digit', month: 'short', year: 'numeric' });
}

export function toDateInputValue(value?: string | null): Date | null {
  if (!value) return null;
  const d = new Date(value);
  return Number.isNaN(d.getTime()) ? null : d;
}

export function highlightMatch(text: string, query: string): string {
  if (!query.trim()) return escapeHtml(text);
  const q = query.trim();
  const idx = text.toLowerCase().indexOf(q.toLowerCase());
  if (idx < 0) return escapeHtml(text);
  const before = escapeHtml(text.slice(0, idx));
  const match = escapeHtml(text.slice(idx, idx + q.length));
  const after = escapeHtml(text.slice(idx + q.length));
  return `${before}<mark class="oe-hl">${match}</mark>${after}`;
}

function escapeHtml(s: string): string {
  return s
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;');
}

export function buildOwnershipCsv(
  rows: Array<{
    code: string;
    title: string;
    departmentName: string;
    ownerName: string;
    dueDate: string;
    status: string;
  }>,
): string {
  const header = ['OE Code', 'Objective Element', 'Department', 'Owner', 'Due Date', 'Status'];
  const escape = (v: string) => `"${(v ?? '').replace(/"/g, '""')}"`;
  const lines = [header.join(',')];
  for (const r of rows) {
    lines.push(
      [r.code, r.title, r.departmentName, r.ownerName, r.dueDate, r.status].map(escape).join(','),
    );
  }
  return lines.join('\n');
}

export function recentDeptStorageKey(hospitalId: string): string {
  return `aovias.oe-ownership.recent-depts.${hospitalId}`;
}

export function loadRecentDepartmentIds(hospitalId: string): string[] {
  try {
    const raw = localStorage.getItem(recentDeptStorageKey(hospitalId));
    if (!raw) return [];
    const parsed = JSON.parse(raw) as string[];
    return Array.isArray(parsed) ? parsed.filter((x) => typeof x === 'string') : [];
  } catch {
    return [];
  }
}

export function pushRecentDepartmentId(hospitalId: string, departmentId: string, max = 8): void {
  const prev = loadRecentDepartmentIds(hospitalId).filter((id) => id !== departmentId);
  const next = [departmentId, ...prev].slice(0, max);
  try {
    localStorage.setItem(recentDeptStorageKey(hospitalId), JSON.stringify(next));
  } catch {
    /* ignore quota */
  }
}

export function relativeSavedLabel(savedAtMs: number | undefined, now = Date.now()): string {
  if (!savedAtMs) return 'Saved';
  const sec = Math.max(0, Math.round((now - savedAtMs) / 1000));
  if (sec < 5) return 'Saved just now';
  if (sec < 60) return `Saved ${sec} sec ago`;
  const min = Math.round(sec / 60);
  return `Saved ${min} min ago`;
}
