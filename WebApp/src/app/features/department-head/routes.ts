import { Routes } from '@angular/router';
import { roleGuard } from '../../core/guards/role.guard';
import { asRoleIds } from '../../core/auth/roles/role-definitions';

export const DEPARTMENT_HEAD_ROUTES: Routes = [
  {
    path: '',
    canActivate: [
      roleGuard(
        asRoleIds(['DEPARTMENT_HEAD', 'MANAGER', 'ORG_ADMIN', 'GROUP_ADMIN', 'SUPER_ADMIN'] as const),
      ),
    ],
    loadComponent: () =>
      import('./pages/department-head-home.component').then((m) => m.DepartmentHeadHomeComponent),
    data: { title: 'Department Head Workspace' },
  },
];
