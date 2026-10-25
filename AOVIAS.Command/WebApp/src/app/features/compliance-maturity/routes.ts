import { Routes } from '@angular/router';

export const COMPLIANCE_MATURITY_ROUTES: Routes = [
  {
    path: '',
    title: 'NABH Compliance Journey',
    data: {
      navSection: 'dashboard',
      description: 'Progressive NABH compliance maturity journey with CMI scoring',
    },
    loadComponent: () =>
      import('./pages/compliance-maturity.component').then(
        (m) => m.ComplianceMaturityComponent,
      ),
  },
];
