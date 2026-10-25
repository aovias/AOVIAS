import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';
import { onboardingEntryGuard, onboardingGuard } from './core/guards/onboarding.guard';

const legacyAdminRedirect = (legacyPrefix: string, targetPrefix: string) => ({
  path: '**',
  loadComponent: () =>
    import('./core/layout/legacy-admin-redirect.component').then((m) => m.LegacyAdminRedirectComponent),
  data: { legacyPrefix, targetPrefix },
});

export const routes: Routes = [
  {
    path: '',
    redirectTo: 'app',
    pathMatch: 'full',
  },
  {
    path: 'setup',
    loadComponent: () =>
      import('./features/setup/pages/setup-wizard-page.component').then(
        (m) => m.SetupWizardPageComponent,
      ),
  },
  {
    path: 'invitations/accept',
    loadComponent: () =>
      import('./features/onboarding/pages/invitation-accept-page.component').then(
        (m) => m.InvitationAcceptPageComponent,
      ),
  },
  {
    path: 'login',
    loadComponent: () => import('./login/login.component').then((m) => m.LoginComponent),
  },
  {
    path: 'signup',
    loadComponent: () => import('./signup/signup').then((m) => m.SignupComponent),
  },
  {
    path: 'onboarding',
    canActivate: [authGuard, onboardingEntryGuard],
    loadChildren: () => import('./features/onboarding/routes').then((m) => m.ONBOARDING_ROUTES),
  },
  {
    path: 'register',
    loadComponent: () =>
      import('./features/auth/pages/register.component').then((m) => m.RegisterComponent),
  },
  {
    path: 'forgot-password',
    loadComponent: () =>
      import('./features/auth/pages/forgot-password.component').then(
        (m) => m.ForgotPasswordComponent,
      ),
  },
  {
    path: 'reset-password',
    loadComponent: () =>
      import('./features/auth/pages/reset-password.component').then(
        (m) => m.ResetPasswordComponent,
      ),
  },
  {
    path: 'login/mfa',
    loadComponent: () =>
      import('./features/auth/pages/mfa-login.component').then((m) => m.MfaLoginComponent),
  },
  {
    path: 'verify-email',
    loadComponent: () =>
      import('./features/auth/pages/verify-email.component').then((m) => m.VerifyEmailComponent),
  },
  {
    path: 'error/403',
    loadComponent: () =>
      import('./features/auth/pages/error-403.component').then((m) => m.Error403Component),
  },
  {
    path: 'error/404',
    loadComponent: () =>
      import('./features/auth/pages/error-404.component').then((m) => m.Error404Component),
  },
  {
    path: 'error/500',
    loadComponent: () =>
      import('./features/auth/pages/error-500.component').then((m) => m.Error500Component),
  },
  {
    path: 'error/maintenance',
    loadComponent: () =>
      import('./features/auth/pages/maintenance.component').then((m) => m.MaintenanceComponent),
  },
  // Phase 7.6 — public alias used when Gateway returns maintenance 503 / ops redirects
  {
    path: 'maintenance',
    loadComponent: () =>
      import('./features/auth/pages/maintenance.component').then((m) => m.MaintenanceComponent),
  },
  {
    path: 'commercial-admin',
    loadChildren: () =>
      import('./features/commercial-admin/routes').then((m) => m.COMMERCIAL_ADMIN_ROUTES),
  },
  {
    path: 'app',
    canActivate: [authGuard, onboardingGuard],
    children: [
      {
        path: 'settings',
        loadChildren: () => import('./features/settings/routes').then((m) => m.SETTINGS_ROUTES),
      },
      {
        path: '',
        loadComponent: () => import('./core/layout/shell.component').then((m) => m.ShellComponent),
        children: [
          {
            path: '',
            redirectTo: 'dashboard',
            pathMatch: 'full',
          },
          {
            path: 'dashboard',
            loadChildren: () =>
              import('./features/dashboard/routes').then((m) => m.DASHBOARD_ROUTES),
          },
          {
            path: 'role-homes',
            loadChildren: () =>
              import('./features/role-homes/routes').then((m) => m.ROLE_HOMES_ROUTES),
          },
          {
            path: 'frameworks',
            loadChildren: () =>
              import('./features/frameworks/routes').then((m) => m.FRAMEWORKS_ROUTES),
          },
          {
            path: 'control-center',
            loadChildren: () => import('./features/control-center/routes').then((m) => m.routes),
          },
          {
            path: 'journey',
            loadChildren: () => import('./features/journey/routes').then((m) => m.routes),
          },
          {
            path: 'tasks',
            loadChildren: () => import('./features/tasks/routes').then((m) => m.routes),
          },
          {
            path: 'evidence',
            loadChildren: () => import('./features/evidence/routes').then((m) => m.routes),
          },
          {
            path: 'audit',
            loadChildren: () => import('./features/audit/routes').then((m) => m.routes),
          },
          {
            path: 'reports',
            loadChildren: () => import('./features/reporting/routes').then((m) => m.routes),
          },
          {
            path: 'me',
            loadChildren: () =>
              import('./features/productivity/routes').then((m) => m.PRODUCTIVITY_ROUTES),
          },
          {
            path: 'productivity',
            loadChildren: () =>
              import('./features/productivity/routes').then((m) => m.PRODUCTIVITY_ROUTES),
          },
          {
            path: 'gap-analysis/:assessmentId',
            loadChildren: () =>
              import('./features/gap-analysis/routes').then((m) => m.GAP_ANALYSIS_ROUTES),
          },
          {
            path: 'ccc',
            loadChildren: () =>
              import('./features/compliance-control-center/routes').then((m) => m.routes),
          },
          {
            path: 'compliance-maturity',
            loadChildren: () =>
              import('./features/compliance-maturity/routes').then((m) => m.COMPLIANCE_MATURITY_ROUTES),
          },
          {
            path: 'analytics',
            loadChildren: () => import('./features/analytics/routes').then((m) => m.ANALYTICS_ROUTES),
          },
          {
            path: 'copilot',
            loadChildren: () => import('./features/copilot/routes').then((m) => m.COPILOT_ROUTES),
          },
          {
            path: 'auditor',
            loadChildren: () => import('./features/auditor/routes').then((m) => m.AUDITOR_ROUTES),
          },
          {
            path: 'department-head',
            loadChildren: () =>
              import('./features/department-head/routes').then((m) => m.DEPARTMENT_HEAD_ROUTES),
          },
          {
            path: 'consultant',
            loadChildren: () =>
              import('./features/consultant/routes').then((m) => m.CONSULTANT_ROUTES),
          },
          {
            path: 'portfolio',
            loadChildren: () =>
              import('./features/consultant/routes').then((m) => m.CONSULTANT_ROUTES),
          },
          {
            path: 'documentation',
            loadChildren: () =>
              import('./features/documentation/routes').then((m) => m.DOCUMENTATION_ROUTES),
          },
          {
            path: 'risks',
            loadChildren: () =>
              import('./features/risks/routes').then((m) => m.RISKS_ROUTES),
          },
          {
            path: 'patient-safety',
            loadChildren: () =>
              import('./features/patient-safety/routes').then((m) => m.PATIENT_SAFETY_ROUTES),
          },
          {
            path: 'quality-intelligence',
            loadChildren: () =>
              import('./features/quality-intelligence/routes').then((m) => m.QUALITY_INTELLIGENCE_ROUTES),
          },
          {
            path: 'compliance-graph',
            loadChildren: () =>
              import('./features/compliance-graph/routes').then((m) => m.COMPLIANCE_GRAPH_ROUTES),
          },
          {
            path: 'account',
            children: [
              { path: '', redirectTo: '/app/settings/account', pathMatch: 'full' },
              { path: 'security', redirectTo: '/app/settings/account', pathMatch: 'full' },
            ],
          },
          {
            path: 'persons',
            loadChildren: () => import('./features/persons/routes').then((m) => m.PERSON_ROUTES),
          },
          {
            path: 'workforce',
            loadChildren: () => import('./features/workforce/routes').then((m) => m.WORKFORCE_ROUTES),
          },
          {
            path: 'professional',
            children: [
              { path: '', redirectTo: '/app/settings/teams/professionals', pathMatch: 'full' },
              {
                path: 'renewals',
                loadComponent: () =>
                  import('./features/professional/pages/renewal-dashboard.component').then(
                    (m) => m.RenewalDashboardComponent,
                  ),
              },
              {
                path: ':id/timeline',
                loadComponent: () =>
                  import('./features/professional/pages/professional-timeline.component').then(
                    (m) => m.ProfessionalTimelineComponent,
                  ),
              },
              {
                path: ':id',
                loadComponent: () =>
                  import('./features/professional/pages/professional-detail.component').then(
                    (m) => m.ProfessionalDetailComponent,
                  ),
              },
              legacyAdminRedirect('/app/professional', '/app/settings/teams/professionals'),
            ],
          },
          // Stage 5A — access governance is administration; it now lives in the
          // Administration Workspace Security and Approvals tabs.
          {
            path: 'access-governance',
            children: [
              { path: '', redirectTo: '/app/settings/security/roles', pathMatch: 'full' },
              { path: 'permissions', redirectTo: '/app/settings/security/permissions', pathMatch: 'full' },
              { path: 'policies', redirectTo: '/app/settings/security/policies', pathMatch: 'full' },
              { path: 'effective', redirectTo: '/app/settings/security/effective', pathMatch: 'full' },
              { path: 'simulator', redirectTo: '/app/settings/security/simulator', pathMatch: 'full' },
              { path: 'delegations', redirectTo: '/app/settings/approvals/delegations', pathMatch: 'full' },
              { path: 'approvals', redirectTo: '/app/settings/approvals/matrix', pathMatch: 'full' },
              // Pre-existing hand-off, preserved.
              { path: 'responsibilities', redirectTo: '/app/governance/responsibilities', pathMatch: 'full' },
              legacyAdminRedirect('/app/access-governance', '/app/settings/security'),
            ],
          },
          {
            path: 'governance',
            loadChildren: () => import('./features/governance/routes').then((m) => m.GOVERNANCE_ROUTES),
          },
          // Stage 5A — enterprise security posture is administration; it now lives in the
          // Administration Workspace Security tab.
          {
            path: 'security',
            children: [legacyAdminRedirect('/app/security', '/app/settings/security/posture')],
          },
          {
            path: 'notifications',
            children: [legacyAdminRedirect('/app/notifications', '/app/settings/notifications')],
          },
          {
            path: 'ai-identity',
            children: [
              { path: '', redirectTo: '/app/settings/ai/dashboard', pathMatch: 'full' },
              { path: 'profiles', redirectTo: '/app/settings/ai/identity', pathMatch: 'full' },
              { path: 'prompts', redirectTo: '/app/settings/ai/prompts', pathMatch: 'full' },
              { path: 'prompts/:id/versions', redirectTo: '/app/settings/ai/prompts/:id/versions' },
              { path: 'policies', redirectTo: '/app/settings/ai/policies', pathMatch: 'full' },
              { path: 'permissions', redirectTo: '/app/settings/ai/permissions', pathMatch: 'full' },
              { path: 'executions', redirectTo: '/app/settings/ai/executions', pathMatch: 'full' },
              { path: 'analytics', redirectTo: '/app/settings/ai/analytics', pathMatch: 'full' },
              { path: 'approvals', redirectTo: '/app/settings/ai/approvals', pathMatch: 'full' },
              { path: 'models', redirectTo: '/app/settings/ai/models', pathMatch: 'full' },
              { path: 'providers', redirectTo: '/app/settings/ai/providers', pathMatch: 'full' },
              legacyAdminRedirect('/app/ai-identity', '/app/settings/ai'),
            ],
          },
          {
            path: 'workforce-intelligence',
            children: [
              legacyAdminRedirect(
                '/app/workforce-intelligence',
                '/app/settings/teams/intelligence',
              ),
            ],
          },
          {
            path: 'directory',
            children: [
              legacyAdminRedirect('/app/directory', '/app/settings/teams/directory'),
            ],
          },
          {
            path: 'integration',
            children: [
              legacyAdminRedirect('/app/integration', '/app/settings/system/integrations'),
            ],
          },
          {
            path: 'organization',
            loadChildren: () =>
              import('./features/organization/routes').then((m) => m.ORGANIZATION_ROUTES),
          },
        ],
      },
    ],
  },
];
