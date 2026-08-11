import { Routes } from '@angular/router';
import { permissionGuard } from '../../shared/authorization/permission.guard';

export const ADMIN_AUDIT_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./pages/audit-activity-page.component').then((m) => m.AuditActivityPageComponent),
    canActivate: [permissionGuard('Audit.View')],
  },
];
