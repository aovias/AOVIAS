import type { NavigationSearchHit } from '../surfaces/search.provider';

/** Result categories shown in the command palette. */
export type SearchResultCategory =
  | 'pages'
  | 'modules'
  | 'actions'
  | 'settings'
  | 'favorites'
  | 'suggested'
  | 'ops'
  | 'departments'
  | 'audits'
  | 'reports'
  | 'documents'
  | 'users';

export interface SearchIndexEntry {
  readonly id: string;
  readonly title: string;
  readonly subtitle?: string;
  readonly route?: string;
  readonly icon?: string;
  readonly category: SearchResultCategory;
  readonly keywords: readonly string[];
  readonly moduleId?: string;
  readonly badgeKey?: string;
  /** When set, invoke instead of navigating. */
  readonly actionId?: string;
}

export interface CommandActionDef {
  readonly id: string;
  readonly title: string;
  readonly subtitle?: string;
  readonly route?: string;
  readonly keywords: readonly string[];
  readonly category: 'actions' | 'settings';
}

/** Static command actions (navigate / open known routes). */
export const COMMAND_ACTIONS: readonly CommandActionDef[] = Object.freeze([
  {
    id: 'cmd.open-dashboard',
    title: 'Open Dashboard',
    route: '/app/dashboard',
    keywords: ['home', 'overview'],
    category: 'actions',
  },
  {
    id: 'cmd.open-audit',
    title: 'Open Audit Calendar',
    route: '/app/audit',
    keywords: ['audit', 'calendar', 'internal'],
    category: 'actions',
  },
  {
    id: 'cmd.create-capa',
    title: 'Create CAPA',
    route: '/app/audit/capa',
    keywords: ['capa', 'deficiency', 'corrective'],
    category: 'actions',
  },
  {
    id: 'cmd.start-internal-audit',
    title: 'Start Internal Audit',
    route: '/app/audit/planning',
    keywords: ['audit', 'start', 'planning'],
    category: 'actions',
  },
  {
    id: 'cmd.view-framework',
    title: 'View Framework',
    route: '/app/frameworks',
    keywords: ['framework', 'nabh', 'catalog'],
    category: 'actions',
  },
  {
    id: 'cmd.open-department',
    title: 'Open Department',
    route: '/app/department-head',
    keywords: ['department', 'workspace'],
    category: 'actions',
  },
  {
    id: 'cmd.generate-report',
    title: 'Generate Report',
    route: '/app/reports/compliance',
    keywords: ['report', 'generate', 'export'],
    category: 'actions',
  },
  {
    id: 'cmd.go-settings',
    title: 'Go to Settings',
    route: '/app/settings/account',
    keywords: ['settings', 'account', 'preferences'],
    category: 'settings',
  },
  {
    id: 'cmd.create-incident',
    title: 'Create Incident',
    route: '/app/patient-safety/command-center',
    keywords: ['incident', 'patient', 'safety'],
    category: 'actions',
  },
  {
    id: 'cmd.search-policies',
    title: 'Search Policies',
    route: '/app/documentation/policies',
    keywords: ['policy', 'policies', 'documentation'],
    category: 'actions',
  },
  {
    id: 'cmd.nav-preferences',
    title: 'Navigation Preferences',
    route: '/app/settings/account',
    keywords: ['preferences', 'density', 'sidebar'],
    category: 'settings',
  },
]);

/** Future API sources — empty until backends land. */
export const PLACEHOLDER_CATEGORIES: readonly SearchResultCategory[] = [
  'departments',
  'audits',
  'reports',
  'documents',
  'users',
] as const;

export function hitToIndexEntry(
  hit: NavigationSearchHit,
  category: SearchResultCategory,
): SearchIndexEntry {
  return {
    id: hit.id,
    title: hit.title,
    route: hit.route,
    icon: hit.icon,
    category,
    keywords: [hit.title, hit.route, hit.moduleId],
    moduleId: hit.moduleId,
  };
}
