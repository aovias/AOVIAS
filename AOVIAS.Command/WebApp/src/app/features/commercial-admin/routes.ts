import { Routes } from '@angular/router';
import { authGuard } from '../../core/guards/auth.guard';
import { roleGuard } from '../../core/guards/role.guard';
import { COMMERCIAL_ADMIN_ROLE_IDS } from '../../core/auth/roles/role-definitions';

export const COMMERCIAL_ADMIN_ROUTES: Routes = [
  {
    path: '',
    canActivate: [authGuard, roleGuard(COMMERCIAL_ADMIN_ROLE_IDS)],
    loadComponent: () =>
      import('./pages/commercial-admin-shell.component').then((m) => m.CommercialAdminShellComponent),
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
      {
        path: 'dashboard',
        loadComponent: () =>
          import('./pages/commercial-admin-pages.component').then((m) => m.CommercialDashboardPage),
      },
      {
        path: 'organizations',
        loadComponent: () =>
          import('./pages/commercial-admin-pages.component').then((m) => m.CommercialOrganizationsPage),
      },
      {
        path: 'organizations/:id',
        loadComponent: () =>
          import('./pages/commercial-admin-pages.component').then((m) => m.CommercialOrganizationDetailPage),
      },
      {
        path: 'users',
        loadComponent: () =>
          import('./pages/commercial-admin-pages.component').then((m) => m.CommercialUsersPage),
      },
      {
        path: 'trials',
        loadComponent: () =>
          import('./pages/commercial-admin-pages.component').then((m) => m.CommercialTrialsPage),
      },
      {
        path: 'licenses',
        loadComponent: () =>
          import('./pages/commercial-admin-pages.component').then((m) => m.CommercialLicensesPage),
      },
      {
        path: 'email',
        loadComponent: () =>
          import('./pages/commercial-admin-pages.component').then((m) => m.CommercialEmailPage),
      },
      {
        path: 'support',
        loadComponent: () =>
          import('./pages/commercial-admin-pages.component').then((m) => m.CommercialSupportPage),
      },
      {
        path: 'support/:orgId',
        loadComponent: () =>
          import('./pages/commercial-admin-pages.component').then((m) => m.CommercialSupportPage),
      },
      {
        path: 'commercial',
        loadComponent: () =>
          import('./pages/commercial-admin-pages.component').then((m) => m.CommercialOpsPage),
      },
      {
        path: 'audit',
        loadComponent: () =>
          import('./pages/commercial-admin-pages.component').then((m) => m.CommercialAuditPage),
      },
      {
        path: 'search',
        loadComponent: () =>
          import('./pages/commercial-admin-pages.component').then((m) => m.CommercialSearchPage),
      },
    ],
  },
];
