import { Routes } from '@angular/router';

export const WORKFORCE_ADMIN_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./components/workforce-admin-shell.component').then((m) => m.WorkforceAdminShellComponent),
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
      {
        path: 'dashboard',
        loadComponent: () =>
          import('./pages/workforce-dashboard-page.component').then((m) => m.WorkforceDashboardPageComponent),
      },
      {
        path: 'intelligence',
        loadChildren: () =>
          import('../workforce-intelligence/routes').then((m) => m.WORKFORCE_INTELLIGENCE_ROUTES),
      },
      {
        path: 'directory',
        loadChildren: () =>
          import('../enterprise-directory/routes').then((m) => m.ENTERPRISE_DIRECTORY_ROUTES),
      },
      {
        path: 'departments',
        loadComponent: () =>
          import('./pages/workforce-departments-page.component').then((m) => m.WorkforceDepartmentsPageComponent),
      },
      {
        path: 'designations',
        loadComponent: () =>
          import('./pages/workforce-designations-page.component').then((m) => m.WorkforceDesignationsPageComponent),
      },
      {
        path: 'professionals',
        loadComponent: () =>
          import('./pages/workforce-professionals-page.component').then((m) => m.WorkforceProfessionalsPageComponent),
      },
      {
        path: 'memberships',
        loadComponent: () =>
          import('./pages/workforce-memberships-page.component').then((m) => m.WorkforceMembershipsPageComponent),
      },
      {
        path: 'invitations',
        loadComponent: () =>
          import('./pages/workforce-invitations-page.component').then((m) => m.WorkforceInvitationsPageComponent),
      },
      {
        path: 'training',
        loadChildren: () => import('../training/routes').then((m) => m.TRAINING_ROUTES),
      },
      // Legacy aliases
      { path: 'membership', redirectTo: 'memberships', pathMatch: 'full' },
      { path: 'professional', redirectTo: 'professionals', pathMatch: 'full' },
    ],
  },
];
