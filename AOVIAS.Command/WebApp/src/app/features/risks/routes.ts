import { Routes } from '@angular/router';

export const RISKS_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./pages/risks-feature-shell.component').then((m) => m.RisksFeatureShellComponent),
    children: [
      { path: '', redirectTo: 'register', pathMatch: 'full' },
      {
        path: 'register',
        loadComponent: () =>
          import('./pages/risk-register-page.component').then((m) => m.RiskRegisterPageComponent),
      },
      {
        path: 'matrix',
        loadComponent: () =>
          import('./pages/risk-matrix-page.component').then((m) => m.RiskMatrixPageComponent),
      },
      {
        path: 'new',
        loadComponent: () =>
          import('./pages/risk-form-page.component').then((m) => m.RiskFormPageComponent),
      },
      {
        path: ':id/edit',
        loadComponent: () =>
          import('./pages/risk-form-page.component').then((m) => m.RiskFormPageComponent),
      },
    ],
  },
];
