import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    redirectTo: 'regulatory-governance',
    pathMatch: 'full',
  },
  {
    path: 'regulatory-governance',
    loadComponent: () =>
      import('./regulatory-governance/pages/ccc.component').then((m) => m.CCCComponent),
  },
  {
    path: 'organizational-control',
    loadComponent: () =>
      import('./organizational-control/pages/ccc.component').then((m) => m.CCCComponent),
  },
  {
    path: 'access-accountability',
    loadComponent: () =>
      import('./access-accountability/pages/ccc.component').then((m) => m.CCCComponent),
  },
  {
    path: 'compliance-infrastructure',
    loadComponent: () =>
      import('./compliance-infrastructure/pages/ccc.component').then((m) => m.CCCComponent),
  },
  {
    path: 'risk-escalation',
    loadComponent: () =>
      import('./risk-escalation/pages/ccc.component').then((m) => m.CCCComponent),
  },
  {
    path: 'monitoring-traceability',
    loadComponent: () =>
      import('./monitoring-traceability/pages/ccc.component').then((m) => m.CCCComponent),
  },
  {
    path: 'integration-system',
    loadComponent: () =>
      import('./integration-system/pages/ccc.component').then((m) => m.CCCComponent),
  },
];
