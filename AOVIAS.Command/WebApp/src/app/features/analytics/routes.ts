import { Routes } from '@angular/router';

export const ANALYTICS_ROUTES: Routes = [
  {
    path: '',
    title: 'Analytics Hub',
    data: {
      navSection: 'dashboard',
      description: 'Read-only compliance intelligence and KPI observatory',
    },
    loadComponent: () =>
      import('./pages/analytics-hub.component').then((m) => m.AnalyticsHubComponent),
  },
  {
    path: ':section',
    title: 'Analytics Hub',
    loadComponent: () =>
      import('./pages/analytics-hub.component').then((m) => m.AnalyticsHubComponent),
  },
];
