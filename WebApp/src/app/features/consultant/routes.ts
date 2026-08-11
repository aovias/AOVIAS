import { Routes } from '@angular/router';
import { roleGuard } from '../../core/guards/role.guard';
import { asRoleIds } from '../../core/auth/roles/role-definitions';

/** Consultant / multi-hospital portfolio — also aliased as /app/portfolio. */
export const CONSULTANT_ROUTES: Routes = [
  {
    path: '',
    canActivate: [
      roleGuard(
        asRoleIds([
          'COMPLIANCE_VIEWER',
          'REGULATORY_ADMIN',
          'AUTHORITY',
          'ORG_ADMIN',
          'GROUP_ADMIN',
          'SUPER_ADMIN',
        ] as const),
      ),
    ],
    loadComponent: () =>
      import('./pages/consultant-portfolio.component').then((m) => m.ConsultantPortfolioComponent),
    data: { title: 'Consultant Portfolio' },
  },
];
