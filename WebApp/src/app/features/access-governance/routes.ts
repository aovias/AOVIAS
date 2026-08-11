import { Routes } from '@angular/router';

import { permissionGuard } from '../../shared/authorization/permission.guard';



export const ACCESS_GOVERNANCE_ROUTES: Routes = [

  { path: '', loadComponent: () => import('./pages/role-management.component').then((m) => m.RoleManagementComponent), canActivate: [permissionGuard('Compliance.Standard.View')] },

  { path: 'permissions', loadComponent: () => import('./pages/permission-catalog.component').then((m) => m.PermissionCatalogComponent), canActivate: [permissionGuard('Compliance.Standard.View')] },

  { path: 'policies', loadComponent: () => import('./pages/policy-designer.component').then((m) => m.PolicyDesignerComponent), canActivate: [permissionGuard('Compliance.Standard.View')] },

  { path: 'delegations', loadComponent: () => import('./pages/delegation-center.component').then((m) => m.DelegationCenterComponent), canActivate: [permissionGuard('Compliance.Standard.View')] },

  { path: 'responsibilities', redirectTo: '/app/governance/responsibilities', pathMatch: 'full' },

  { path: 'approvals', loadComponent: () => import('./pages/approval-matrix.component').then((m) => m.ApprovalMatrixComponent), canActivate: [permissionGuard('Compliance.Standard.View')] },

  { path: 'effective', loadComponent: () => import('./pages/effective-permissions.component').then((m) => m.EffectivePermissionsComponent), canActivate: [permissionGuard('Compliance.Standard.View')] },

  { path: 'simulator', loadComponent: () => import('./pages/authorization-simulator.component').then((m) => m.AuthorizationSimulatorComponent), canActivate: [permissionGuard('Compliance.Standard.View')] },

];

