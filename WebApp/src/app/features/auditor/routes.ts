import { Routes } from '@angular/router';

/**
 * `/app/auditor/**` — retired as a separate destination in Phase 2.
 *
 * Every auditor page now lives inside the Audit Workspace, so there is exactly
 * one URL per capability and one navigation source of truth. This table exists
 * purely so previously valid links, bookmarks, and in-app drill-downs keep
 * working; it forwards into the workspace and nothing more.
 *
 * The page components are untouched — they are loaded by `audit/routes.ts`.
 * Role guards moved with them, using the same role set that used to guard this
 * tree, so access control is unchanged.
 *
 *   /app/auditor                 → /app/audit/overview/auditor-home
 *   /app/auditor/dashboard       → /app/audit/overview/auditor-home
 *   /app/auditor/sessions        → /app/audit/execution/sessions
 *   /app/auditor/sessions/:id    → /app/audit/execution/sessions/:id
 *   /app/auditor/observations     → /app/audit/execution/observations
 *   /app/auditor/evidence-review → /app/audit/evidence
 *   /app/auditor/findings        → /app/audit/findings
 *   /app/auditor/reports         → /app/audit/reports/sessions
 *   /app/auditor/history         → /app/audit/reports/history
 */
export const AUDITOR_ROUTES: Routes = [
  { path: '', redirectTo: '/app/audit/overview/auditor-home', pathMatch: 'full' },
  { path: 'dashboard', redirectTo: '/app/audit/overview/auditor-home', pathMatch: 'full' },
  { path: 'sessions', redirectTo: '/app/audit/execution/sessions', pathMatch: 'full' },
  { path: 'sessions/:id', redirectTo: '/app/audit/execution/sessions/:id' },
  { path: 'observations', redirectTo: '/app/audit/execution/observations', pathMatch: 'full' },
  { path: 'evidence-review', redirectTo: '/app/audit/evidence', pathMatch: 'full' },
  { path: 'findings', redirectTo: '/app/audit/findings', pathMatch: 'full' },
  { path: 'reports', redirectTo: '/app/audit/reports/sessions', pathMatch: 'full' },
  { path: 'history', redirectTo: '/app/audit/reports/history', pathMatch: 'full' },
];
