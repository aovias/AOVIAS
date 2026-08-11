import { Routes } from '@angular/router';

export const DOCUMENTATION_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./pages/documentation-feature-shell.component').then((m) => m.DocumentationFeatureShellComponent),
    children: [
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
      {
        path: 'dashboard',
        loadComponent: () =>
          import('./pages/documentation-dashboard-page.component').then(
            (m) => m.DocumentationDashboardPageComponent,
          ),
      },
      {
        path: 'register',
        loadComponent: () =>
          import('./pages/documentation-documents-page.component').then(
            (m) => m.DocumentationDocumentsPageComponent,
          ),
        data: { section: 'register' },
      },
      {
        path: 'policies',
        loadComponent: () =>
          import('./pages/documentation-documents-page.component').then(
            (m) => m.DocumentationDocumentsPageComponent,
          ),
        data: { section: 'policies', typeFilter: 'policy' },
      },
      {
        path: 'sops',
        loadComponent: () =>
          import('./pages/documentation-documents-page.component').then(
            (m) => m.DocumentationDocumentsPageComponent,
          ),
        data: { section: 'sops', typeFilter: 'sop' },
      },
      {
        path: 'manuals',
        loadComponent: () =>
          import('./pages/documentation-documents-page.component').then(
            (m) => m.DocumentationDocumentsPageComponent,
          ),
        data: { section: 'manuals', typeFilter: 'manual' },
      },
      {
        path: 'forms',
        loadComponent: () =>
          import('./pages/documentation-documents-page.component').then(
            (m) => m.DocumentationDocumentsPageComponent,
          ),
        data: { section: 'forms', typeFilter: 'form' },
      },
      {
        path: 'authoring',
        loadComponent: () =>
          import('./pages/authoring-studio-home-page.component').then(
            (m) => m.AuthoringStudioHomePageComponent,
          ),
      },
      {
        path: 'authoring/:id',
        loadComponent: () =>
          import('./pages/authoring-studio-page.component').then(
            (m) => m.AuthoringStudioPageComponent,
          ),
      },
      {
        path: 'explorer',
        loadComponent: () =>
          import('./pages/document-explorer-page.component').then(
            (m) => m.DocumentExplorerPageComponent,
          ),
      },
      {
        path: 'graph',
        loadComponent: () =>
          import('./pages/document-graph-page.component').then(
            (m) => m.DocumentGraphPageComponent,
          ),
      },
      {
        path: 'graph/:id',
        loadComponent: () =>
          import('./pages/document-graph-page.component').then(
            (m) => m.DocumentGraphPageComponent,
          ),
      },
      {
        path: 'review-calendar',
        loadComponent: () =>
          import('./pages/documentation-review-calendar-page.component').then(
            (m) => m.DocumentationReviewCalendarPageComponent,
          ),
      },
      {
        path: 'acknowledgements',
        loadComponent: () =>
          import('./pages/documentation-acknowledgements-page.component').then(
            (m) => m.DocumentationAcknowledgementsPageComponent,
          ),
      },
      {
        path: 'analytics',
        loadComponent: () =>
          import('./pages/documentation-analytics-page.component').then(
            (m) => m.DocumentationAnalyticsPageComponent,
          ),
      },
      {
        path: 'templates',
        loadComponent: () =>
          import('./pages/documentation-templates-page.component').then(
            (m) => m.DocumentationTemplatesPageComponent,
          ),
      },
      {
        path: 'settings',
        loadComponent: () =>
          import('./pages/documentation-settings-page.component').then(
            (m) => m.DocumentationSettingsPageComponent,
          ),
      },
    ],
  },
];
