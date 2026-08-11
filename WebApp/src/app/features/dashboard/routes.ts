import { Routes } from '@angular/router';

export const DASHBOARD_ROUTES: Routes = [
  {
    path: '',
    title: 'Compliance Dashboard',
    data: {
      navSection: 'dashboard',
      description: 'Command center overview with department readiness heatmap',
    },
    loadComponent: () =>
      import('./pages/dashboard.component').then((m) => m.DashboardComponent),
  },
];
