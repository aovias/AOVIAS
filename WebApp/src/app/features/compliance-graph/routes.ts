import { Routes } from '@angular/router';

export const COMPLIANCE_GRAPH_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./pages/compliance-graph-feature-shell.component').then((m) => m.ComplianceGraphFeatureShellComponent),
    children: [
      { path: '', redirectTo: 'viewer', pathMatch: 'full' },
      {
        path: 'viewer',
        loadComponent: () => import('./pages/graph-viewer-page.component').then((m) => m.GraphViewerPageComponent),
      },
      {
        path: 'search',
        loadComponent: () => import('./pages/enterprise-search-page.component').then((m) => m.EnterpriseSearchPageComponent),
      },
      {
        path: 'entity/:id',
        loadComponent: () => import('./pages/entity-explorer-page.component').then((m) => m.EntityExplorerPageComponent),
      },
      {
        path: 'traceability',
        loadComponent: () => import('./pages/traceability-hub-page.component').then((m) => m.TraceabilityHubPageComponent),
      },
      {
        path: 'executive',
        loadComponent: () => import('./pages/executive-impact-page.component').then((m) => m.ExecutiveImpactPageComponent),
      },
      {
        path: 'analytics',
        loadComponent: () => import('./pages/graph-analytics-page.component').then((m) => m.GraphAnalyticsPageComponent),
      },
    ],
  },
];
