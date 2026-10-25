import { Routes } from '@angular/router';
import { auditSurfaceGuard } from './workspace/services/audit-surface.guard';

/**
 * Audit Workspace routing — Phase 2 (consolidation).
 *
 * The workspace shell mounts once for `/app/audit/**`; only the content region
 * changes between capabilities.
 *
 * ONE canonical URL per capability:
 *   overview        /app/audit/overview      (programme | auditor-home)
 *   planning        /app/audit/planning
 *   execution       /app/audit/execution     (sessions | observations | surveillance | simulation)
 *   evidence        /app/audit/evidence
 *   findings        /app/audit/findings      (+ /:id)
 *   capa            /app/audit/capa
 *   reports         /app/audit/reports       (audit | sessions | history)
 *   intelligence    /app/audit/intelligence
 *   administration  /app/audit/administration
 *
 * Every previously valid URL still resolves — see the redirect block at the end
 * and `auditor/routes.ts`. Nothing is deleted; duplicates became redirects.
 *
 * Guards: the auditor session surfaces keep the exact role set that protected
 * them under `/app/auditor/**`. Consolidation changes no permission.
 */
export const routes: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./workspace/audit-workspace.component').then((m) => m.AuditWorkspaceComponent),
    children: [
      { path: '', redirectTo: 'overview', pathMatch: 'full' },

      // ── Overview ──────────────────────────────────────────────────────
      {
        path: 'overview',
        loadComponent: () =>
          import('./workspace/components/audit-overview-shell.component').then(
            (m) => m.AuditOverviewShellComponent,
          ),
        children: [
          { path: '', redirectTo: 'programme', pathMatch: 'full' },
          {
            path: 'programme',
            loadComponent: () =>
              import('./pages/audit-dashboard.component').then((m) => m.AuditDashboardComponent),
          },
          {
            path: 'auditor-home',
            canActivate: [auditSurfaceGuard('overview', 'auditor-home')],
            loadComponent: () =>
              import('../auditor/pages/auditor-dashboard-page.component').then(
                (m) => m.AuditorDashboardPageComponent,
              ),
          },
        ],
      },

      // ── Planning ──────────────────────────────────────────────────────
      {
        path: 'planning',
        loadComponent: () =>
          import('./pages/audit-planning.component').then((m) => m.AuditPlanningComponent),
      },

      // ── Execution ─────────────────────────────────────────────────────
      {
        path: 'execution',
        loadComponent: () =>
          import('./workspace/components/audit-execution-shell.component').then(
            (m) => m.AuditExecutionShellComponent,
          ),
        children: [
          { path: '', redirectTo: 'surveillance', pathMatch: 'full' },
          {
            path: 'sessions',
            canActivate: [auditSurfaceGuard('execution', 'sessions')],
            loadComponent: () =>
              import('../auditor/pages/auditor-sessions-page.component').then(
                (m) => m.AuditorSessionsPageComponent,
              ),
          },
          {
            path: 'sessions/:id',
            canActivate: [auditSurfaceGuard('execution', 'sessions')],
            loadComponent: () =>
              import('../auditor/pages/auditor-session-workspace-page.component').then(
                (m) => m.AuditorSessionWorkspacePageComponent,
              ),
          },
          {
            path: 'observations',
            canActivate: [auditSurfaceGuard('execution', 'observations')],
            loadComponent: () =>
              import('../auditor/pages/auditor-observations-page.component').then(
                (m) => m.AuditorObservationsPageComponent,
              ),
          },
          {
            path: 'surveillance',
            loadComponent: () =>
              import('./pages/audit-surveillance.component').then(
                (m) => m.AuditSurveillanceComponent,
              ),
          },
          {
            path: 'simulation',
            loadComponent: () =>
              import('./pages/audit-simulation.component').then((m) => m.AuditSimulationComponent),
          },
        ],
      },

      // ── Evidence ──────────────────────────────────────────────────────
      // The Assessment Workspace (Chapter → Standard → OE tree with evidence and
      // traceability) is the module's evidence surface; it lives here only.
      {
        path: 'evidence',
        loadComponent: () =>
          import('../auditor/pages/auditor-assessment-page.component').then(
            (m) => m.AuditorAssessmentPageComponent,
          ),
      },

      // ── Findings ──────────────────────────────────────────────────────
      {
        path: 'findings',
        loadComponent: () =>
          import('./pages/audit-findings-page.component').then((m) => m.AuditFindingsPageComponent),
      },
      {
        path: 'findings/:id',
        loadComponent: () =>
          import('./pages/audit-finding-detail.component').then(
            (m) => m.AuditFindingDetailComponent,
          ),
      },

      // ── CAPA ──────────────────────────────────────────────────────────
      {
        path: 'capa',
        loadComponent: () =>
          import('./pages/corrective-actions.component').then((m) => m.CorrectiveActionsComponent),
      },

      // ── Reports ───────────────────────────────────────────────────────
      {
        path: 'reports',
        loadComponent: () =>
          import('./workspace/components/audit-reports-shell.component').then(
            (m) => m.AuditReportsShellComponent,
          ),
        children: [
          { path: '', redirectTo: 'audit', pathMatch: 'full' },
          {
            path: 'audit',
            loadComponent: () =>
              import('./pages/audit-reports.component').then((m) => m.AuditReportsComponent),
          },
          {
            path: 'sessions',
            canActivate: [auditSurfaceGuard('reports', 'sessions')],
            loadComponent: () =>
              import('../auditor/pages/auditor-reports-page.component').then(
                (m) => m.AuditorReportsPageComponent,
              ),
          },
          {
            path: 'history',
            canActivate: [auditSurfaceGuard('reports', 'history')],
            loadComponent: () =>
              import('../auditor/pages/auditor-history-page.component').then(
                (m) => m.AuditorHistoryPageComponent,
              ),
          },
        ],
      },

      // ── Intelligence / Administration (capability placeholders) ───────
      {
        path: 'intelligence',
        loadComponent: () =>
          import('./workspace/components/audit-intelligence-page.component').then(
            (m) => m.AuditIntelligencePageComponent,
          ),
      },
      {
        path: 'administration',
        loadComponent: () =>
          import('./workspace/components/audit-administration-page.component').then(
            (m) => m.AuditAdministrationPageComponent,
          ),
      },

      // ── Preserved legacy URLs (redirect only — nothing removed) ───────
      { path: 'assessment', redirectTo: 'evidence', pathMatch: 'full' },
      { path: 'corrective-actions', redirectTo: 'capa', pathMatch: 'full' },
      { path: 'simulation', redirectTo: 'execution/simulation', pathMatch: 'full' },
      { path: 'surveillance', redirectTo: 'execution/surveillance', pathMatch: 'full' },
      /** Historical nav alias — "Internal Audit" was always the dashboard. */
      { path: 'internal', redirectTo: 'overview', pathMatch: 'full' },
      /** Historical nav alias for Final Audit → reports. */
      { path: 'final', redirectTo: 'reports', pathMatch: 'full' },
      { path: 'calendar', redirectTo: 'planning', pathMatch: 'full' },
    ],
  },
];
