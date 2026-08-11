import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () => import('./pages/reports-landing.component').then((m) => m.ReportsLandingComponent),
  },
  {
    path: 'compliance',
    loadComponent: () => import('./pages/compliance-reports.component').then((m) => m.ComplianceReportsComponent),
  },
  {
    path: 'audit',
    loadComponent: () => import('./pages/audit-reports.component').then((m) => m.AuditReportsComponent),
  },
  {
    path: 'tasks',
    loadComponent: () => import('./pages/task-reports.component').then((m) => m.TaskReportsComponent),
  },
  {
    path: 'evidence',
    loadComponent: () => import('./pages/evidence-reports.component').then((m) => m.EvidenceReportsComponent),
  },
  {
    path: 'readiness',
    loadComponent: () => import('./pages/readiness-reports.component').then((m) => m.ReadinessReportsComponent),
  },
  {
    path: 'custom',
    loadComponent: () => import('./pages/custom-reports.component').then((m) => m.CustomReportsComponent),
  },
];
