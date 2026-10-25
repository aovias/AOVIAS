import { Routes } from '@angular/router';

export const ORGANIZATION_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./pages/organization-feature-shell.component').then(
        (m) => m.OrganizationFeatureShellComponent,
      ),
    children: [
      { path: '', redirectTo: 'overview', pathMatch: 'full' },
      {
        path: 'overview',
        loadComponent: () =>
          import('./pages/organization-overview.component').then(
            (m) => m.OrganizationOverviewComponent,
          ),
      },
      {
        path: 'hospitals',
        loadComponent: () =>
          import('./pages/organization-explorer.component').then(
            (m) => m.HospitalHierarchyComponent,
          ),
      },
      {
        path: 'facility',
        loadComponent: () =>
          import('./pages/organization-explorer.component').then(
            (m) => m.HospitalHierarchyComponent,
          ),
      },
      {
        path: 'departments',
        loadComponent: () =>
          import('./pages/organization-explorer.component').then(
            (m) => m.DepartmentHierarchyComponent,
          ),
      },
      {
        path: 'membership',
        loadComponent: () =>
          import('./pages/organization-explorer.component').then(
            (m) => m.MembershipExplorerComponent,
          ),
      },
      {
        path: 'membership/history',
        loadComponent: () =>
          import('./pages/organization-explorer.component').then(
            (m) => m.MembershipHistoryComponent,
          ),
      },
      {
        path: 'oe-ownership',
        loadComponent: () =>
          import('./pages/oe-ownership-matrix.component').then(
            (m) => m.OeOwnershipMatrixComponent,
          ),
      },
      {
        path: 'assignments',
        loadComponent: () =>
          import('./pages/organization-explorer.component').then(
            (m) => m.AssignmentTimelineComponent,
          ),
      },
      {
        path: 'graph',
        loadComponent: () =>
          import('./pages/organization-explorer.component').then(
            (m) => m.OrganizationGraphComponent,
          ),
      },
      {
        path: 'settings',
        loadComponent: () =>
          import('./pages/organization-settings-page.component').then(
            (m) => m.OrganizationSettingsPageComponent,
          ),
      },
    ],
  },
];
