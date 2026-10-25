import { Routes } from '@angular/router';

export const ROLE_HOMES_ROUTES: Routes = [
  {
    path: 'qm',
    title: 'Quality Manager Home',
    data: {
      navSection: 'dashboard',
      description: 'Quality Manager operational workspace',
    },
    loadComponent: () => import('./qm-home.component').then((m) => m.QmHomeComponent),
  },
  {
    path: '',
    pathMatch: 'full',
    redirectTo: 'qm',
  },
];
