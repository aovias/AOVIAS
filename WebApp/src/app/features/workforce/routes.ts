import { Routes } from '@angular/router';

export const WORKFORCE_ROUTES: Routes = [
  { path: '', loadComponent: () => import('./pages/workforce-directory.component').then((m) => m.WorkforceDirectoryComponent) },
  { path: 'org-chart', loadComponent: () => import('./pages/org-chart.component').then((m) => m.OrgChartComponent) },
  { path: 'manager', loadComponent: () => import('./pages/manager-view.component').then((m) => m.ManagerViewComponent) },
  { path: ':id/transfer', loadComponent: () => import('./pages/transfer-wizard.component').then((m) => m.TransferWizardComponent) },
  { path: ':id/promote', loadComponent: () => import('./pages/promotion-wizard.component').then((m) => m.PromotionWizardComponent) },
  { path: ':id', loadComponent: () => import('./pages/employment-detail.component').then((m) => m.EmploymentDetailComponent) },
];
