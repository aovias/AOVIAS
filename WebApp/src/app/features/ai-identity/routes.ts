import { Routes } from '@angular/router';
import { permissionGuard } from '../../shared/authorization/permission.guard';

export const AI_ADMIN_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./components/ai-admin-shell.component').then((m) => m.AiAdminShellComponent),
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
      {
        path: 'dashboard',
        loadComponent: () =>
          import('./pages/ai-overview-page.component').then((m) => m.AiOverviewPageComponent),
      },
      {
        path: 'identity',
        loadComponent: () =>
          import('./pages/ai-profile-manager.component').then((m) => m.AiProfileManagerComponent),
        canActivate: [permissionGuard('AI.Profile.Manage')],
      },
      {
        path: 'models',
        loadComponent: () =>
          import('./pages/ai-models-page.component').then((m) => m.AiModelsPageComponent),
      },
      {
        path: 'providers',
        loadComponent: () =>
          import('./pages/ai-providers-page.component').then((m) => m.AiProvidersPageComponent),
      },
      {
        path: 'prompts',
        loadComponent: () =>
          import('./pages/prompt-library.component').then((m) => m.PromptLibraryComponent),
        canActivate: [permissionGuard('AI.Prompt.Manage')],
      },
      {
        path: 'prompts/:id/versions',
        loadComponent: () =>
          import('./pages/prompt-version-history.component').then((m) => m.PromptVersionHistoryComponent),
        canActivate: [permissionGuard('AI.Prompt.Manage')],
      },
      {
        path: 'policies',
        loadComponent: () =>
          import('./pages/automation-policies.component').then((m) => m.AutomationPoliciesComponent),
        canActivate: [permissionGuard('AI.Policy.Manage')],
      },
      {
        path: 'permissions',
        loadComponent: () =>
          import('./pages/ai-permissions.component').then((m) => m.AiPermissionsComponent),
      },
      {
        path: 'executions',
        loadComponent: () =>
          import('./pages/execution-history.component').then((m) => m.ExecutionHistoryComponent),
      },
      {
        path: 'analytics',
        loadComponent: () =>
          import('./pages/usage-analytics.component').then((m) => m.UsageAnalyticsComponent),
        canActivate: [permissionGuard('AI.Analytics.View')],
      },
      {
        path: 'approvals',
        loadComponent: () =>
          import('./pages/approval-queue.component').then((m) => m.ApprovalQueueComponent),
      },
      // Legacy aliases within AI admin shell
      { path: 'overview', redirectTo: 'dashboard', pathMatch: 'full' },
      { path: 'profiles', redirectTo: 'identity', pathMatch: 'full' },
    ],
  },
];

/** @deprecated Use AI_ADMIN_ROUTES — kept for legacy import compatibility. */
export const AI_IDENTITY_ROUTES = AI_ADMIN_ROUTES;
